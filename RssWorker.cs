using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;
using RssWebhook.RssModels;

namespace RssWebhook
{
    class RssWorker : IDisposable
    {
        private const string s_timestampFormat = "yyyy-MM-dd HH:mm:ss";
        private static readonly Regex s_htmlTagRegex = new Regex("<[^>]*>", RegexOptions.Compiled);
        private static XmlSerializer s_serializer = new XmlSerializer(typeof(Feed));

        private readonly HttpClient _client;

        public RssWorker()
        {
            _client = new HttpClient();
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public async Task RunAsync(FeedInfo[] feeds)
        {
            foreach (var feedInfo in feeds)
            {
                try
                {
                    var feed = await GetFeedAsync(feedInfo);
                    if (feed.Channel.Items is not { Length: > 0 } items) continue;

                    int idx = Array.FindIndex(items, x => x.Guid == feedInfo.LatestGuid);

                    if (idx == -1)
                        idx = items.Length;

                    for (int i = idx - 1; i >= 0; i--)
                    {
                        try
                        {
                            await ProcessItem(feedInfo, feed.Channel, items[i]);

                            if (i != 0)
                            {
                                // Stay within webhook ratelimit
                                await Task.Delay(TimeSpan.FromSeconds(1));
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"{DateTimeOffset.Now.ToString(s_timestampFormat)} Error processing item {items[i].Guid}: {ex.Message}");
                        }
                    }

                    feedInfo.LatestGuid = items[0].Guid;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"{DateTimeOffset.Now.ToString(s_timestampFormat)} Error processing feed {feedInfo.FeedUri}: {ex.Message}");
                }
            }
        }

        private async Task<Feed> GetFeedAsync(FeedInfo feedInfo)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, feedInfo.FeedUri);

            if (feedInfo.FeedAuth is not null)
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(feedInfo.FeedAuth);

            using var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            using var stream = response.Content.ReadAsStream();
            var feed = (Feed)s_serializer.Deserialize(stream)!;

            return feed;
        }

        private async Task ProcessItem(FeedInfo info, Channel channel, Item item)
        {
            var message = new
            {
                username = channel.Title,
                embeds = new[]
                {
                    new
                    {
                        title = item.Title,
                        description = FormatDescription(item.Description, 350),
                        url = item.Link,
                        color = 0x00819C,
                        timestamp = DateTimeOffset.Parse(item.PubDateString).ToString("u"),
                        footer = new
                        {
                            text = $"{item.Category}",
                        },
                    }
                }
            };

            var json = JsonSerializer.Serialize(message);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync(info.WebhookUri, content);
            response.EnsureSuccessStatusCode();
        }

        private static string FormatDescription(string input, int maxLength)
        {
            var result = s_htmlTagRegex.Replace(input, "");
            result = HttpUtility.HtmlDecode(result);

            const string suff = "...";
            if (result.Length > maxLength)
                result = result[..(maxLength - suff.Length)] + suff;

            return result;
        }
    }
}

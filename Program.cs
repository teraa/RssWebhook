using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

var jsonSettings = new JsonSerializerOptions
{
    WriteIndented = true
};

string configFile = args[1];
var feedInfosJson = File.ReadAllText(configFile);
var feedInfos = JsonSerializer.Deserialize<FeedInfo[]>(feedInfosJson, jsonSettings);

using var client = new HttpClient();

var serializer = new XmlSerializer(typeof(Feed));

foreach (var feedInfo in feedInfos!)
{
    try
    {
        var feed = await GetFeed(feedInfo.FeedUri, feedInfo.FeedAuth);
        if (feed.Channel.Items is not { Length: > 0 } items) continue;

        int idx = Array.FindIndex(items, x => x.Guid == feedInfo.LatestGuid);

        if (idx == -1)
            idx = items.Length;

        for (int i = idx - 1; i >= 0; i--)
        {
            try
            {
                await ProcessItem(feedInfo, feed.Channel, items[i]);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error processing item ({items[i].Guid}): {ex}");
            }
        }

        feedInfo.LatestGuid = items[0].Guid;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error processing feed ({feedInfo.FeedUri}): {ex}");
    }
}

File.WriteAllText(configFile, JsonSerializer.Serialize(feedInfos, jsonSettings));

async Task<Feed> GetFeed(Uri uri, string? auth)
{
    var request = new HttpRequestMessage(HttpMethod.Get, uri);

    if (auth is not null)
        request.Headers.Authorization = AuthenticationHeaderValue.Parse(auth);

    var response = await client.SendAsync(request);
    response.EnsureSuccessStatusCode();

    var stream = response.Content.ReadAsStream();
    var feed = (Feed)serializer.Deserialize(stream)!;

    return feed;
}

async Task ProcessItem(FeedInfo info, Channel channel, Item item)
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

    var response = await client.PostAsync(info.WebhookUri, content);
    response.EnsureSuccessStatusCode();
    await Task.Delay(TimeSpan.FromSeconds(1));
}

static string FormatDescription(string input, int maxLength)
{
    var result = Regex.Replace(input, "<[^>]*>", "");
    result = HttpUtility.HtmlDecode(result);

    const string suff = "...";
    if (result.Length > maxLength)
        result = result[..(maxLength - suff.Length)] + suff;

    return result;
}

public class FeedInfo
{
    public Uri FeedUri { get; set; } = default!;
    public string? FeedAuth { get; set; } = default!;
    public Uri WebhookUri { get; set; } = default!;
    public string? LatestGuid { get; set; } = default!;
}

#nullable disable
[XmlRootAttribute("rss", IsNullable = false)]
public class Feed
{
    [XmlElementAttribute("channel", IsNullable = false)]
    public Channel Channel { get; init; }
}

public class Channel
{
    [XmlElement("title")]
    public string Title { get; init; }

    [XmlElement("link")]
    public string Link { get; init; }

    [XmlElement("description")]
    public string Description { get; init; }

    [XmlElement("ttl")]
    public int Ttl { get; init; }

    [XmlElement("item")]
    public Item[] Items { get; init; }
}

public class Item
{
    [XmlElement("title")]
    public string Title { get; init; }

    [XmlElement("pubDate")]
    public string PubDateString { get; init; }

    [XmlElement("category")]
    public string Category { get; init; }

    [XmlElement("description")]
    public string Description { get; init; }

    [XmlElement("link")]
    public string Link { get; init; }

    [XmlElement("guid")]
    public string Guid { get; init; }

    [XmlElementAttribute("creator", Namespace = "http://purl.org/dc/elements/1.1/")]
    public string Creator { get; init; }
}
#nullable restore

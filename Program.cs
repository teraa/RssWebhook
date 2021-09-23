using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace RssWebhook
{
    class Program
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        static async Task Main(string[] args)
        {

            if (args.Length != 1)
            {
                Console.Error.WriteLine($"Usage: <config>");
                return;
            }

            string configFile = args[0];
            string feedsJson = File.ReadAllText(configFile);
            var feeds = JsonSerializer.Deserialize<FeedInfo[]>(feedsJson, s_jsonOptions)!;

            using var worker = new RssWorker();
            await worker.RunAsync(feeds);

            feedsJson = JsonSerializer.Serialize(feeds, s_jsonOptions);
            File.WriteAllText(configFile, feedsJson);
        }
    }

    public class FeedInfo
    {
        public Uri FeedUri { get; set; } = default!;
        public string? FeedAuth { get; set; } = default!;
        public Uri WebhookUri { get; set; } = default!;
        public string? LatestGuid { get; set; } = default!;
    }
}

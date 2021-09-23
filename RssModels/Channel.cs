
using System.Xml.Serialization;

#nullable disable
namespace RssWebhook.RssModels
{
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
}
#nullable restore

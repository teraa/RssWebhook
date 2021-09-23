
using System.Xml.Serialization;

#nullable disable
namespace RssWebhook.RssModels
{
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
}
#nullable restore

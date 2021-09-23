
using System.Xml.Serialization;

#nullable disable
namespace RssWebhook.RssModels
{
    [XmlRootAttribute("rss", IsNullable = false)]
    public class Feed
    {
        [XmlElementAttribute("channel", IsNullable = false)]
        public Channel Channel { get; init; }
    }
}
#nullable restore

using myMangaSiteExtension;
using myMangaSiteExtension.Attributes;

namespace Batoto
{
    [ISiteExtensionDescription(
        "Batoto-German",
        "batoto.net",
        "http://www.batoto.net/",
        RootUrl = "http://www.batoto.net",
        Author = "James Parks",
        Version = "0.0.1",
        SupportedObjects = SupportedObjects.All,
        Language = "German")]
    public class Batoto_German : Batoto { }
}

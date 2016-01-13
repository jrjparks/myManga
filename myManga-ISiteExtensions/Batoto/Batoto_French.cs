using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Enums;

namespace Batoto
{
    [ISiteExtensionDescription(
        "Batoto-French",
        "batoto.net",
        "http://www.batoto.net/",
        RootUrl = "http://www.batoto.net",
        Author = "James Parks",
        Version = "0.0.1",
        SupportedObjects = SupportedObjects.All,
        Language = "French",
        RequiresAuthentication = true)]
    public sealed class Batoto_French : Batoto { }
}

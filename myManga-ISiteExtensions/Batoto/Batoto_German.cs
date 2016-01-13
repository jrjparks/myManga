using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Enums;

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
        Language = "German",
        RequiresAuthentication = true)]
    public sealed class Batoto_German : Batoto { }
}

using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Enums;

namespace Batoto
{
    [ISiteExtensionDescription(
        "Batoto-Italian",
        "batoto.net",
        "http://www.batoto.net/",
        RootUrl = "http://www.batoto.net",
        Author = "James Parks",
        Version = "0.0.1",
        SupportedObjects = SupportedObjects.All,
        Language = "Italian",
        RequiresAuthentication = true)]
    public sealed class Batoto_Italian : Batoto { }
}

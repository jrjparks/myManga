using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Enums;

namespace Batoto
{
    [ISiteExtensionDescription(
        "Batoto-Spanish",
        "batoto.net",
        "http://www.batoto.net/",
        RootUrl = "http://www.batoto.net",
        Author = "James Parks",
        Version = "0.0.1",
        SupportedObjects = SupportedObjects.All,
        Language = "Spanish",
        RequiresAuthentication = true)]
    public sealed class Batoto_Spanish : Batoto { }
}
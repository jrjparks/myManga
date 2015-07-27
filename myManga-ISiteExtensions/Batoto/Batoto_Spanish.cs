using myMangaSiteExtension;
using myMangaSiteExtension.Attributes;

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
        Language = "Spanish")]
    public sealed class Batoto_Spanish : Batoto { }
}
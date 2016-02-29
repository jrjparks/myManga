using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Enums;

namespace Batoto
{
    [IExtensionDescription(
        Name = "Batoto",
        URLFormat = "bato.to",
        RefererHeader = "https://bato.to/reader",
        RootUrl = "http://www.batoto.net",
        Author = "James Parks",
        Version = "0.0.1",
        SupportedObjects = SupportedObjects.All,
        Language = "Italian",
        RequiresAuthentication = true)]
    public sealed class Batoto_Italian : Batoto { }
}

using System;
using System.Diagnostics;
using System.Xml.Serialization;
using myMangaSiteExtension.Objects;

namespace myMangaSiteExtension.Collections
{
    [Serializable, XmlRoot, DebuggerStepThrough]
    public class MangaObjectCollection : GenericCollection<MangaObject>
    {
    }
}

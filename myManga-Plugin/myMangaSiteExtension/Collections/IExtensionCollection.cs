using myMangaSiteExtension.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace myMangaSiteExtension.Collections
{
    [DebuggerStepThrough]
    public class IExtensionCollection<ExtensionType> : GenericCollection<ExtensionType>
        where ExtensionType : IExtension
    {
        public virtual ExtensionType this[String Name]
        {
            get { return innerList[IndexOf(Name)]; }
            set { innerList[IndexOf(Name)] = value; }
        }

        public virtual ExtensionType this[String Name, String Language]
        {
            get { return innerList[IndexOf(Name, Language)]; }
            set { innerList[IndexOf(Name, Language)] = value; }
        }

        public virtual Int32 IndexOf(String Name)
        {
            ExtensionType Extension = innerList.FirstOrDefault(_ =>
                Equals(_.ExtensionDescriptionAttribute.Name, Name));
            if (Equals(Extension, null)) return -1;
            return innerList.IndexOf(Extension);
        }

        public virtual Int32 IndexOf(String Name, String Language)
        {
            ExtensionType Extension = innerList.FirstOrDefault(_ =>
                Equals(_.ExtensionDescriptionAttribute.Name, Name)
                && Equals(_.ExtensionDescriptionAttribute.Language, Language));
            if (Equals(Extension, null)) return -1;
            return innerList.IndexOf(Extension);
        }

        public virtual Boolean Contains(String Name)
        { return IndexOf(Name) >= 0; }

        public virtual Boolean Contains(String Name, String Language)
        { return IndexOf(Name, Language) >= 0; }

        public IExtensionCollection() : base() { }
        public IExtensionCollection(int capacity) : base(capacity: capacity) { }
        public IExtensionCollection(IEnumerable<ExtensionType> collection) : base(collection: collection) { }
    }
}

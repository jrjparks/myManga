using System;
using System.Reflection;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Interfaces;

namespace myMangaSiteExtension.Collections
{
    public class IDatabaseExtensionCollection : GenericCollection<IDatabaseExtension>
    {
        public virtual IDatabaseExtension this[String name]
        {
            get { return innerList[IndexOf(name)]; }
            set { innerList[IndexOf(name)] = value; }
        }

        public virtual Int32 IndexOf(String name)
        {
            foreach (IDatabaseExtension siteExtensionItem in innerList)
            {
                IDatabaseExtensionAttribute siteExtensionAttribute = siteExtensionItem.GetType().GetCustomAttribute<IDatabaseExtensionAttribute>(true);
                if (siteExtensionAttribute.Name.Equals(name))
                    return innerList.IndexOf(siteExtensionItem);
            }
            return -1;
        }

        public virtual Boolean Contains(String name)
        {
            return IndexOf(name) >= 0;
        }
    }
}

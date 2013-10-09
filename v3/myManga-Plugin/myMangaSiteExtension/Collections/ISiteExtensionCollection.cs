using System;
using System.Reflection;
using myMangaSiteExtension.Attributes.ISiteExtension;

namespace myMangaSiteExtension.Collections
{
    public class ISiteExtensionCollection : GenericCollection<ISiteExtension>
    {
        public virtual ISiteExtension this[String name]
        {
            get { return innerList[IndexOf(name)]; }
            set { innerList[IndexOf(name)] = value; }
        }

        public virtual Int32 IndexOf(String name)
        {
            foreach (ISiteExtension siteExtensionItem in innerList)
            {
                ISiteExtensionDescriptionAttribute siteExtensionAttribute = siteExtensionItem.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(true);
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

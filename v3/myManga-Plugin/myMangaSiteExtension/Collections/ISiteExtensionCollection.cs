using System;
using System.Collections;
using System.Collections.Generic;
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
                MemberInfo siteExtensionInfo = siteExtensionItem.GetType();
                ISiteExtensionAttribute siteExtensionAttribute = siteExtensionInfo.GetCustomAttribute<ISiteExtensionAttribute>(true);
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

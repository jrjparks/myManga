using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace myMangaSiteExtension.Attributes
{
    public static class AttributesHelper
    {
        public static T GetAttribute<T>(this myMangaSiteExtension.ISiteExtension element, Type attributeType, Boolean inherit) where T : System.Attribute
        {
            return (T)Attribute.GetCustomAttribute(element.GetType() as MemberInfo, attributeType, inherit);
        }
    }
}

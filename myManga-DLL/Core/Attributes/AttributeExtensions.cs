using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Core.Attributes
{
    public static class AttributeExtensions
    {
        public static T[] GetAttributeOfType<T>(this object value, bool inherit = false) where T : System.Attribute
        {
            MemberInfo info = value.GetType();
            IEnumerable<T> attributes = info.GetCustomAttributes<T>(inherit);
            return value.GetType().GetAttributeOfType<T>(inherit);
        }

        public static T[] GetAttributeOfEnum<T>(this Enum value, bool inherit = false) where T : System.Attribute
        {
            MemberInfo[] info = value.GetType().GetMember(value.ToString());
            IEnumerable<T> attributes = info[0].GetCustomAttributes<T>(inherit);
            return attributes.ToArray();
        }
    }
}

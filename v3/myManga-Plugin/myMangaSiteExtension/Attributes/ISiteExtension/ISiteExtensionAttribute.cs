using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myMangaSiteExtension.Attributes.ISiteExtension
{
    [DebuggerStepThrough, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ISiteExtensionDescriptionAttribute : Attribute
    {
        /// <summary>
        /// Name of the supported site.
        /// </summary>
        public String Name;
        /// <summary>
        /// Author fo the ISiteExtension. (Your name)
        /// </summary>
        public String Author = "";
        /// <summary>
        /// Version of the ISiteExtension
        /// </summary>
        public String Version = "0.0.0";

        public String URLFormat;
        // This is not a misspelling. https://en.wikipedia.org/wiki/HTTP_referer
        public String RefererHeader;
        public String RootUrl;
        public SupportedObjects SupportedObjects = SupportedObjects.None;

        public String Language;

        /// <summary>
        /// ISiteExtension attribute.
        /// </summary>
        /// <param name="Name">Name of the site</param>
        /// <param name="URLFormat">Format of the sites url</param>
        /// <param name="RefererHeader">Referer header to use when connecting to the site</param>
        public ISiteExtensionDescriptionAttribute(String Name, String URLFormat, String RefererHeader)
        {
            this.Name = Name;
            this.URLFormat = URLFormat;
            this.RefererHeader = RefererHeader;
        }

        public override string ToString()
        {
            return String.Format("{0} by {1} version {2}", Name, Author, Version);
        }
    }
}

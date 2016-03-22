using myMangaSiteExtension.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myMangaSiteExtension.Attributes
{
    [DebuggerStepThrough, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class IExtensionDescriptionAttribute : Attribute
    {
        /// <summary>
        /// Name of the supported site.
        /// </summary>
        public String Name;

        /// <summary>
        /// Language of supported site.
        /// </summary>
        public String Language;

        /// <summary>
        /// Author fo the extension. (Your name)
        /// </summary>
        public String Author = "";

        /// <summary>
        /// Version of the extension
        /// </summary>
        public String Version = "0.0.0";

        /// <summary>
        /// Is authentication required.
        /// </summary>
        public Boolean RequiresAuthentication = false;

        public String URLFormat;
        // This is not a misspelling. https://en.wikipedia.org/wiki/HTTP_referer
        public String RefererHeader;
        public String RootUrl;

        public SupportedObjects SupportedObjects = SupportedObjects.None;

        public override string ToString()
        {
            return String.Format("{0} by {1} version {2}", Name, Author, Version);
        }
    }
}

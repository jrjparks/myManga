using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myMangaSiteExtension.Attributes.ISiteExtension
{
    [DebuggerStepThrough, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ISiteExtensionAttribute : Attribute
    {
        public String Name { get; private set; }
        public String Author { get; private set; }
        public String Version { get; private set; }

        public String URLFormat { get; private set; }
        // This is not a misspelling. https://en.wikipedia.org/wiki/HTTP_referer
        public String RefererHeader { get; private set; }
        public SupportedObjects SupportedObjects { get; private set; }
        
        public ISiteExtensionAttribute(String Name, String Author = "", String Version = "", SupportedObjects SupportedObjects = myMangaSiteExtension.SupportedObjects.None)
        {
            this.Name = Name;
            this.Author = Author;
            this.Version = Version;
            this.SupportedObjects = SupportedObjects;
        }

        public override string ToString()
        {
            return String.Format("{0} by {1} version {2}", Name, Author, Version);
        }
    }
}

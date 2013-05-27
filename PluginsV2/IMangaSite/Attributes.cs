using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace IMangaSite
{
    [DebuggerStepThrough]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class IMangaSiteDataAttribute : Attribute
    {
        public String Name { get; set; }
        public String Author { get; set; }
        public String Version { get; set; }

        public String UrlFormat { get; set; }
        public String RefererHeader { get; set; }
        public SupportedMethods SupportedMethods { get; set; }

        public IMangaSiteDataAttribute(String Name, String Author, String Version, String UrlFormat, String RefererHeader)
            : this(Name, Author, Version, UrlFormat, RefererHeader, global::IMangaSite.SupportedMethods.None)
        { }
        public IMangaSiteDataAttribute(String Name, String Author, String Version, String UrlFormat, String RefererHeader, SupportedMethods SupportedMethods)
        {
            this.Name = Name;
            this.Author = Author;
            this.Version = Version;
            this.UrlFormat = UrlFormat;
            this.RefererHeader = RefererHeader;
            this.SupportedMethods = SupportedMethods;

        }
    }
}

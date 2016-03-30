using myMangaSiteExtension.Primitives.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace myMangaSiteExtension.Objects
{
    [DebuggerStepThrough]
    public class SearchResultObject
    {
        public String Id { get; set; }
        public Double Rating { get; set; }
        public String Name { get; set; }
        public String Description { get; set; }
        public String ExtensionName { get; set; }
        public String ExtensionLanguage { get; set; }        
        public String Url { get; set; }
        public LocationObject Cover { get; set; }
        public List<String> Authors { get; set; }
        public List<String> Artists { get; set; }
    }
}

using myMangaSiteExtension.Primitives.Objects;
using myMangaSiteExtension.Enums;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace myMangaSiteExtension.Objects
{
    [DebuggerStepThrough]
    public class SearchRequestObject
    {
        public String Url { get; set; }
        public String Referer { get; set; }
        public String RequestContent { get; set; }
        public SearchMethod Method { get; set; }
    }
}

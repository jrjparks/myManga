using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Core.IO;

namespace myManga_App.Objects
{
    [Serializable, XmlRoot, DebuggerStepThrough]
    public sealed class UserConfigurationObject : SerializableObject
    {
        [XmlIgnore]
        private List<String> enabledSiteExtentions;
        [XmlArray, XmlArrayItem("SiteExtentionName")]
        public List<String> EnabledSiteExtentions { get { return enabledSiteExtentions ?? (enabledSiteExtentions = new List<String>()); } }

        public UserConfigurationObject() : base() { }
        public UserConfigurationObject(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}

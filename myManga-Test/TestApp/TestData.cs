using myMangaSiteExtension.Primitives.Objects;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace TestApp
{
    [Serializable, XmlRoot("TestData"), DebuggerStepThrough]
    public class TestData : SerializableObject
    {
        public TestData() { }

        protected TestData(SerializationInfo info, StreamingContext context) :
            base(info, context) { }

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected String testString;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected UInt32 testUInt32;

        [XmlAttribute("TestString")]
        public String TestString
        {
            get { return testString; }
            set
            {
                testString = value;
            }
        }
        [XmlAttribute("TestUInt32")]
        public UInt32 TestUInt32
        {
            get { return testUInt32; }
            set
            {
                testUInt32 = value;
            }
        }
    }
}

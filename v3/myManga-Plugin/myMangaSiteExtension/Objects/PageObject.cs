using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Core.IO;
using System.Windows;

namespace myMangaSiteExtension.Objects
{
    [Serializable, XmlRoot, DebuggerStepThrough]
    public class PageObject : SerializableObject
    {
        #region Protected
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected String name;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected UInt32 page_number;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected List<String> remote_locations;
        #endregion

        #region Public
        [NonSerialized, XmlIgnore]
        public readonly ChapterObject ParentChapterObject;

        public static readonly DependencyProperty NameProperty = DependencyProperty.Register("Name", typeof(String), typeof(PageObject));
        [XmlAttribute]
        public String Name
        {
            get { return name; }
            set { name = value; }
        }

        public static readonly DependencyProperty PageNumberProperty = DependencyProperty.Register("PageNumber", typeof(UInt32), typeof(MangaObject));
        [XmlAttribute]
        public UInt32 PageNumber
        {
            get { return page_number; }
            set { page_number = value; }
        }

        public static readonly DependencyProperty RemoteLocationsProperty = DependencyProperty.Register("RemoteLocations", typeof(List<String>), typeof(MangaObject));
        [XmlElement]
        public List<String> RemoteLocations
        {
            get { return remote_locations; }
            set { remote_locations = value; }
        }

        public PageObject() : base() { }
        public PageObject(ChapterObject ChapterObject) : this() { ParentChapterObject = ChapterObject; }
        public PageObject(SerializationInfo info, StreamingContext context) : base(info, context) { }
        #endregion
    }
}

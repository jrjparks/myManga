using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Core.IO;
using myMangaSiteExtension.Collections;
using System.Windows;

namespace myMangaSiteExtension.Objects
{
    [Serializable, XmlRoot, DebuggerStepThrough]
    public class ChapterObject : SerializableObject
    {
        #region Protected
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected String name;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected List<Core.IO.KeyValuePair<String, String>> locations;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected PageObjectCollection pages;
        #endregion

        #region Public
        [NonSerialized, XmlIgnore]
        public readonly MangaObject ParentMangaObject;

        public static readonly DependencyProperty NameProperty = DependencyProperty.Register("Name", typeof(String), typeof(ChapterObject));
        [XmlAttribute]
        public String Name
        {
            get { return name; }
            set { name = value; }
        }

        public static readonly DependencyProperty LocationsProperty = DependencyProperty.Register("Locations", typeof(List<Core.IO.KeyValuePair<String, String>>), typeof(ChapterObject));
        [XmlArray, XmlArrayItem]
        public List<Core.IO.KeyValuePair<String, String>> Locations
        {
            get { return locations ?? (locations = new List<Core.IO.KeyValuePair<string, string>>()); }
            set { locations = value; }
        }

        [XmlArray, XmlArrayItem]
        public PageObjectCollection Pages
        {
            get { return pages ?? (pages = new PageObjectCollection()); }
            set { pages = value; }
        }

        public ChapterObject() : base() { }
        public ChapterObject(MangaObject MangaObject) : this() { ParentMangaObject = MangaObject; }
        public ChapterObject(SerializationInfo info, StreamingContext context) : base(info, context) { }
        #endregion
    }
}
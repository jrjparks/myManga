using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Windows;
using System.Xml.Serialization;
using Core.IO;
using myMangaSiteExtension.Collections;

namespace myMangaSiteExtension.Objects
{
    [Serializable, XmlRoot, DebuggerStepThrough]
    public class MangaObject : SerializableObject
    {
        #region Protected
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected String name;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected List<String> alternate_names;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected List<String> authors;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected List<String> artists;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected List<String> genres;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected FlowDirection pageFlowDirection;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected Dictionary<String, String> locations;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected List<String> covers;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected ChapterObjectCollection chapters;
        #endregion

        #region Public
        public static readonly DependencyProperty NameProperty = DependencyProperty.Register("Name", typeof(String), typeof(MangaObject));
        [XmlAttribute]
        public String Name
        {
            get { return name; }
            set { name = value; }
        }

        public static readonly DependencyProperty AlternateNamesProperty = DependencyProperty.Register("AlternateNames", typeof(List<String>), typeof(MangaObject));
        [XmlArray, XmlArrayItem("Name")]
        public List<String> AlternateNames
        {
            get { return alternate_names ?? (alternate_names = new List<String>()); }
            set { alternate_names = value; }
        }

        public static readonly DependencyProperty AuthorsProperty = DependencyProperty.Register("Authors", typeof(List<String>), typeof(MangaObject));
        [XmlArray, XmlArrayItem("Name")]
        public List<String> Authors
        {
            get { return authors ?? (authors = new List<String>()); }
            set { authors = value; }
        }

        public static readonly DependencyProperty ArtistsProperty = DependencyProperty.Register("Artists", typeof(List<String>), typeof(MangaObject));
        [XmlArray, XmlArrayItem("Name")]
        public List<String> Artists
        {
            get { return artists ?? (artists = new List<String>()); }
            set { artists = value; }
        }

        public static readonly DependencyProperty GenresProperty = DependencyProperty.Register("Genres", typeof(List<String>), typeof(MangaObject));
        [XmlArray, XmlArrayItem("Name")]
        public List<String> Genres
        {
            get { return genres ?? (genres = new List<String>()); }
            set { genres = value; }
        }

        public static readonly DependencyProperty PageFlowDirectionProperty = DependencyProperty.Register("PageFlowDirection", typeof(FlowDirection), typeof(MangaObject));
        [XmlAttribute]
        public FlowDirection PageFlowDirection
        {
            get { return pageFlowDirection; }
            set { pageFlowDirection = value; }
        }

        public static readonly DependencyProperty CoversProperty = DependencyProperty.Register("Covers", typeof(List<String>), typeof(MangaObject));
        [XmlArray, XmlArrayItem("Cover")]
        public List<String> Covers
        {
            get { return covers ?? (covers = new List<String>()); }
            set { covers = value; }
        }

        public static readonly DependencyProperty ChaptersProperty = DependencyProperty.Register("Chapters", typeof(ChapterObjectCollection), typeof(MangaObject));
        [XmlArray, XmlArrayItem]
        public ChapterObjectCollection Chapters
        {
            get { return chapters ?? (chapters = new ChapterObjectCollection()); }
            set { chapters = new ChapterObjectCollection(); }
        }

        public MangaObject() : base() { }
        public MangaObject(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
        #endregion
    }
}

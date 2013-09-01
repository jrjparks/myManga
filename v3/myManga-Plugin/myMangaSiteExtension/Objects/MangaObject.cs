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
    public class MangaObject : SerializableObject, INotifyPropertyChanging, INotifyPropertyChanged
    {
        #region NotifyPropertyChange
        public event PropertyChangingEventHandler PropertyChanging;
        protected void OnPropertyChanging([CallerMemberName] String caller = "")
        {
            if (PropertyChanging != null)
                PropertyChanging(this, new PropertyChangingEventArgs(caller));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] String caller = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }
        #endregion

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
        [XmlAttribute]
        public String Name
        {
            get { return name; }
            set
            {
                OnPropertyChanging();
                name = value;
                OnPropertyChanged();
            }
        }

        [XmlArray, XmlArrayItem("Name")]
        public List<String> AlternateNames
        {
            get
            {
                if (alternate_names == null)
                    alternate_names = new List<String>();
                return alternate_names;
            }
            set
            {
                OnPropertyChanging();
                alternate_names = value;
                OnPropertyChanged();
            }
        }

        [XmlArray, XmlArrayItem("Name")]
        public List<String> Authors
        {
            get
            {
                if (authors == null)
                    authors = new List<String>();
                return authors;
            }
            set
            {
                OnPropertyChanging();
                authors = value;
                OnPropertyChanged();
            }
        }

        [XmlArray, XmlArrayItem("Name")]
        public List<String> Artists
        {
            get
            {
                if (artists == null)
                    artists = new List<String>();
                return artists;
            }
            set
            {
                OnPropertyChanging();
                artists = value;
                OnPropertyChanged();
            }
        }

        [XmlArray, XmlArrayItem("Name")]
        public List<String> Genres
        {
            get
            {
                if (genres == null)
                    genres = new List<String>();
                return genres;
            }
            set
            {
                OnPropertyChanging();
                genres = value;
                OnPropertyChanged();
            }
        }

        [XmlAttribute]
        public FlowDirection PageFlowDirection
        {
            get { return pageFlowDirection; }
            set
            {
                OnPropertyChanging();
                pageFlowDirection = value;
                OnPropertyChanged();
            }
        }

        [XmlArray, XmlArrayItem("Cover")]
        public List<String> Covers
        {
            get
            {
                if (covers == null)
                    covers = new List<String>();
                return covers;
            }
            set
            {
                OnPropertyChanging();
                covers = value;
                OnPropertyChanged();
            }
        }

        [XmlArray, XmlArrayItem]
        public ChapterObjectCollection Chapters
        {
            get
            {
                if (chapters == null)
                    chapters = new ChapterObjectCollection();
                return chapters;
            }
            set
            {
                OnPropertyChanging();
                chapters = value;
                OnPropertyChanged();
            }
        }

        public MangaObject() : base() { }
        public MangaObject(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
        #endregion
    }
}

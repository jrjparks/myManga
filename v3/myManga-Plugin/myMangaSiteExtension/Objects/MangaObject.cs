using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Windows;
using System.Xml.Serialization;
using Core.IO;
using myMangaSiteExtension.Enums;

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
        protected MangaObjectType mangaType;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected String name;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected List<String> alternate_names;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected String description;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected List<String> authors;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected List<String> artists;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected List<String> genres;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected FlowDirection pageFlowDirection;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected List<LocationObject> locations;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected List<LocationObject> databaseLocations;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected List<String> covers;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected Int32 preferredcover;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected List<ChapterObject> chapters;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected DateTime released = DateTime.MinValue;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected Double rating;
        #endregion

        #region Public
        [XmlAttribute]
        public MangaObjectType MangaType
        {
            get { return mangaType; }
            set
            {
                OnPropertyChanging();
                mangaType = value;
                OnPropertyChanged();
            }
        }

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
            get { return alternate_names ?? (alternate_names = new List<String>()); }
            set
            {
                OnPropertyChanging();
                alternate_names = value;
                OnPropertyChanged();
            }
        }

        [XmlAttribute]
        public String Description
        {
            get { return description; }
            set
            {
                OnPropertyChanging();
                description = value;
                OnPropertyChanged();
            }
        }

        [XmlArray, XmlArrayItem("Name")]
        public List<String> Authors
        {
            get { return authors ?? (authors = new List<String>()); }
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
            get { return artists ?? (artists = new List<String>()); }
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
            get { return genres ?? (genres = new List<String>()); }
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

        [XmlArray, XmlArrayItem("Location")]
        public List<LocationObject> Locations
        {
            get { return locations ?? (locations = new List<LocationObject>()); }
            set
            {
                OnPropertyChanging();
                locations = value;
                OnPropertyChanged();
            }
        }

        [XmlArray, XmlArrayItem("Location")]
        public List<LocationObject> DatabaseLocations
        {
            get { return databaseLocations ?? (databaseLocations = new List<LocationObject>()); }
            set
            {
                OnPropertyChanging();
                databaseLocations = value;
                OnPropertyChanged();
            }
        }

        [XmlArray, XmlArrayItem("Cover")]
        public List<String> Covers
        {
            get { return covers ?? (covers = new List<String>()); }
            set
            {
                OnPropertyChanging();
                covers = value;
                OnPropertyChanged();
            }
        }

        [XmlAttribute]
        public Int32 PreferredCover
        {
            get { return preferredcover; }
            set
            {
                OnPropertyChanging();
                preferredcover = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        public String SelectedCover
        {
            get { return Covers.Count > PreferredCover ? Covers[PreferredCover] : Covers.FirstOrDefault(); }
        }

        [XmlArray, XmlArrayItem]
        public List<ChapterObject> Chapters
        {
            get { return chapters ?? (chapters = new List<ChapterObject>()); }
            set
            {
                OnPropertyChanging();
                chapters = value;
                OnPropertyChanged();
            }
        }

        public DateTime Released
        {
            get { return released; }
            set
            {
                OnPropertyChanging();
                released = value;
                OnPropertyChanged();
            }
        }

        public Double Rating
        {
            get { return rating; }
            set
            {
                OnPropertyChanging();
                rating = value;
                OnPropertyChanged();
            }
        }
        [XmlIgnore]
        public bool RatingSpecified { get { return this.Rating >= 0; } }

        public MangaObject() : base() { }
        public MangaObject(SerializationInfo info, StreamingContext context) : base(info, context) { }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Core.IO;

namespace myMangaSiteExtension.Objects
{
    [Serializable, XmlRoot, DebuggerStepThrough]
    public class SearchResultObject : SerializableObject, INotifyPropertyChanging, INotifyPropertyChanged
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
        protected Int32 id;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected String url;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected String coverurl;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected List<String> authors;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected List<String> artists;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected Double rating;
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

        [XmlAttribute]
        public Int32 Id
        {
            get { return id; }
            set
            {
                OnPropertyChanging();
                id = value;
                OnPropertyChanged();
            }
        }

        [XmlAttribute]
        public String Url
        {
            get { return url; }
            set
            {
                OnPropertyChanging();
                url = value;
                OnPropertyChanged();
            }
        }

        [XmlAttribute]
        public String CoverUrl
        {
            get { return coverurl; }
            set
            {
                OnPropertyChanging();
                coverurl = value;
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

        public SearchResultObject() : base() { }
        public SearchResultObject(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
        #endregion
    }
}

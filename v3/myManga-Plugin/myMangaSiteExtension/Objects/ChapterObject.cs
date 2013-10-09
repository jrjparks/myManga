using Core.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace myMangaSiteExtension.Objects
{
    [Serializable, XmlRoot, DebuggerStepThrough]
    public class ChapterObject : SerializableObject, INotifyPropertyChanging, INotifyPropertyChanged
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
        protected Int32 volume;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected Int32 chapter;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected Int32 subchapter;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected List<LocationObject> locations;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected List<PageObject> pages;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected DateTime released = DateTime.MinValue;
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
        public Int32 Volume
        {
            get { return volume; }
            set
            {
                OnPropertyChanging();
                volume = value;
                OnPropertyChanged();
            }
        }

        [XmlAttribute]
        public Int32 Chapter
        {
            get { return chapter; }
            set
            {
                OnPropertyChanging();
                chapter = value;
                OnPropertyChanged();
            }
        }

        [XmlAttribute]
        public Int32 SubChapter
        {
            get { return subchapter; }
            set
            {
                OnPropertyChanging();
                subchapter = value;
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

        [XmlArray, XmlArrayItem]
        public List<PageObject> Pages
        {
            get { return pages ?? (pages = new List<PageObject>()); }
            set
            {
                OnPropertyChanging();
                pages = value;
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

        public ChapterObject() : base() { }
        public ChapterObject(SerializationInfo info, StreamingContext context) : base(info, context) { }
        #endregion
    }
}
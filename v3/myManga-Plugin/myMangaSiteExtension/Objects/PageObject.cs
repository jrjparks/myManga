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
    public class PageObject : SerializableObject, INotifyPropertyChanging, INotifyPropertyChanged
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
        protected UInt32 page_number;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected List<String> remote_locations;
        #endregion

        #region Public
        [NonSerialized, XmlIgnore]
        public readonly ChapterObject ParentChapterObject;

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
        public UInt32 PageNumber
        {
            get { return page_number; }
            set
            {
                OnPropertyChanging();
                page_number = value;
                OnPropertyChanged();
            }
        }
        [XmlElement]
        public List<String> RemoteLocations
        {
            get { return remote_locations; }
            set
            {
                OnPropertyChanging();
                remote_locations = value;
                OnPropertyChanged();
            }
        }

        public PageObject() : base() { }
        public PageObject(ChapterObject ChapterObject) : this() { ParentChapterObject = ChapterObject; }
        public PageObject(SerializationInfo info, StreamingContext context) : base(info, context) { }
        #endregion
    }
}
using Core.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Core.IO;
using myMangaSiteExtension.Collections;
using System.Runtime.Serialization;

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
        protected Int32 chapter;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected Int32 subchapter;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected List<Core.IO.KeyValuePair<String, String>> locations;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected List<PageObject> pages;

        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected DateTime released = DateTime.MinValue;
        #endregion

        #region Public
        [NonSerialized, XmlIgnore]
        public readonly MangaObject ParentMangaObject;

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
        public List<Core.IO.KeyValuePair<String, String>> Locations
        {
            get { return locations ?? (locations = new List<Core.IO.KeyValuePair<String, String>>()); }
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
        public ChapterObject(MangaObject MangaObject) : this() { ParentMangaObject = MangaObject; }
        public ChapterObject(SerializationInfo info, StreamingContext context) : base(info, context) { }
        #endregion
    }
}
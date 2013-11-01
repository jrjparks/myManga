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

namespace myMangaSiteExtension.Objects
{
    [Serializable, XmlRoot, DebuggerStepThrough]
    public class BookmarkObject : SerializableObject, INotifyPropertyChanging, INotifyPropertyChanged
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
        protected Int32 volume;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected Int32 chapter;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected Int32 subchapter;
        #endregion

        #region Public
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
        #endregion
    }
}

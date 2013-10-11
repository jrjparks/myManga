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
    public class LocationObject : SerializableObject, INotifyPropertyChanging, INotifyPropertyChanged
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
        protected String extensionname;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected String url;
        [NonSerialized, XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected Boolean enabled = true;
        #endregion

        #region Public
        [XmlAttribute]
        public String ExtensionName
        {
            get { return extensionname; }
            set
            {
                OnPropertyChanging();
                extensionname = value;
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
        public Boolean Enabled
        {
            get { return enabled; }
            set
            {
                OnPropertyChanging();
                enabled = value;
                OnPropertyChanged();
            }
        }

        public LocationObject() : base() { }
        public LocationObject(SerializationInfo info, StreamingContext context) : base(info, context) { }
        public override string ToString()
        {
            return String.Format("{0}[{2}]: {1}", ExtensionName, Url, Enabled ? "Enabled" : "Disabled");
        }
        #endregion
    }
}
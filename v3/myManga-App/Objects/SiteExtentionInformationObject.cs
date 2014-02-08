using Core.IO;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using myMangaSiteExtension.Attributes;
using System.Runtime.Serialization;
using myMangaSiteExtension;

namespace myManga_App.Objects
{
    [Serializable, XmlRoot, DebuggerStepThrough]
    public class SiteExtentionInformationObject : SerializableObject, INotifyPropertyChanging, INotifyPropertyChanged
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

        #region Private
        [XmlIgnore]
        private ISiteExtensionDescriptionAttribute iSiteExtensionDescriptionAttribute;

        [XmlIgnore]
        private Boolean enabled;
        #endregion

        #region Public
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
        [XmlAttribute]
        public String Name
        {
            get { return iSiteExtensionDescriptionAttribute.Name; }
            set
            {
                OnPropertyChanging();
                iSiteExtensionDescriptionAttribute.Name = value;
                OnPropertyChanged();
            }
        }
        [XmlAttribute]
        public String Version
        {
            get { return iSiteExtensionDescriptionAttribute.Version; }
            set
            {
                OnPropertyChanging();
                iSiteExtensionDescriptionAttribute.Version = value;
                OnPropertyChanged();
            }
        }
        [XmlAttribute]
        public String Author
        {
            get { return iSiteExtensionDescriptionAttribute.Author; }
            set
            {
                OnPropertyChanging();
                iSiteExtensionDescriptionAttribute.Author = value;
                OnPropertyChanged();
            }
        }
        [XmlAttribute]
        public SupportedObjects SupportedObjects
        {
            get { return iSiteExtensionDescriptionAttribute.SupportedObjects; }
            set
            {
                OnPropertyChanging();
                iSiteExtensionDescriptionAttribute.SupportedObjects = value;
                OnPropertyChanged();
            }
        }

        public SiteExtentionInformationObject() : base() { }
        public SiteExtentionInformationObject(ISiteExtensionDescriptionAttribute attribute) : base() { iSiteExtensionDescriptionAttribute = attribute; }
        public SiteExtentionInformationObject(SerializationInfo info, StreamingContext context) : base(info, context) { }
        #endregion
    }
}

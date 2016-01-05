using Core.IO;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Enums;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace myManga_App.Objects
{
    [Serializable, XmlRoot, DebuggerStepThrough]
    public class DatabaseExtensionInformationObject : SerializableObject, INotifyPropertyChanging, INotifyPropertyChanged
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
        private IDatabaseExtensionDescriptionAttribute iDatabaseExtensionAttribute;

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
            get { return iDatabaseExtensionAttribute.Name; }
            set
            {
                OnPropertyChanging();
                iDatabaseExtensionAttribute.Name = value;
                OnPropertyChanged();
            }
        }
        [XmlAttribute]
        public String Version
        {
            get { return iDatabaseExtensionAttribute.Version; }
            set
            {
                OnPropertyChanging();
                iDatabaseExtensionAttribute.Version = value;
                OnPropertyChanged();
            }
        }
        [XmlAttribute]
        public String Author
        {
            get { return iDatabaseExtensionAttribute.Author; }
            set
            {
                OnPropertyChanging();
                iDatabaseExtensionAttribute.Author = value;
                OnPropertyChanged();
            }
        }
        [XmlAttribute]
        public SupportedObjects SupportedObjects
        {
            get { return iDatabaseExtensionAttribute.SupportedObjects; }
            set
            {
                OnPropertyChanging();
                iDatabaseExtensionAttribute.SupportedObjects = value;
                OnPropertyChanged();
            }
        }

        public DatabaseExtensionInformationObject() : base() { }
        public DatabaseExtensionInformationObject(IDatabaseExtensionDescriptionAttribute attribute)
            : base()
        { iDatabaseExtensionAttribute = attribute; }
        protected DatabaseExtensionInformationObject(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        { base.GetObjectData(info, context); }
        #endregion
    }
}

using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Primitives.Objects;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace myManga_App.Objects.UserConfig
{
    [Serializable, XmlRoot, DebuggerStepThrough]
    public sealed class EnabledExtensionObject : SerializableObject, INotifyPropertyChanging, INotifyPropertyChanged
    {
        #region NotifyPropertyChange
        public event PropertyChangingEventHandler PropertyChanging;
        private void OnPropertyChanging([CallerMemberName] String caller = "")
        {
            if (PropertyChanging != null)
                PropertyChanging(this, new PropertyChangingEventArgs(caller));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] String caller = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(caller));=
        }
        #endregion

        #region Private
        [XmlIgnore]
        private String name;
        [XmlIgnore]
        private String language;
        [XmlIgnore]
        private Boolean enabled = false;
        [XmlIgnore]
        private String extensiontype = false;
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
        public String Language
        {
            get { return language; }
            set
            {
                OnPropertyChanging();
                language = value;
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
        [XmlAttribute]
        public String ExtensionType
        {
            get { return extensiontype; }
            set
            {
                OnPropertyChanging();
                extensiontype = value;
                OnPropertyChanged();
            }
        }
        #endregion

        public EnabledExtensionObject() { }
        public EnabledExtensionObject(IExtension Extension)
        {
            Name = Extension.ExtensionDescriptionAttribute.Name;
            Language = Extension.ExtensionDescriptionAttribute.Language;
            ExtensionType = Extension.GetType().Name;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        { base.GetObjectData(info, context); }
    }
}

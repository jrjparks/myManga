using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Primitives.Objects;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace myManga_App.Objects.UserConfig
{
    [Serializable, XmlRoot]//, DebuggerStepThrough]
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
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
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
        private String extensiontype = String.Empty;
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
            Type InterfaceType = Enumerable.FirstOrDefault(Extension.GetType().GetInterfaces());
            if (!Equals(InterfaceType, null)) ExtensionType = InterfaceType.Name;
            else ExtensionType = "UnknownExtension";
        }
        private EnabledExtensionObject(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        public override string ToString()
        {
            return String.Format("[{2}]{0} ({1})", Name, Language, ExtensionType);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        { base.GetObjectData(info, context); }

        public override Boolean Equals(Object obj)
        {
            EnabledExtensionObject ExtObj = (obj as EnabledExtensionObject);
            if (Equals(ExtObj, null)) return false;
            if (!Equals(Name, ExtObj.Name)) return false;
            if (!Equals(Language, ExtObj.Language)) return false;
            if (!Equals(ExtensionType, ExtObj.ExtensionType)) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode()
                & Name.GetHashCode()
                & Language.GetHashCode()
                & ExtensionType.GetHashCode();
        }

        public Boolean EqualsIExtension(IExtension Extension)
        {
            if (Equals(Extension, null)) return false;
            String ExtType = "UnknownExtension";
            Type InterfaceType = Enumerable.FirstOrDefault(Extension.GetType().GetInterfaces());
            if (!Equals(InterfaceType, null)) ExtType = InterfaceType.Name;

            if (!Equals(Name, Extension.ExtensionDescriptionAttribute.Name)) return false;
            if (!Equals(Language, Extension.ExtensionDescriptionAttribute.Language)) return false;
            if (!Equals(ExtensionType, ExtType)) return false;
            return true;
        }

        public Boolean EqualsLocationObject(LocationObject Location)
        {
            if (Equals(Location, null)) return false;
            if (!Equals(Name, Location.ExtensionName)) return false;
            if (!Equals(Location.ExtensionLanguage, null)) // Only check language if location has one.
                if (!Equals(Language, Location.ExtensionLanguage)) return false;
            return true;
        }

        public static Boolean operator ==(EnabledExtensionObject A, EnabledExtensionObject B)
        {
            if (ReferenceEquals(A, B)) return true;
            if (Equals(A, null)) return false;
            if (Equals(B, null)) return false;
            return A.Equals(B);
        }

        public static Boolean operator !=(EnabledExtensionObject A, EnabledExtensionObject B)
        {
            return !(A == B);
        }
    }
}

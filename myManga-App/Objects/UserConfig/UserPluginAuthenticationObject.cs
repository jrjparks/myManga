using myMangaSiteExtension.Primitives.Objects;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace myManga_App.Objects.UserConfig
{
    [Serializable, XmlRoot, DebuggerStepThrough]
    public sealed class UserPluginAuthenticationObject : SerializableObject, INotifyPropertyChanging, INotifyPropertyChanged
    {
        #region NotifyPropertyChange
        public event EventHandler<GenericEventArgs<String>> UserConfigurationUpdated;
        private void OnUserConfigurationUpdated(String e)
        {
            if (UserConfigurationUpdated != null)
                UserConfigurationUpdated(this, new GenericEventArgs<String>(e));
        }

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
            OnUserConfigurationUpdated(caller);
        }
        #endregion

        [XmlIgnore]
        public String pluginName;
        [XmlElement]
        public String PluginName
        {
            get { return pluginName; }
            set { OnPropertyChanging(); pluginName = value; OnPropertyChanged(); }
        }

        [XmlIgnore]
        public String pluginLanguage;
        [XmlElement]
        public String PluginLanguage
        {
            get { return pluginLanguage; }
            set { OnPropertyChanging(); pluginLanguage = value; OnPropertyChanged(); }
        }

        [XmlIgnore]
        public String username;
        [XmlElement]
        public String Username
        {
            get { return username; }
            set { OnPropertyChanging(); username = value; OnPropertyChanged(); }
        }

        [XmlIgnore]
        public string encryptedPassword;
        [XmlElement]
        public String EncryptedPassword
        {
            get { return encryptedPassword; }
            set { OnPropertyChanging(); encryptedPassword = value; OnPropertyChanged(); }
        }

        [XmlIgnore]
        public SecureString Password
        {
            get
            {
                Byte[] entropy = Encoding.UTF8.GetBytes(Assembly.GetExecutingAssembly().FullName),
                    passwordData = Convert.FromBase64String(this.EncryptedPassword);
                SecureString secureString = new SecureString();
                foreach (Char c in Encoding.UTF8.GetString(ProtectedData.Unprotect(passwordData, entropy, DataProtectionScope.CurrentUser)))
                { secureString.AppendChar(c); }
                secureString.MakeReadOnly();
                return secureString;
            }
            set
            {
                Byte[] entropy = Encoding.UTF8.GetBytes(Assembly.GetExecutingAssembly().FullName),
                    passwordData;
                IntPtr bstr = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(value);
                try
                { passwordData = Encoding.UTF8.GetBytes(System.Runtime.InteropServices.Marshal.PtrToStringBSTR(bstr)); }
                finally
                { System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(bstr); }
                this.EncryptedPassword = Convert.ToBase64String(ProtectedData.Protect(passwordData, entropy, DataProtectionScope.CurrentUser));
                value.Dispose();
            }
        }

        public UserPluginAuthenticationObject()
            : base()
        { }
        private UserPluginAuthenticationObject(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        { base.GetObjectData(info, context); }
    }
}

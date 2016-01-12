using myMangaSiteExtension.Primitives.Objects;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace myManga_App.Objects.UserConfig
{
    [Serializable, XmlRoot, DebuggerStepThrough]
    public sealed class UserAuthenticationObject : SerializableObject, INotifyPropertyChanging, INotifyPropertyChanged
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
        private readonly ObservableCollection<UserPluginAuthenticationObject> userPluginAuthentications = new ObservableCollection<UserPluginAuthenticationObject>();
        [XmlArray, XmlArrayItem("UserPluginAuthentications")]
        public ObservableCollection<UserPluginAuthenticationObject> UserPluginAuthentications
        {
            get { return userPluginAuthentications; }
            set
            {
                userPluginAuthentications.Clear();
                foreach (UserPluginAuthenticationObject _value in value)
                    userPluginAuthentications.Add(_value);
            }
        }

        public UserAuthenticationObject()
            : base()
        { }
        private UserAuthenticationObject(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        { base.GetObjectData(info, context); }
    }
}

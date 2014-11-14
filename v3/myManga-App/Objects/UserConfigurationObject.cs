using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Core.IO;
using System.Windows;

namespace myManga_App.Objects
{
    [Serializable, XmlRoot, DebuggerStepThrough]
    public sealed class UserConfigurationObject : SerializableObject
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

        [XmlIgnore]
        private WindowState windowState;
        [XmlElement]
        public WindowState WindowState
        {
            get
            {
                if (windowState == null)
                    windowState = WindowState.Normal;
                return windowState;
            }
            set
            {
                OnPropertyChanging();
                windowState = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        private Size windowSize;
        [XmlElement]
        public Size WindowSize
        {
            get
            {
                if (windowSize == null)
                    windowSize = new Size(640, 480);
                return windowSize;
            }
            set
            {
                OnPropertyChanging();
                windowSize = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        private SaveType saveType;
        [XmlElement]
        public SaveType SaveType
        {
            get
            {
                if (saveType == null)
                    saveType = SaveType.XML;
                return saveType;
            }
            set
            {
                OnPropertyChanging();
                saveType = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        private List<String> enabledSiteExtentions;
        [XmlArray, XmlArrayItem("SiteExtentionName")]
        public List<String> EnabledSiteExtentions
        {
            get
            { return enabledSiteExtentions ?? (enabledSiteExtentions = new List<String>()); }
            set
            {
                OnPropertyChanging();
                enabledSiteExtentions = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        private List<String> enabledDatabaseExtentions;
        [XmlArray, XmlArrayItem("DatabaseExtentionName")]
        public List<String> EnabledDatabaseExtentions
        {
            get
            { return enabledDatabaseExtentions ?? (enabledDatabaseExtentions = new List<String>()); }
            set
            {
                OnPropertyChanging();
                enabledDatabaseExtentions = value;
                OnPropertyChanged();
            }
        }

        public UserConfigurationObject() : base() { }
        public UserConfigurationObject(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}

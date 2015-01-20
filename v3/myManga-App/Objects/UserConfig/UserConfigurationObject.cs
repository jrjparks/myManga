using Core.IO;
using myManga_App.Objects.MVVM;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Windows;
using System.Xml.Serialization;

namespace myManga_App.Objects
{
    [Serializable, XmlRoot, DebuggerStepThrough]
    public sealed class UserConfigurationObject : SerializableObject
    {
        #region NotifyPropertyChange
        public event EventHandler<String> UserConfigurationUpdated;
        private void OnUserConfigurationUpdated(String e)
        {
            if (UserConfigurationUpdated != null)
                UserConfigurationUpdated(this, e);
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
        private WindowState windowState = WindowState.Normal;
        [XmlElement]
        public WindowState WindowState
        {
            get { return windowState; }
            set
            {
                OnPropertyChanging();
                windowState = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        public Double windowSizeWidth = 640D;
        [XmlElement]
        public Double WindowSizeWidth
        {
            get { return windowSizeWidth; }
            set
            {
                OnPropertyChanging();
                windowSizeWidth = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        public Double windowSizeHeight = 480D;
        [XmlElement]
        public Double WindowSizeHeight
        {
            get { return windowSizeHeight; }
            set
            {
                OnPropertyChanging();
                windowSizeHeight = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        private SaveType saveType = SaveType.XML;
        [XmlElement]
        public SaveType SaveType
        {
            get { return saveType; }
            set
            {
                OnPropertyChanging();
                saveType = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        private readonly ObservableCollection<SerializableViewModelViewType> viewTypes = new ObservableCollection<SerializableViewModelViewType>();
        [XmlArray, XmlArrayItem("ViewType")]
        public ObservableCollection<SerializableViewModelViewType> ViewTypes
        {
            get { return viewTypes; }
            set
            {
                viewTypes.Clear();
                foreach (SerializableViewModelViewType _value in value)
                    viewTypes.Add(_value);
            }
        }

        [XmlIgnore]
        private readonly ObservableCollection<String> enabledSiteExtensions = new ObservableCollection<String>();
        [XmlArray, XmlArrayItem("SiteExtensionName")]
        public ObservableCollection<String> EnabledSiteExtensions
        {
            get { return enabledSiteExtensions; }
            set
            {
                enabledSiteExtensions.Clear();
                foreach (String _value in value)
                    enabledSiteExtensions.Add(_value);
            }
        }

        [XmlIgnore]
        private readonly ObservableCollection<String> enabledDatabaseExtentions = new ObservableCollection<String>();
        [XmlArray, XmlArrayItem("DatabaseExtentionName")]
        public ObservableCollection<String> EnabledDatabaseExtentions
        {
            get { return enabledDatabaseExtentions; }
            set
            {
                enabledDatabaseExtentions.Clear();
                foreach (String _value in value)
                    enabledDatabaseExtentions.Add(_value);
            }
        }

        [XmlIgnore]
        private Double defaultPageZoom = 1D;
        [XmlElement]
        public Double DefaultPageZoom
        {
            get { return defaultPageZoom; }
            set
            {
                OnPropertyChanging();
                defaultPageZoom = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        private Boolean removeBackChapters = false;
        [XmlElement]
        public Boolean RemoveBackChapters
        {
            get { return removeBackChapters; }
            set
            {
                OnPropertyChanging();
                removeBackChapters = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        private Int32 backChaptersToKeep = 3;
        [XmlElement]
        public Int32 BackChaptersToKeep
        {
            get { return backChaptersToKeep; }
            set
            {
                OnPropertyChanging();
                backChaptersToKeep = value;
                OnPropertyChanged();
            }
        }

        public UserConfigurationObject() : base() { CreateEventLinks(); }
        public UserConfigurationObject(SerializationInfo info, StreamingContext context) : base(info, context) { CreateEventLinks(); }

        private void CreateEventLinks()
        {
            ViewTypes.CollectionChanged += (s, e) => OnUserConfigurationUpdated("ViewTypes");
            ViewTypes.CollectionChanged += (s, e) => OnPropertyChanged("ViewTypes");
            EnabledSiteExtensions.CollectionChanged += (s, e) => OnUserConfigurationUpdated("EnabledSiteExtensions");
            EnabledSiteExtensions.CollectionChanged += (s, e) => OnPropertyChanged("EnabledSiteExtensions");
            EnabledDatabaseExtentions.CollectionChanged += (s, e) => OnUserConfigurationUpdated("EnabledDatabaseExtentions");
            EnabledDatabaseExtentions.CollectionChanged += (s, e) => OnPropertyChanged("EnabledDatabaseExtentions");
        }
    }
}

using myManga_App.IO.Local.Object;
using myManga_App.Objects.MVVM;
using myMangaSiteExtension.Primitives.Objects;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Windows;
using System.Xml.Serialization;

namespace myManga_App.Objects.UserConfig
{
    [Serializable, XmlRoot, DebuggerStepThrough]
    public sealed class UserConfigurationObject : SerializableObject, INotifyPropertyChanging, INotifyPropertyChanged
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
        private SerializeType serializeType = SerializeType.XML;
        [XmlElement]
        public SerializeType SerializeType
        {
            get { return serializeType; }
            set
            {
                OnPropertyChanging();
                serializeType = value;
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
        private readonly ObservableCollection<String> enabledDatabaseExtensions = new ObservableCollection<String>();
        [XmlArray, XmlArrayItem("DatabaseExtentionName")]
        public ObservableCollection<String> EnabledDatabaseExtensions
        {
            get { return enabledDatabaseExtensions; }
            set
            {
                enabledDatabaseExtensions.Clear();
                foreach (String _value in value)
                    enabledDatabaseExtensions.Add(_value);
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

        [XmlIgnore]
        private Boolean downloadNewChapters = false;
        [XmlElement]
        public Boolean DownloadNewChapters
        {
            get { return downloadNewChapters; }
            set
            {
                OnPropertyChanging();
                downloadNewChapters = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        private ThemeType theme = ThemeType.Light;
        [XmlAttribute]
        public ThemeType Theme
        {
            get { return theme; }
            set
            {
                OnPropertyChanging();
                theme = value;
                OnPropertyChanged();
            }
        }

        public UserConfigurationObject()
            : base()
        { CreateEventLinks(); }
        private UserConfigurationObject(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { CreateEventLinks(); }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        { base.GetObjectData(info, context); }

        private void CreateEventLinks()
        {
            ViewTypes.CollectionChanged += (s, e) => OnUserConfigurationUpdated("ViewTypes");
            ViewTypes.CollectionChanged += (s, e) => OnPropertyChanged("ViewTypes");
            EnabledSiteExtensions.CollectionChanged += (s, e) => OnUserConfigurationUpdated("EnabledSiteExtensions");
            EnabledSiteExtensions.CollectionChanged += (s, e) => OnPropertyChanged("EnabledSiteExtensions");
            EnabledDatabaseExtensions.CollectionChanged += (s, e) => OnUserConfigurationUpdated("EnabledDatabaseExtensions");
            EnabledDatabaseExtensions.CollectionChanged += (s, e) => OnPropertyChanged("EnabledDatabaseExtensions");
        }
    }
}

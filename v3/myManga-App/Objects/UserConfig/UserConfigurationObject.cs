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
            get { return windowState; }
            set
            {
                OnPropertyChanging();
                windowState = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        public Double windowSizeWidth;
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
        public Double windowSizeHeight;
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
        private SaveType saveType;
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
        private ObservableCollection<SerializableViewModelViewType> viewTypes;
        [XmlArray, XmlArrayItem("ViewType")]
        public ObservableCollection<SerializableViewModelViewType> ViewTypes
        {
            get { return viewTypes ?? (viewTypes = new ObservableCollection<SerializableViewModelViewType>()); }
            set
            {
                OnPropertyChanging();
                viewTypes = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        private ObservableCollection<String> enabledSiteExtensions;
        [XmlArray, XmlArrayItem("SiteExtensionName")]
        public ObservableCollection<String> EnabledSiteExtensions
        {
            get { return enabledSiteExtensions ?? (enabledSiteExtensions = new ObservableCollection<String>()); }
            set
            {
                OnPropertyChanging();
                enabledSiteExtensions = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        private ObservableCollection<String> enabledDatabaseExtentions;
        [XmlArray, XmlArrayItem("DatabaseExtentionName")]
        public ObservableCollection<String> EnabledDatabaseExtentions
        {
            get { return enabledDatabaseExtentions ?? (enabledDatabaseExtentions = new ObservableCollection<String>()); }
            set
            {
                OnPropertyChanging();
                enabledDatabaseExtentions = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        private Double defaultPageZoom;
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

        public UserConfigurationObject() : base() { }
        public UserConfigurationObject(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}

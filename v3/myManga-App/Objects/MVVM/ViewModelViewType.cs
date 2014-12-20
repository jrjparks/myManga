using Core.IO;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace myManga_App.Objects.MVVM
{
    public enum ViewModelViewType
    {
        Normal,
        Grid,
        Tree,
        List,
        Details,
    }

    [Serializable, XmlRoot, DebuggerStepThrough]
    public class SerializableViewModelViewType : SerializableObject, INotifyPropertyChanging, INotifyPropertyChanged
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
        public String viewModelName;
        [XmlElement]
        public String ViewModelName
        {
            get { return viewModelName; }
            set { OnPropertyChanging(); viewModelName = value; OnPropertyChanged(); }
        }

        [XmlIgnore]
        public ViewModelViewType viewType;
        [XmlElement]
        public ViewModelViewType ViewType
        {
            get { return viewType; }
            set { OnPropertyChanging(); viewType = value; OnPropertyChanged(); }
        }

        public SerializableViewModelViewType() : base() { }
        public SerializableViewModelViewType(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}

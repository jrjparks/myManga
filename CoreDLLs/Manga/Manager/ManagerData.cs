using System;
using BakaBox.MVVM;

namespace Manga.Manager
{
    public enum DownloadType
    {
        Manga = 0x01,
        Chapter = 0x02
    }

    public sealed class ManagerData<TData> : ModelBase
    {
        private String _Title { get; set; }
        public String Title
        {
            get { return _Title; }
            private set
            {
                OnPropertyChanging("Title");
                _Title = value;
                OnPropertyChanged("Title");
            }
        }

        private TData _Data { get; set; }
        public TData Data
        {
            get { return _Data; }
            private set
            {
                OnPropertyChanging("Data");
                _Data = value;
                OnPropertyChanged("Data");
            }
        }

        private Object[] _Parameters { get; set; }
        public Object[] Parameters
        {
            get { return _Parameters; }
            private set
            {
                OnPropertyChanging("Parameters");
                _Parameters = value;
                OnPropertyChanged("Parameters");
            }
        }

        public DownloadType DownloadType { get; private set; }

        public void UpdateTitle(String Title)
        { this.Title = Title; }
        public void UpdateData(TData Data)
        { this.Data = Data; }

        public ManagerData(DownloadType DownloadType, TData Data)
            : this(String.Empty, DownloadType, Data)
        { }
        public ManagerData(String Title, DownloadType DownloadType, TData Data)
            : this(Title, DownloadType, Data, null)
        { }
        public ManagerData(String Title, DownloadType DownloadType, TData Data, params Object[] Parameters)
        { this.Title = Title; this.Data = Data; this.DownloadType = DownloadType; this.Parameters = Parameters; }
    }

    public sealed class ManagerData<TData, TParameter> : ModelBase
    {
        private String _Title { get; set; }
        public String Title
        {
            get { return _Title; }
            private set
            {
                OnPropertyChanging("Title");
                _Title = value;
                OnPropertyChanged("Title");
            }
        }

        private TData _Data { get; set; }
        public TData Data
        {
            get { return _Data; }
            private set
            {
                OnPropertyChanging("Data");
                _Data = value;
                OnPropertyChanged("Data");
            }
        }

        private TParameter _Parameter { get; set; }
        public TParameter Parameter
        {
            get { return _Parameter; }
            private set
            {
                OnPropertyChanging("Parameter");
                _Parameter = value;
                OnPropertyChanged("Parameter");
            }
        }

        public DownloadType DownloadType { get; private set; }

        public void UpdateTitle(String Title)
        { this.Title = Title; }
        public void UpdateData(TData Data)
        { this.Data = Data; }

        public ManagerData(DownloadType DownloadType, TData Data)
            : this(String.Empty, DownloadType, Data)
        { }
        public ManagerData(String Title, DownloadType DownloadType, TData Data)
            : this(Title, DownloadType, Data, default(TParameter))
        { }
        public ManagerData(String Title, DownloadType DownloadType, TData Data, TParameter Parameter)
        { this.Title = Title; this.Data = Data; this.DownloadType = DownloadType; this.Parameter = Parameter; }
    }
}

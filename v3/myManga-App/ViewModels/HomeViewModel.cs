using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Amib.Threading;
using Core.IO;
using Core.MVVM;
using Core.Other.Singleton;
using myManga_App.IO;
using myMangaSiteExtension;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;

namespace myManga_App.ViewModels
{
    public class HomeViewModel : IDisposable, INotifyPropertyChanging, INotifyPropertyChanged
    {
        #region NotifyPropertyChange
        public event PropertyChangingEventHandler PropertyChanging;
        protected void OnPropertyChanging([CallerMemberName] String caller = "")
        {
            if (PropertyChanging != null)
                PropertyChanging(this, new PropertyChangingEventArgs(caller));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] String caller = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }
        #endregion

        #region MangaList
        protected ObservableCollection<MangaObject> mangaList;
        public ObservableCollection<MangaObject> MangaList
        {
            get { return mangaList ?? (mangaList = new ObservableCollection<MangaObject>()); }
            set
            {
                OnPropertyChanging();
                mangaList = value;
                OnPropertyChanged();
            }
        }

        protected MangaObject mangaObj;
        public MangaObject MangaObj
        {
            get { return mangaObj; }
            set
            {
                OnPropertyChanging();
                mangaObj = value;
                OnPropertyChanged();
            }
        }

        protected String searchFilter;
        public String SearchFilter
        {
            get { return searchFilter; }
            set
            {
                OnPropertyChanging();
                searchFilter = value;
                mangaListView.Refresh();
                MangaObj = MangaList.FirstOrDefault();
                OnPropertyChanged();
            }
        }

        protected ICollectionView mangaListView;

        protected DelegateCommand clearSearchCommand;
        public ICommand ClearSearchCommand
        {
            get { return clearSearchCommand ?? (clearSearchCommand = new DelegateCommand(ClearSearch, CanClearSearch)); }
        }
        protected void ClearSearch()
        { SearchFilter = String.Empty; }
        protected Boolean CanClearSearch()
        { return !String.IsNullOrWhiteSpace(SearchFilter); }
        #endregion

        #region SearchSites
        protected DelegateCommand searchSitesCommand;
        public ICommand SearchSiteCommend
        { get { return searchSitesCommand ?? (searchSitesCommand = new DelegateCommand(SearchSites, CanSearchSite)); } }

        protected Boolean CanSearchSite()
        { return !String.IsNullOrWhiteSpace(SearchFilter) && (SearchFilter.Trim().Length >= 3); }

        protected void SearchSites()
        {
            IsLoading = true;
            Singleton<myManga_App.IO.Network.SmartSearch>.Instance.SearchManga(SearchFilter.Trim());
        }

        private delegate void Instance_SearchCompleteInvoke(object sender, List<MangaObject> e);
        private void Instance_SearchComplete(object sender, List<MangaObject> e)
        {
            if (App.Dispatcher.Thread == Thread.CurrentThread)
            {
                IsLoading = false;
                foreach (MangaObject MangaObj in e)
                    if (!MangaList.Any(mo => mo.Name == MangaObj.Name))
                        MangaList.Add(MangaObj);
            }
            else
                App.Dispatcher.BeginInvoke(new Instance_SearchCompleteInvoke(Instance_SearchComplete), new Object[] { sender, e });
        }
        #endregion

        #region RefreshList
        protected DelegateCommand refreshListCommand;
        public ICommand RefreshListCommand
        { get { return refreshListCommand ?? (refreshListCommand = new DelegateCommand(RefreshList, CanRefresh)); } }

        protected Boolean CanRefresh()
        { return MangaList.Count > 0; }

        protected void RefreshList()
        {
            IsLoading = true;
            MangaObject[] MangaObjects = new MangaObject[MangaList.Count];
            MangaList.CopyTo(MangaObjects, 0);
            foreach (MangaObject mangaObject in MangaObjects)
                Singleton<myManga_App.IO.Network.SmartMangaDownloader>.Instance.DownloadMangaObject(mangaObject);
            MangaObjects = null;
        }

        private delegate void Instance_MangaObjectCompleteInvoke(object sender, MangaObject e);
        void Instance_MangaObjectComplete(object sender, MangaObject e)
        {
            if (App.Dispatcher.Thread == Thread.CurrentThread)
            {
                IsLoading = !(sender as myManga_App.IO.Network.SmartMangaDownloader).IsIdle;
                if (!MangaList.Any(mo => mo.Name == e.Name)) MangaList.Add(MangaObj);
                else MangaList.First(mo => mo.Name == e.Name).Merge(e);
            }
            else App.Dispatcher.BeginInvoke(new Instance_MangaObjectCompleteInvoke(Instance_MangaObjectComplete), new Object[] { sender, e });
        }
        #endregion

        protected Boolean isLoading;
        public Boolean IsLoading
        {
            get { return isLoading; }
            set
            {
                OnPropertyChanging();
                isLoading = value;
                OnPropertyChanged();
            }
        }

        protected App App = App.Current as App;

        public HomeViewModel()
        {
            ConfigureSearchFilter();
            Singleton<myManga_App.IO.Network.SmartSearch>.Instance.SearchComplete += Instance_SearchComplete;
            Singleton<myManga_App.IO.Network.SmartMangaDownloader>.Instance.MangaObjectComplete += Instance_MangaObjectComplete;
            if (!DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            {
                foreach (String MangaArchiveFilePath in Directory.GetFiles(App.MANGA_ARCHIVE_DIRECTORY, "*.ma", SearchOption.AllDirectories))
                    MangaList.Add(MangaArchiveFilePath.LoadFromArchive<MangaObject>("MangaObject", SaveType.XML));
                MangaObj = MangaList.FirstOrDefault();
            }
#if DEBUG
#endif
        }

        protected void ConfigureSearchFilter()
        {
            mangaListView = CollectionViewSource.GetDefaultView(MangaList);
            mangaListView.Filter = mangaObject => String.IsNullOrWhiteSpace(SearchFilter) ? true : (mangaObject as MangaObject).Name.ToLower().Contains(SearchFilter.ToLower());
            if (mangaListView.CanSort)
                mangaListView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        public void Dispose() { }
    }
}

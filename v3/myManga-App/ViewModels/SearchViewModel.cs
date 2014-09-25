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
using System.Windows.Threading;
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
    public class SearchViewModel : IDisposable, INotifyPropertyChanging, INotifyPropertyChanged
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

        #region Search
        public void StartSearch(String search_content)
        {
            SearchFilter = search_content;
        }

        protected DelegateCommand searchSitesCommand;
        public ICommand SearchSiteCommand
        { get { return searchSitesCommand ?? (searchSitesCommand = new DelegateCommand(SearchSites, CanSearchSite)); } }

        protected Boolean CanSearchSite()
        { return !String.IsNullOrWhiteSpace(SearchFilter) && (SearchFilter.Trim().Length >= 3); }

        protected void SearchSites()
        {
            IsLoading = true;
            Singleton<myManga_App.IO.Network.SmartSearch>.Instance.SearchManga(SearchFilter.Trim());
        }

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

        private delegate void Instance_DownloadMangaCompleteInvoke(object sender, MangaObject e);
        private void Instance_DownloadMangaComplete(object sender, MangaObject e)
        {
            if (App.Dispatcher.Thread == Thread.CurrentThread)
            {
                String save_path = Path.Combine(App.MANGA_ARCHIVE_DIRECTORY, e.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION));
                MangaObj.SaveToArchive(save_path, SaveType: SaveType.XML);
                if (!MangaList.Any(mo => mo.Name == e.Name)) MangaList.Add(MangaObj);
                else MangaList.First(mo => mo.Name == e.Name).Merge(e);
            }
            else
                App.Dispatcher.BeginInvoke(new Instance_DownloadMangaCompleteInvoke(Instance_DownloadMangaComplete), new Object[] { sender, e });
        }
        #endregion

        #region Manga List
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
                mangaListView.MoveCurrentToFirst();
                OnPropertyChanged();
            }
        }

        protected ICollectionView mangaListView;
        #endregion

        protected App App = App.Current as App;

        public SearchViewModel()
        {
            ConfigureSearchFilter();
            Singleton<myManga_App.IO.Network.SmartSearch>.Instance.SearchComplete += Instance_SearchComplete;
            Singleton<myManga_App.IO.Network.SmartMangaDownloader>.Instance.MangaObjectComplete += Instance_DownloadMangaComplete;
        }

        protected void ConfigureSearchFilter()
        {
            mangaListView = CollectionViewSource.GetDefaultView(MangaList);
            mangaListView.Filter = mangaObject => String.IsNullOrWhiteSpace(SearchFilter) ? true : (mangaObject as MangaObject).IsNameMatch(SearchFilter);
            if (mangaListView.CanSort)
                mangaListView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        public void Dispose() { }
    }
}

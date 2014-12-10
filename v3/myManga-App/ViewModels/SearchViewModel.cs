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
using myManga_App.Properties;
using myManga_App.IO.Network;
using myManga_App.IO.ViewModel;

namespace myManga_App.ViewModels
{
    public class SearchViewModel : BaseViewModel
    {
        #region Search
        public void StartSearch(String search_content)
        {
            Messenger.Default.Send(this, "FocusRequest");
            SearchFilter = search_content;
            SearchSites();
        }

        protected DelegateCommand searchSitesCommand;
        public ICommand SearchSiteCommand
        { get { return searchSitesCommand ?? (searchSitesCommand = new DelegateCommand(SearchSites, CanSearchSite)); } }

        protected Boolean CanSearchSite()
        { return !String.IsNullOrWhiteSpace(SearchFilter) && (SearchFilter.Trim().Length >= 3); }

        protected void SearchSites()
        {
            MangaList.Clear();
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

        #region Store MangaInfo
        protected DelegateCommand storeMangaInfoCommand;
        public ICommand StoreMangaInfoCommand
        { get { return storeMangaInfoCommand ?? (storeMangaInfoCommand = new DelegateCommand(StoreMangaInfo, CanStoreMangaInfo)); } }

        protected Boolean CanStoreMangaInfo()
        { return MangaObj != null; }

        protected void StoreMangaInfo()
        { DownloadManager.Default.Download(MangaObj); }

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

        public SearchViewModel()
            : base()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                ConfigureSearchFilter();
                Singleton<myManga_App.IO.Network.SmartSearch>.Instance.SearchComplete += Instance_SearchComplete;
                Messenger.Default.RegisterRecipient<String>(this, StartSearch, "SearchRequest");
            }
        }

        protected void ConfigureSearchFilter()
        {
            mangaListView = CollectionViewSource.GetDefaultView(MangaList);
            mangaListView.Filter = mangaObject => String.IsNullOrWhiteSpace(SearchFilter) ? true : (mangaObject as MangaObject).IsNameMatch(SearchFilter);
            if (mangaListView.CanSort)
                mangaListView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }
    }
}

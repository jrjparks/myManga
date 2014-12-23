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
using Core.MVVM;

namespace myManga_App.ViewModels
{
    public sealed class SearchViewModel : BaseViewModel
    {
        #region Search
        private void StartSearch(String search_content)
        {
            Messenger.Default.Send(this, "FocusRequest");
            SearchFilter = search_content;
            SearchSites();
        }

        private DelegateCommand searchSitesCommand;
        public ICommand SearchSiteCommand
        { get { return searchSitesCommand ?? (searchSitesCommand = new DelegateCommand(SearchSites, CanSearchSite)); } }

        private Boolean CanSearchSite()
        { return !String.IsNullOrWhiteSpace(SearchFilter) && (SearchFilter.Trim().Length >= 3); }

        private void SearchSites()
        {
            MangaCollection.Clear();
            IsLoading = true;
            Guid ResultId = DownloadManager.Default.Search(SearchFilter.Trim());
            Messenger.Default.RegisterRecipient<List<MangaObject>>(
                this, DisplaySearchResults, 
                String.Format("SearchResult-{0}", ResultId.ToString()));
        }

        private void DisplaySearchResults(List<MangaObject> Results, Object Context)
        {
            Messenger.Default.UnregisterRecipient(this, Context);
            IsLoading = false;
            foreach (MangaObject MangaObj in Results)
                MangaCollection.Add(MangaObj);
        }

        private Boolean isLoading;
        public Boolean IsLoading
        {
            get { return isLoading; }
            set { SetProperty(ref isLoading, value); }
        }

        private DelegateCommand clearSearchCommand;
        public ICommand ClearSearchCommand
        {
            get { return clearSearchCommand ?? (clearSearchCommand = new DelegateCommand(ClearSearch, CanClearSearch)); }
        }
        private void ClearSearch()
        { SearchFilter = String.Empty; }
        private Boolean CanClearSearch()
        { return !String.IsNullOrWhiteSpace(SearchFilter); }
        #endregion

        #region Manga List
        private static readonly DependencyProperty MangaCollectionProperty = DependencyProperty.RegisterAttached(
            "MangaCollection",
            typeof(ObservableCollection<MangaObject>),
            typeof(SearchViewModel),
            new PropertyMetadata(new ObservableCollection<MangaObject>()));
        public ObservableCollection<MangaObject> MangaCollection
        {
            get { return (ObservableCollection<MangaObject>)GetValue(MangaCollectionProperty); }
            set { SetValue(MangaCollectionProperty, value); }
        }

        private static readonly DependencyProperty SelectedMangaObjectProperty = DependencyProperty.RegisterAttached(
            "SelectedMangaObject",
            typeof(MangaObject),
            typeof(SearchViewModel));
        public MangaObject SelectedMangaObject
        {
            get { return (MangaObject)GetValue(SelectedMangaObjectProperty); }
            set { SetValue(SelectedMangaObjectProperty, value); }
        }

        private static readonly DependencyProperty SearchFilterProperty = DependencyProperty.RegisterAttached(
            "SearchFilter",
            typeof(String),
            typeof(SearchViewModel),
            new PropertyMetadata(OnSearchFilterChanged));
        public String SearchFilter
        {
            get { return (String)GetValue(SearchFilterProperty); }
            set { SetValue(SearchFilterProperty, value); }
        }

        private static void OnSearchFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SearchViewModel _this = (d as SearchViewModel);
            _this.MangaListView.Refresh();
            _this.MangaListView.MoveCurrentToFirst();
        }

        private ICollectionView MangaListView;
        #endregion

        #region Store MangaInfo
        private DelegateCommand storeMangaInfoCommand;
        public ICommand StoreMangaInfoCommand
        { get { return storeMangaInfoCommand ?? (storeMangaInfoCommand = new DelegateCommand(StoreMangaInfo, CanStoreMangaInfo)); } }

        private Boolean CanStoreMangaInfo()
        { return SelectedMangaObject != null; }

        private void StoreMangaInfo()
        { DownloadManager.Default.Download(SelectedMangaObject); }

        private delegate void Instance_SearchCompleteInvoke(object sender, List<MangaObject> e);
        private void Instance_SearchComplete(object sender, List<MangaObject> e)
        {
            if (App.Dispatcher.Thread == Thread.CurrentThread)
            {
                IsLoading = false;
                foreach (MangaObject MangaObj in e)
                    if (!MangaCollection.Any(mo => mo.Name == MangaObj.Name))
                        MangaCollection.Add(MangaObj);
            }
            else
                App.Dispatcher.BeginInvoke(new Instance_SearchCompleteInvoke(Instance_SearchComplete), new Object[] { sender, e });
        }
        #endregion

        public SearchViewModel()
            : base()
        {
            if (!IsInDesignMode)
            {
                ConfigureSearchFilter();
                Messenger.Default.RegisterRecipient<String>(this, StartSearch, "SearchRequest");
            }
        }

        private void ConfigureSearchFilter()
        {
            MangaListView = CollectionViewSource.GetDefaultView(MangaCollection);
            MangaListView.Filter = mangaObject => String.IsNullOrWhiteSpace(SearchFilter) ? true : (mangaObject as MangaObject).IsNameMatch(SearchFilter);
            if (MangaListView.CanSort)
                MangaListView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }
    }
}

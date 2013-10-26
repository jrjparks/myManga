using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Input;
using Amib.Threading;
using Core.IO;
using Core.MVVM;
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
        protected SmartGroupObject<List<SearchResultObject>> SearchWorkGroup;

        protected DelegateCommand searchSitesCommand;
        public ICommand SearchSiteCommend
        {
            get { return searchSitesCommand ?? (searchSitesCommand = new DelegateCommand(SearchSites, CanSearchSite)); }
        }
        protected void SearchSites()
        {
            if (SearchWorkGroup != null && !SearchWorkGroup.WorkItemsGroup.IsIdle)
            {
                SearchWorkGroup.WorkItemsGroup.OnIdle -= SearchWorkGroup_OnIdle;
                SearchWorkGroup.WorkItemsGroup.Cancel(true);
            }
            SearchWorkGroup = Core.Other.Singleton.Singleton<myManga_App.IO.Network.SmartSearch>.Instance.SearchManga(SearchFilter);
            SearchWorkGroup.WorkItemsGroup.OnIdle += SearchWorkGroup_OnIdle;
        }

        void SearchWorkGroup_OnIdle(Amib.Threading.IWorkItemsGroup workItemsGroup)
        {
            SearchWorkGroup.WorkItemsGroup.OnIdle -= SearchWorkGroup_OnIdle;
            SearchWorkGroup.WorkItemsGroup.Cancel();
            Regex safeAlphaNumeric = new Regex("[^a-z0-9]", RegexOptions.IgnoreCase);
            Dictionary<String, MangaObject> MangaObjectSearchResults = new Dictionary<String, MangaObject>();
            foreach (IWorkItemResult<List<SearchResultObject>> SearchResults in SearchWorkGroup.WorkItemResults)
            {
                foreach (SearchResultObject SearchResult in SearchResults.Result)
                {
                    String key = safeAlphaNumeric.Replace(SearchResult.Name.ToLower(), String.Empty);
                    if (MangaObjectSearchResults.ContainsKey(key))
                        MangaObjectSearchResults[key].Merge(SearchResult.ConvertToMangaObject());
                    else
                        MangaObjectSearchResults.Add(key, SearchResult.ConvertToMangaObject());
                }
            }
            SearchWorkGroup = null;
        }
        protected Boolean CanSearchSite()
        { return !String.IsNullOrWhiteSpace(SearchFilter) && (SearchFilter.Length >= 3); }
        #endregion

        protected App App = App.Current as App;

        public HomeViewModel()
        {
            ConfigureSearchFilter();
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
        }

        public void Dispose() { }
    }
}

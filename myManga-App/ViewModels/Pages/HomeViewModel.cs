using Core.IO;
using Core.MVVM;
using myManga_App.Objects;
using myManga_App.Objects.Cache;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using myManga_App.ViewModels.Objects;

namespace myManga_App.ViewModels.Pages
{
    public sealed class HomeViewModel : BaseViewModel
    {
        private readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(30);

        public HomeViewModel()
            : base(SupportsViewTypeChange: true)
        {
            MangaCacheObjectDetail = new MangaCacheObjectDetailViewModel();
            if (!IsInDesignMode)
            {
                ConfigureMangaArchiveCacheObjectView();
                Messenger.Default.RegisterRecipient<MangaCacheObject>(this, SelectMangaCacheObject => SelectedMangaCacheObject = SelectMangaCacheObject, "SelectMangaCacheObject");
            }
        }

        protected override void SubDispose()
        {

        }

        #region Search Term
        private static readonly DependencyProperty SearchTermProperty = DependencyProperty.RegisterAttached(
            "SearchTerm",
            typeof(String),
            typeof(HomeViewModel),
            new PropertyMetadata(String.Empty, OnSearchTermChanged));

        public String SearchTerm
        {
            get { return (String)GetValue(SearchTermProperty); }
            set { SetValue(SearchTermProperty, value); }
        }

        private static void OnSearchTermChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            HomeViewModel _this = (d as HomeViewModel);
            _this.MangaCacheObjectView.Refresh();
            _this.MangaCacheObjectView.MoveCurrentToFirst();
        }

        private DelegateCommand clearSearchTermCommand;
        public ICommand ClearSearchTermCommand
        { get { return clearSearchTermCommand ?? (clearSearchTermCommand = new DelegateCommand(() => SearchTerm = String.Empty)); } }
        #endregion

        #region Forward Search Term
        private DelegateCommand<String> forwardSearchTermCommand;
        public ICommand ForwardSearchTermCommand
        { get { return forwardSearchTermCommand ?? (forwardSearchTermCommand = new DelegateCommand<String>(ForwardSearch, CanForwardSearch)); } }

        private Boolean CanForwardSearch(String SearchTerm)
        {
            if (Equals(SearchTerm, null)) return false;
            SearchTerm = SearchTerm.Trim();
            if (String.IsNullOrWhiteSpace(SearchTerm)) return false;
            else if (SearchTerm.Length < 3) return false;
            return true;
        }

        private void ForwardSearch(String SearchTerm)
        { Messenger.Default.Send(SearchTerm, "SearchRequest"); }
        #endregion

        #region SelectedMangaCacheObject
        private static readonly DependencyProperty SelectedMangaCacheObjectProperty = DependencyProperty.RegisterAttached(
            "SelectedMangaCacheObject",
            typeof(MangaCacheObject),
            typeof(HomeViewModel),
            new PropertyMetadata((d,e) => {
                (d as HomeViewModel).MangaCacheObjectDetail.MangaCacheObject = e.NewValue as MangaCacheObject;
            }));

        public MangaCacheObject SelectedMangaCacheObject
        {
            get { return (MangaCacheObject)GetValue(SelectedMangaCacheObjectProperty); }
            set { SetValue(SelectedMangaCacheObjectProperty, value); }
        }
        #endregion

        #region MangaCacheObjects

        #region Search Term
        private static readonly DependencyPropertyKey MangaCacheObjectDetailPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "MangaCacheObjectDetail",
            typeof(MangaCacheObjectDetailViewModel),
            typeof(HomeViewModel),
            null);
        private static readonly DependencyProperty MangaCacheObjectDetailProperty = MangaCacheObjectDetailPropertyKey.DependencyProperty;

        public MangaCacheObjectDetailViewModel MangaCacheObjectDetail
        {
            get { return (MangaCacheObjectDetailViewModel)GetValue(MangaCacheObjectDetailProperty); }
            private set { SetValue(MangaCacheObjectDetailPropertyKey, value); }
        }
        #endregion

        private ICollectionView MangaCacheObjectView
        { get; set; }

        private void ConfigureMangaArchiveCacheObjectView()
        {
            MangaCacheObjectView = CollectionViewSource.GetDefaultView(App.MangaCacheObjects);

            if (MangaCacheObjectView.CanGroup)
            {
                MangaCacheObjectView.GroupDescriptions.Clear();
                MangaCacheObjectView.GroupDescriptions.Add(new PropertyGroupDescription("IsNewManga"));
                MangaCacheObjectView.GroupDescriptions.Add(new PropertyGroupDescription("HasMoreToRead"));

                ActivateLiveGrouping(MangaCacheObjectView, "IsNewManga", "HasMoreToRead");
            }
            if (MangaCacheObjectView.CanSort)
            {
                MangaCacheObjectView.SortDescriptions.Clear();
                MangaCacheObjectView.SortDescriptions.Add(new SortDescription("IsNewManga", ListSortDirection.Descending));
                MangaCacheObjectView.SortDescriptions.Add(new SortDescription("HasMoreToRead", ListSortDirection.Descending));
                MangaCacheObjectView.SortDescriptions.Add(new SortDescription("MangaObject.Name", ListSortDirection.Ascending));

                ActivateLiveSorting(MangaCacheObjectView, "IsNewManga", "HasMoreToRead", "MangaObject.Name");
            }
            MangaCacheObjectView.Filter = FilterMangaCacheObject;
            ActivateLiveFiltering(MangaCacheObjectView, "MangaObject", "BookmarkObject");
        }

        private Boolean FilterMangaCacheObject(object item)
        {
            MangaCacheObject MangaCacheObject = item as MangaCacheObject;
            if (String.IsNullOrWhiteSpace(SearchTerm)) return true;
            return MangaCacheObject.MangaObject.IsNameMatch(SearchTerm);
        }

        private void ActivateLiveSorting(ICollectionView collectionView, params String[] propertyNames)
        { ActivateLiveSorting(collectionView, propertyNames.ToList()); }
        private void ActivateLiveSorting(ICollectionView collectionView, IList<String> propertyNames)
        {
            ICollectionViewLiveShaping collectionViewLiveShaping = collectionView as ICollectionViewLiveShaping;
            if (Equals(collectionViewLiveShaping, null)) return;
            else if (collectionViewLiveShaping.CanChangeLiveSorting)
            {
                foreach (String propertyName in propertyNames)
                { collectionViewLiveShaping.LiveSortingProperties.Add(propertyName); }
                collectionViewLiveShaping.IsLiveSorting = true;
            }
        }

        private void ActivateLiveGrouping(ICollectionView collectionView, params String[] propertyNames)
        { ActivateLiveGrouping(collectionView, propertyNames.ToList()); }
        private void ActivateLiveGrouping(ICollectionView collectionView, IList<String> propertyNames)
        {
            ICollectionViewLiveShaping collectionViewLiveShaping = collectionView as ICollectionViewLiveShaping;
            if (Equals(collectionViewLiveShaping, null)) return;
            else if (collectionViewLiveShaping.CanChangeLiveGrouping)
            {
                foreach (String propertyName in propertyNames)
                { collectionViewLiveShaping.LiveGroupingProperties.Add(propertyName); }
                collectionViewLiveShaping.IsLiveGrouping = true;
            }
        }

        private void ActivateLiveFiltering(ICollectionView collectionView, params String[] propertyNames)
        { ActivateLiveFiltering(collectionView, propertyNames.ToList()); }
        private void ActivateLiveFiltering(ICollectionView collectionView, IList<String> propertyNames)
        {
            ICollectionViewLiveShaping collectionViewLiveShaping = collectionView as ICollectionViewLiveShaping;
            if (Equals(collectionViewLiveShaping, null)) return;
            else if (collectionViewLiveShaping.CanChangeLiveFiltering)
            {
                foreach (String propertyName in propertyNames)
                { collectionViewLiveShaping.LiveFilteringProperties.Add(propertyName); }
                collectionViewLiveShaping.IsLiveFiltering = true;
            }
        }
        #endregion

        #region Refresh Command
        private DelegateCommand<MangaCacheObject> refreshCommand;
        public ICommand RefreshCommand
        { get { return refreshCommand ?? (refreshCommand = new DelegateCommand<MangaCacheObject>(Refresh, CanRefresh)); } }

        private Boolean CanRefresh(MangaCacheObject MangaCacheObject)
        {
            if (Equals(MangaCacheObject, null)) return false;
            return true;
        }

        private void Refresh(MangaCacheObject MangaCacheObject)
        { App.ContentDownloadManager.Download(MangaCacheObject.MangaObject, true, MangaCacheObject.DownloadProgressReporter); }
        #endregion

        #region Delete Command
        private DelegateCommand<MangaCacheObject> deleteCommand;
        public ICommand DeleteCommand
        { get { return deleteCommand ?? (deleteCommand = new DelegateCommand<MangaCacheObject>(DeleteAsync, CanDeleteAsync)); } }

        private Boolean CanDeleteAsync(MangaCacheObject MangaCacheObject)
        {
            if (Equals(MangaCacheObject, null)) return false;
            return true;
        }

        private void DeleteAsync(MangaCacheObject MangaCacheObject)
        {
            String SavePath = Path.Combine(App.MANGA_ARCHIVE_DIRECTORY, MangaCacheObject.MangaObject.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION));
            MessageBoxResult msgboxResult = MessageBox.Show(String.Format("Are you sure you wish to delete \"{0}\"?", MangaCacheObject.MangaObject.Name), "Delete Manga?", MessageBoxButton.YesNo);
            if (Equals(msgboxResult, MessageBoxResult.Yes))
                File.Delete(SavePath);
        }
        #endregion

        #region Refresh List Command
        private DelegateCommand refreshListCommand;
        public ICommand RefreshListCommand
        { get { return refreshListCommand ?? (refreshListCommand = new DelegateCommand(RefreshList, CanRefreshList)); } }

        private Boolean CanRefreshList()
        {
            if (Equals(App.MangaCacheObjects, null)) return false;
            if (Equals(App.MangaCacheObjects.Count, 0)) return false;
            return true;
        }

        private void RefreshList()
        {
            foreach (MangaCacheObject MangaCacheObject in App.MangaCacheObjects)
            { App.ContentDownloadManager.Download(MangaCacheObject.MangaObject, true, MangaCacheObject.DownloadProgressReporter); }
        }
        #endregion

        #region Downloads
        #region Chapter Download
        private DelegateCommand<ChapterObject> downloadChapterAsyncCommand;
        public ICommand DownloadChapterAsyncCommand
        { get { return downloadChapterAsyncCommand ?? (downloadChapterAsyncCommand = new DelegateCommand<ChapterObject>(DownloadChapterAsync, CanDownloadChapterAsync)); } }

        private Boolean CanDownloadChapterAsync(ChapterObject ChapterObject)
        {
            if (Equals(SelectedMangaCacheObject, null)) return false;
            if (Equals(SelectedMangaCacheObject.MangaObject, null)) return false;
            if (Equals(ChapterObject, null)) return false;
            return true;
        }

        private void DownloadChapterAsync(ChapterObject ChapterObject)
        { App.ContentDownloadManager.Download(SelectedMangaCacheObject.MangaObject, ChapterObject); }
        #endregion

        #region Selected Chapters Download
        private DelegateCommand<IList> downloadSelectedChaptersAsyncCommand;
        public ICommand DownloadSelectedChaptersAsyncCommand
        { get { return downloadSelectedChaptersAsyncCommand ?? (downloadSelectedChaptersAsyncCommand = new DelegateCommand<IList>(DownloadSelectedChaptersAsync, CanDownloadSelectedChaptersAsync)); } }

        private Boolean CanDownloadSelectedChaptersAsync(IList SelectedChapterObjects)
        {
            if (Equals(SelectedMangaCacheObject, null)) return false;
            if (Equals(SelectedMangaCacheObject.MangaObject, null)) return false;
            if (Equals(SelectedChapterObjects, null)) return false;
            if (Equals(SelectedChapterObjects.Count, 0)) return false;
            return true;
        }

        private void DownloadSelectedChaptersAsync(IList SelectedChapterObjects)
        {
            foreach (ChapterObject ChapterObject in SelectedChapterObjects)
            { App.ContentDownloadManager.Download(SelectedMangaCacheObject.MangaObject, ChapterObject); }
        }
        #endregion

        #region All Chapters Download
        private DelegateCommand downloadAllChaptersAsyncCommand;
        public ICommand DownloadAllChaptersAsyncCommand
        { get { return downloadAllChaptersAsyncCommand ?? (downloadAllChaptersAsyncCommand = new DelegateCommand(DownloadAllChaptersAsync, CanDownloadAllChaptersAsync)); } }

        private Boolean CanDownloadAllChaptersAsync()
        {
            if (Equals(SelectedMangaCacheObject, null)) return false;
            if (Equals(SelectedMangaCacheObject.MangaObject, null)) return false;
            return true;
        }

        private void DownloadAllChaptersAsync()
        {
            foreach (ChapterObject ChapterObject in SelectedMangaCacheObject.MangaObject.Chapters)
            { App.ContentDownloadManager.Download(SelectedMangaCacheObject.MangaObject, ChapterObject); }
        }
        #endregion

        #region To Latest Chapter Download
        private DelegateCommand downloadToLatestChapterAsyncCommand;
        public ICommand DownloadToLatestChapterAsyncCommand
        { get { return downloadToLatestChapterAsyncCommand ?? (downloadToLatestChapterAsyncCommand = new DelegateCommand(DownloadToLatestChapterAsync, CanDownloadToLatestChapterAsync)); } }

        private Boolean CanDownloadToLatestChapterAsync()
        {
            if (Equals(SelectedMangaCacheObject, null)) return false;
            if (Equals(SelectedMangaCacheObject.MangaObject, null)) return false;
            return true;
        }

        private void DownloadToLatestChapterAsync()
        {
            Int32 idx = SelectedMangaCacheObject.MangaObject.Chapters.IndexOf(SelectedMangaCacheObject.ResumeChapterObject);
            foreach (ChapterObject ChapterObject in SelectedMangaCacheObject.MangaObject.Chapters.Skip(idx))
            { App.ContentDownloadManager.Download(SelectedMangaCacheObject.MangaObject, ChapterObject); }
        }
        #endregion
        #endregion

        #region Open ChapterObjects

        #region Read ChapterObjects
        private DelegateCommand<ChapterObject> readChapterCommand;
        public ICommand ReadChapterCommand
        { get { return readChapterCommand ?? (readChapterCommand = new DelegateCommand<ChapterObject>(ReadChapterAsync, CanReadChapterAsync)); } }

        private Boolean CanReadChapterAsync(ChapterObject ChapterObject)
        {
            if (Equals(SelectedMangaCacheObject, null)) return false;
            if (Equals(SelectedMangaCacheObject.MangaObject, null)) return false;
            if (Equals(ChapterObject, null)) return false;
            return true;
        }

        private async void ReadChapterAsync(ChapterObject ChapterObject)
        {
            String BookmarkChapterPath = Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, SelectedMangaCacheObject.MangaObject.MangaFileName());
            MangaObject SelectedMangaObject = SelectedMangaCacheObject.MangaObject;
            if (!ChapterObject.IsLocal(BookmarkChapterPath, App.CHAPTER_ARCHIVE_EXTENSION))
                await App.ContentDownloadManager.DownloadAsync(SelectedMangaObject, ChapterObject);
            Messenger.Default.Send(new ReadChapterRequestObject(SelectedMangaObject, ChapterObject), "ReadChapterRequest");
        }
        #endregion

        #endregion
    }
}

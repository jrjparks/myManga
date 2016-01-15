using myManga_App.Objects.Cache;
using myManga_App.ViewModels.Dialog;
using myManga_App.ViewModels.Objects.Cache.MangaCacheObjectViewModels;
using myMangaSiteExtension.Utilities;
using System;
using System.Collections.Generic;
using System.Communication;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace myManga_App.ViewModels.Pages
{
    public sealed class HomeViewModel : BaseViewModel
    {
        private readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(30);

        public HomeViewModel()
            : base(SupportsViewTypeChange: true)
        {
            MangaCacheObjectDetail = new MangaCacheObjectDetailViewModel();
            MangaCacheObjectDialog = new MangaCacheObjectDialogViewModel()
            { MangaCacheObjectDetail = MangaCacheObjectDetail };
            if (!IsInDesignMode)
            {
                ConfigureMangaArchiveCacheObjectView();
                Messenger.Instance.RegisterRecipient<MangaCacheObject>(this, SelectMangaCacheObject => SelectedMangaCacheObject = SelectMangaCacheObject, "SelectMangaCacheObject");
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
        { Messenger.Instance.Send(SearchTerm, "SearchRequest"); }
        #endregion

        #region SelectedMangaCacheObject
        private static readonly DependencyProperty SelectedMangaCacheObjectProperty = DependencyProperty.RegisterAttached(
            "SelectedMangaCacheObject",
            typeof(MangaCacheObject),
            typeof(HomeViewModel),
            new PropertyMetadata((d, e) =>
            {
                (d as HomeViewModel).MangaCacheObjectDetail.MangaCacheObject = e.NewValue as MangaCacheObject;
            }));

        public MangaCacheObject SelectedMangaCacheObject
        {
            get { return (MangaCacheObject)GetValue(SelectedMangaCacheObjectProperty); }
            set { SetValue(SelectedMangaCacheObjectProperty, value); }
        }
        #endregion

        #region MangaCacheObjects

        #region MangaCacheObjectDialog
        private static readonly DependencyPropertyKey MangaCacheObjectDialogPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "MangaCacheObjectDialog",
            typeof(MangaCacheObjectDialogViewModel),
            typeof(HomeViewModel),
            null);
        private static readonly DependencyProperty MangaCacheObjectDialogProperty = MangaCacheObjectDialogPropertyKey.DependencyProperty;

        public MangaCacheObjectDialogViewModel MangaCacheObjectDialog
        {
            get { return (MangaCacheObjectDialogViewModel)GetValue(MangaCacheObjectDialogProperty); }
            private set { SetValue(MangaCacheObjectDialogPropertyKey, value); }
        }
        #endregion

        #region MangaCacheObjectDetail
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
    }
}

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
            FilterMode = FilterOptions.Default;
            MangaCacheObjectDialog = new MangaCacheObjectDialogViewModel();
            MangaCacheObjectDialog.MangaCacheObjectDetail = new MangaCacheObjectDetailViewModel();

            if (!IsInDesignMode)
            {
                ConfigureMangaArchiveCacheObjectView();
                Messenger.Instance.RegisterRecipient<MangaCacheObject>(this, SelectMangaCacheObject => SelectedMangaCacheObject = SelectMangaCacheObject, "SelectMangaCacheObject");
            }
        }

        protected override void SubDispose()
        {
            MangaCacheObjectDialog.MangaCacheObjectDetail.Dispose();
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
                HomeViewModel control = d as HomeViewModel;
                MangaCacheObject NewValue = e.NewValue as MangaCacheObject;
                if (!Equals(control.MangaCacheObjectDialog, null))
                    if (!Equals(control.MangaCacheObjectDialog.MangaCacheObjectDetail, null))
                        control.MangaCacheObjectDialog.MangaCacheObjectDetail.MangaCacheObject = NewValue;
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

        #region Show MangaCacheObject Dialog
        private DelegateCommand<MangaCacheObject> showMangaCacheObjectDialogCommand;
        public ICommand ShowMangaCacheObjectDialogCommand
        { get { return showMangaCacheObjectDialogCommand ?? (showMangaCacheObjectDialogCommand = new DelegateCommand<MangaCacheObject>(ShowMangaCacheObjectDialog, CanShowMangaCacheObjectDialog)); } }

        private Boolean CanShowMangaCacheObjectDialog(MangaCacheObject MangaCacheObject)
        {
            if (Equals(MangaCacheObject, null)) return false;
            return true;
        }

        private void ShowMangaCacheObjectDialog(MangaCacheObject MangaCacheObject)
        {
            MangaCacheObjectDialog.MangaCacheObjectDetail.MangaCacheObject = MangaCacheObject;
            MangaCacheObjectDialog.ShowDialog();
        }
        #endregion

        #endregion

        #region MangaCacheObject
        private ICollectionView MangaCacheObjectView
        { get; set; }

        [Flags]
        public enum GroupingOptions
        {
            Default = 0,
            Ignore_New = 1 << 0,
            Ignore_MoreToRead = 1 << 1,
        }

        private void ConfigureMangaArchiveCacheObjectView()
        {
            MangaCacheObjectView = CollectionViewSource.GetDefaultView(App.MangaCacheObjects);

            if (MangaCacheObjectView.CanGroup)
            {
                ApplyGrouping(MangaCacheObjectView, "IsNewManga", "HasMoreToRead");
                ActivateLiveGrouping(MangaCacheObjectView, "IsNewManga", "HasMoreToRead");
            }
            if (MangaCacheObjectView.CanSort)
            {
                ApplySorting(MangaCacheObjectView,
                    new SortDescription("IsNewManga", ListSortDirection.Descending),
                    new SortDescription("HasMoreToRead", ListSortDirection.Descending),
                    new SortDescription("MangaObject.Name", ListSortDirection.Ascending));
                ActivateLiveSorting(MangaCacheObjectView, "IsNewManga", "HasMoreToRead", "MangaObject.Name");
            }
            MangaCacheObjectView.Filter = FilterMangaCacheObject;
            ActivateLiveFiltering(MangaCacheObjectView, "MangaObject", "BookmarkObject");
        }

        [Flags]
        public enum FilterOptions
        {
            Default = 0,
            Hide_Unread = 1 << 0,
            Hide_Read = 1 << 1,
            Hide_New = 1 << 2,
        }
        public FilterOptions FilterMode { get; set; }

        private Boolean FilterMangaCacheObject(object item)
        {
            MangaCacheObject MangaCacheObject = item as MangaCacheObject;
            if (String.IsNullOrWhiteSpace(SearchTerm)) return true;
            return MangaCacheObject.MangaObject.IsNameMatch(SearchTerm);
        }

        private void ApplyGrouping(ICollectionView collectionView, params String[] groupPropertyNames)
        {
            IList<PropertyGroupDescription> groupDescriptions =
                (from groupPropertyName in groupPropertyNames
                 select new PropertyGroupDescription(groupPropertyName)).ToList();
            ApplyGrouping(collectionView, groupDescriptions);
        }
        private void ApplyGrouping(ICollectionView collectionView, params PropertyGroupDescription[] groupDescriptions)
        { ApplyGrouping(collectionView, groupDescriptions.ToList()); }
        private void ApplyGrouping(ICollectionView collectionView, IList<PropertyGroupDescription> groupDescriptions)
        {
            collectionView.GroupDescriptions.Clear();
            foreach (PropertyGroupDescription groupDescription in groupDescriptions)
            { collectionView.GroupDescriptions.Add(groupDescription); }
        }

        private void ApplySorting(ICollectionView collectionView, params String[] sortPropertyNames)
        {
            IList<SortDescription> sortDescriptions =
                (from sortPropertyName in sortPropertyNames
                 select new SortDescription(sortPropertyName, ListSortDirection.Ascending)).ToList();
            ApplySorting(collectionView, sortDescriptions.ToList());
        }
        private void ApplySorting(ICollectionView collectionView, params SortDescription[] sortDescriptions)
        { ApplySorting(collectionView, sortDescriptions.ToList()); }
        private void ApplySorting(ICollectionView collectionView, IList<SortDescription> sortDescriptions)
        {
            collectionView.SortDescriptions.Clear();
            foreach (SortDescription sortDescription in sortDescriptions)
            { collectionView.SortDescriptions.Add(sortDescription); }
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

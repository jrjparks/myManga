using Core.MVVM;
using myMangaSiteExtension.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace myManga_App.ViewModels.Pages
{
    public sealed class SearchViewModel : BaseViewModel
    {
        public SearchViewModel()
            : base(SupportsViewTypeChange: false)
        {
            if (!IsInDesignMode)
            {
                SearchProgressReporter = new Progress<Int32>(ProgressValue =>
                {
                    SearchProgressActive = (0 < ProgressValue && ProgressValue < 100);
                    SearchProgress = ProgressValue;
                });
                Messenger.Default.RegisterRecipient<String>(this, SearchTerm =>
                {
                    SearchAsync(SearchTerm);
                    PullFocus();
                }, "SearchRequest");
            }
        }

        #region Search

        #region Progress
        private IProgress<Int32> SearchProgressReporter
        { get; set; }

        private static readonly DependencyPropertyKey SearchProgressPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "SearchProgress",
            typeof(Int32),
            typeof(SearchViewModel),
            new PropertyMetadata(0));
        private static readonly DependencyProperty SearchProgressProperty = SearchProgressPropertyKey.DependencyProperty;

        public Int32 SearchProgress
        {
            get { return (Int32)GetValue(SearchProgressProperty); }
            private set { SetValue(SearchProgressPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey SearchProgressActivePropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "SearchProgressActive",
            typeof(Boolean),
            typeof(SearchViewModel),
            new PropertyMetadata(false));
        private static readonly DependencyProperty SearchProgressActiveProperty = SearchProgressActivePropertyKey.DependencyProperty;

        public Boolean SearchProgressActive
        {
            get { return (Boolean)GetValue(SearchProgressActiveProperty); }
            private set { SetValue(SearchProgressActivePropertyKey, value); }
        }
        #endregion

        #region Search Term
        private static readonly DependencyProperty SearchTermProperty = DependencyProperty.RegisterAttached(
            "SearchTerm",
            typeof(String),
            typeof(SearchViewModel),
            new PropertyMetadata(String.Empty));

        public String SearchTerm
        {
            get { return (String)GetValue(SearchTermProperty); }
            set { SetValue(SearchTermProperty, value); }
        }

        private DelegateCommand clearSearchTermCommand;
        public ICommand ClearSearchTermCommand
        { get { return clearSearchTermCommand ?? (clearSearchTermCommand = new DelegateCommand(() => SearchTerm = String.Empty)); } }
        #endregion

        #region MangaObject List
        private static readonly DependencyProperty SearchResultMangaObjectCollectionProperty = DependencyProperty.RegisterAttached(
            "SearchResultMangaObjectCollection",
            typeof(ObservableCollection<MangaObject>),
            typeof(SearchViewModel),
            new PropertyMetadata(new ObservableCollection<MangaObject>()));
        public ObservableCollection<MangaObject> SearchResultMangaObjectCollection
        {
            get { return (ObservableCollection<MangaObject>)GetValue(SearchResultMangaObjectCollectionProperty); }
            set { SetValue(SearchResultMangaObjectCollectionProperty, value); }
        }

        private static readonly DependencyProperty SelectedSearchResultMangaObjectProperty = DependencyProperty.RegisterAttached(
            "SelectedSearchResultMangaObject",
            typeof(MangaObject),
            typeof(SearchViewModel));
        public MangaObject SelectedSearchResultMangaObject
        {
            get { return (MangaObject)GetValue(SelectedSearchResultMangaObjectProperty); }
            set { SetValue(SelectedSearchResultMangaObjectProperty, value); }
        }
        #endregion

        #region Search Command
        private CancellationTokenSource SearchAsyncCTS { get; set; }

        private DelegateCommand<String> searchAsyncCommand;
        public ICommand SearchAsyncCommand
        { get { return searchAsyncCommand ?? (searchAsyncCommand = new DelegateCommand<String>(SearchAsync, CanSearchAsync)); } }

        private Boolean CanSearchAsync(String SearchTerm)
        {
            if (Equals(SearchTerm, null)) return false;
            SearchTerm = SearchTerm.Trim();
            if (String.IsNullOrWhiteSpace(SearchTerm)) return false;
            else if (SearchTerm.Length < 3) return false;
            return true;
        }

        private async void SearchAsync(String SearchTerm)
        {
            if (Equals(SearchTerm, null)) return;
            this.SearchTerm = SearchTerm.Trim();
            try { if (!Equals(SearchAsyncCTS, null)) { SearchAsyncCTS.Cancel(); } }
            catch { }
            using (SearchAsyncCTS = new CancellationTokenSource())
            {
                try
                {
                    List<MangaObject> MangaObjects = await App.ContentDownloadManager.SearchAsync(SearchTerm, SearchAsyncCTS.Token, SearchProgressReporter);
                    SearchResultMangaObjectCollection.Clear();
                    foreach (MangaObject MangaObject in MangaObjects)
                    { SearchResultMangaObjectCollection.Add(MangaObject); }
                    SelectedSearchResultMangaObject = SearchResultMangaObjectCollection.FirstOrDefault();
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { throw ex; }
                finally { SearchProgressActive = false; }
            }
        }
        #endregion

        #region Download Result Command
        private DelegateCommand<MangaObject> downloadResultCommand;
        public ICommand DownloadResultCommand
        { get { return downloadResultCommand ?? (downloadResultCommand = new DelegateCommand<MangaObject>(DownloadResult, CanDownloadResult)); } }

        private Boolean CanDownloadResult(MangaObject MangaObject)
        {
            if (Equals(MangaObject, null)) return false;
            return true;
        }

        private void DownloadResult(MangaObject MangaObject)
        { App.ContentDownloadManager.Download(MangaObject, false); }
        #endregion

        #endregion
    }
}

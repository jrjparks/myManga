using Core.MVVM;
using myManga_App.IO.Network;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace myManga_App.ViewModels
{
    public sealed class SearchViewModel : BaseViewModel
    {
        #region Progress
        private IProgress<Int32> SearchProgressReporter
        { get; set; }

        private static readonly DependencyProperty SearchProgressProperty = DependencyProperty.RegisterAttached(
            "SearchProgress",
            typeof(Int32),
            typeof(SearchViewModel),
            new PropertyMetadata(0));

        public Int32 SearchProgress
        {
            get { return (Int32)GetValue(SearchProgressProperty); }
            set { SetValue(SearchProgressProperty, value); }
        }

        private static readonly DependencyProperty SearchProgressActiveProperty = DependencyProperty.RegisterAttached(
            "SearchProgressActive",
            typeof(Boolean),
            typeof(SearchViewModel),
            new PropertyMetadata(false));

        public Boolean SearchProgressActive
        {
            get { return (Boolean)GetValue(SearchProgressActiveProperty); }
            set { SetValue(SearchProgressActiveProperty, value); }
        }
        #endregion

        #region MangaObject List
        private static readonly DependencyProperty MangaObjectCollectionProperty = DependencyProperty.RegisterAttached(
            "MangaObjectCollection",
            typeof(ObservableCollection<MangaObject>),
            typeof(SearchViewModel),
            new PropertyMetadata(new ObservableCollection<MangaObject>()));
        public ObservableCollection<MangaObject> MangaObjectCollection
        {
            get { return (ObservableCollection<MangaObject>)GetValue(MangaObjectCollectionProperty); }
            set { SetValue(MangaObjectCollectionProperty, value); }
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
        #endregion

        #region Search
        private CancellationTokenSource SearchAsyncCTS { get; set; }

        #region Search Properties
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
        #endregion

        #region Clear SearchTerm Command
        private DelegateCommand clearSearchCommand;
        public ICommand ClearSearchCommand
        { get { return clearSearchCommand ?? (clearSearchCommand = new DelegateCommand(ClearSearch, CanClearSearch)); } }

        private Boolean CanClearSearch()
        {
            if (Equals(SearchTerm, null)) return false;
            if (String.IsNullOrWhiteSpace(SearchTerm)) return false;
            return true;
        }

        private void ClearSearch()
        { SearchTerm = String.Empty; }
        #endregion

        #region Search Command
        private DelegateCommand<String> searchCommandAsync;
        public ICommand SearchCommandAsync
        { get { return searchCommandAsync ?? (searchCommandAsync = new DelegateCommand<String>(SearchAsync, CanSearchAsync)); } }

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
            try { if (!Equals(SearchAsyncCTS, null)) { SearchAsyncCTS.Cancel(); } }
            catch { }
            using (SearchAsyncCTS = new CancellationTokenSource())
            {
                try
                {
                    List<MangaObject> MangaObjects = await App.ContentDownloadManager.SearchAsync(SearchTerm, SearchAsyncCTS.Token, SearchProgressReporter);
                    MangaObjectCollection.Clear();
                    foreach (MangaObject MangaObject in MangaObjects)
                    { MangaObjectCollection.Add(MangaObject); }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { throw ex; }
                finally { SearchProgressActive = false; }
            }
        }

        private void SearchAsyncRequest(String SearchTerm)
        {
            Messenger.Default.Send(this, "FocusRequest");
            this.SearchTerm = SearchTerm.Trim();
            SearchAsync(SearchTerm);
        }
        #endregion
        #endregion

        #region Download Search Result MangaObject
        private DelegateCommand<MangaObject> downloadMangaObjectCommandAsync;
        public ICommand DownloadMangaObjectCommandAsync
        { get { return downloadMangaObjectCommandAsync ?? (downloadMangaObjectCommandAsync = new DelegateCommand<MangaObject>(DownloadMangaObjectAsync, CanDownloadMangaObjectAsync)); } }

        private Boolean CanDownloadMangaObjectAsync(MangaObject MangaObject)
        {
            if (Equals(MangaObject, null)) return false;
            return true;
        }

        private void DownloadMangaObjectAsync(MangaObject MangaObject)
        { Task.Run(() => App.ContentDownloadManager.DownloadAsync(MangaObject, false, SearchProgressReporter)); }
        #endregion

        public SearchViewModel()
            : base()
        {
            if (!IsInDesignMode)
            {
                SearchProgressReporter = new Progress<Int32>(ProgressValue =>
                {
                    SearchProgressActive = (0 < ProgressValue && ProgressValue < 100);
                    SearchProgress = ProgressValue;
                });
                Messenger.Default.RegisterRecipient<String>(this, SearchAsyncRequest, "SearchRequest");
            }
        }
    }
}

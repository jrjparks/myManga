using myManga_App.IO.Local.Object;
using myMangaSiteExtension.Enums;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace myManga_App.IO.Network
{
    public sealed class ContentDownloadManager : IDisposable
    {
        #region Read-Only
        private readonly App App = App.Current as App;
        private readonly TimeSpan FILE_ACCESS_TIMEOUT = TimeSpan.FromMinutes(30);
        private readonly TimeSpan DOWNLOAD_TIMEOUT = TimeSpan.FromSeconds(10);

        private readonly TimeSpan DEFAULT_DELAY = TimeSpan.FromSeconds(1);
        private readonly TimeSpan DELAY_INCREMENT = TimeSpan.FromSeconds(1);

        private readonly SemaphoreSlim TaskConcurrencySemaphore;
        private readonly SemaphoreSlim ImageTaskConcurrencySemaphore;
        private readonly TaskFactory ContentTaskFactory;
        private readonly CancellationTokenSource cts;

        private readonly MemoryCache ActiveDownloadsCache;
        #endregion

        #region Properties
        public Int32 DownloadConcurrency
        { get; private set; }
        public Int32 ImageDownloadConcurrency
        { get; private set; }

        public Int32 ActiveDownloadCount
        { get { return (DownloadConcurrency + ImageDownloadConcurrency) - (TaskConcurrencySemaphore.CurrentCount + ImageTaskConcurrencySemaphore.CurrentCount); } }

        public Int32 MaxActiveDownloadCount
        { get { return (DownloadConcurrency + ImageDownloadConcurrency); } }

        public Int64 TotalDownloadCount
        { get { return ActiveDownloadsCache.GetCount(); } }

        public Boolean IsActive
        { get { return TotalDownloadCount > 0; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new ContentDownloadManager
        /// DownloadConcurrency = Environment.ProcessorCount * ConcurrencyMultiplier;
        /// ImageDownloadConcurrency = DownloadConcurrency / 2;
        /// </summary>
        /// <param name="ConcurrencyMultiplier">Default is 1.</param>
        public ContentDownloadManager(Int32 ConcurrencyMultiplier = 1)
        {
            ActiveDownloadsCache = new MemoryCache("ActiveDownloadsCache");

            DownloadConcurrency = Environment.ProcessorCount * ConcurrencyMultiplier;
            ImageDownloadConcurrency = DownloadConcurrency / 2;
            TaskConcurrencySemaphore = new SemaphoreSlim(DownloadConcurrency, DownloadConcurrency);
            ImageTaskConcurrencySemaphore = new SemaphoreSlim(ImageDownloadConcurrency, ImageDownloadConcurrency);
            ServicePointManager.DefaultConnectionLimit = DownloadConcurrency;
            
            cts = new CancellationTokenSource();
            ContentTaskFactory = Task.Factory;
        }

        ~ContentDownloadManager()
        { Dispose(); }

        public void Dispose()
        {
            ActiveDownloadsCache.Dispose();
            TaskConcurrencySemaphore.Dispose();
            ImageTaskConcurrencySemaphore.Dispose();
            try
            {
                if (!Equals(cts, null))
                {
                    cts.Cancel();
                    cts.Dispose();
                }
            }
            catch { }
        }
        #endregion

        #region Save Paths
        private String SavePath(MangaObject MangaObject)
        {
            String SavePath = Path.Combine(App.MANGA_ARCHIVE_DIRECTORY, MangaObject.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION));
            Path.GetDirectoryName(SavePath).SafeFolder(); // Create folder tree if needed.
            return SavePath;
        }
        private String SavePath(MangaObject MangaObject, ChapterObject ChapterObject)
        {
            String SavePath = Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, MangaObject.MangaFileName(), ChapterObject.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION));
            Path.GetDirectoryName(SavePath).SafeFolder(); // Create folder tree if needed.
            return SavePath;
        }
        #endregion

        #region CacheKey
        public String CacheKey(MangaObject MangaObject)
        { return String.Format("{0}", MangaObject.MangaArchiveName("CACHE")); }
        public String CacheKey(MangaObject MangaObject, ChapterObject ChapterObject)
        { return String.Format("{0}/{1}", MangaObject.MangaArchiveName("CACHE"), ChapterObject.ChapterArchiveName("CACHE")); }
        public String CacheKey(MangaObject MangaObject, ChapterObject ChapterObject, PageObject PageObject)
        { return String.Format("{0}/{1}/{2}", MangaObject.MangaArchiveName("CACHE"), ChapterObject.ChapterArchiveName("CACHE"), PageObject.PageNumber); }

        public Boolean IsCacheKeyActive(String CacheKey)
        { return ActiveDownloadsCache.Contains(CacheKey); }
        #endregion

        #region Download MangaObject
        public void Download(MangaObject MangaObject, Boolean Refresh = true, IProgress<Int32> ProgressReporter = null)
        { Task.Run(() => DownloadAsync(MangaObject, Refresh, ProgressReporter)); }

        public async Task DownloadAsync(MangaObject MangaObject, Boolean Refresh = true, IProgress<Int32> ProgressReporter = null)
        {
            if (ActiveDownloadsCache.Contains(CacheKey(MangaObject))) return;
            ActiveDownloadsCache.Set(CacheKey(MangaObject), true, DateTimeOffset.MaxValue);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                // Load the MangaObject via Async and LimitedTaskFactory
                MangaObject = await ContentTaskFactory.StartNew(() => LoadMangaObjectAsync(MangaObject, cts.Token, ProgressReporter)).Unwrap();
                // Calculate MangaObject Save Path
                // Save the MangaObject via Async to Save Path with Retry and Timeout of 30min
                await StoreMangaObject(MangaObject);
                if (!Refresh)
                {
                    foreach (LocationObject CoverImageLocation in MangaObject.CoverLocations)
                    { DownloadCover(MangaObject, CoverImageLocation); }
                }
                // Write Async Verify
            }
            catch (Exception ex)
            { throw ex; }
            finally
            {
                ActiveDownloadsCache.Remove(CacheKey(MangaObject));
                if (!Equals(ProgressReporter, null)) ProgressReporter.Report(100);
            }
        }

        private async Task StoreMangaObject(MangaObject MangaObject)
        {
            await App.ZipManager.Retry(() => App.ZipManager.WriteAsync(
                SavePath(MangaObject),
                MangaObject.GetType().Name,
                MangaObject.Serialize(SerializeType: App.UserConfiguration.SerializeType)
                ), FILE_ACCESS_TIMEOUT, DEFAULT_DELAY, DELAY_INCREMENT);
        }
        #endregion

        #region Download ChapterObject
        public void Download(MangaObject MangaObject, ChapterObject ChapterObject, IProgress<Int32> ProgressReporter = null)
        { Task.Run(() => DownloadAsync(MangaObject, ChapterObject, ProgressReporter)); }

        public async Task DownloadAsync(MangaObject MangaObject, ChapterObject ChapterObject, IProgress<Int32> ProgressReporter = null)
        {
            if (ActiveDownloadsCache.Contains(CacheKey(MangaObject, ChapterObject))) return;
            ActiveDownloadsCache.Set(CacheKey(MangaObject, ChapterObject), true, DateTimeOffset.MaxValue);

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                // Load the ChapterObject via Async and LimitedTaskFactory
                IProgress<Int32> ChapterObjectProgressReporter = new Progress<Int32>(ProgressValue =>
                {
                    if (!Equals(ProgressReporter, null)) ProgressReporter.Report((Int32)Math.Round((Double)ProgressValue * 0.25));
                });
                ChapterObject = await ContentTaskFactory.StartNew(() => LoadChapterObjectAsync(
                    MangaObject,
                    ChapterObject,
                    cts.Token,
                    ChapterObjectProgressReporter)).Unwrap();

                if (!ChapterObject.Pages.Count.Equals(0))
                { // Check for pages
                  // Save the ChapterObject via Async to Save Path with Retry and Timeout of 30min
                    await StoreChapterObject(MangaObject, ChapterObject);

                    // Load the PageObjects from the ChapterObject
                    List<Task<PageObject>> PageObjectDownloadTasks = new List<Task<PageObject>>();

                    foreach (PageObject PageObject in ChapterObject.Pages)
                    {
                        PageObjectDownloadTasks.Add(ContentTaskFactory.StartNew(() => LoadPageObjectAsync(
                            MangaObject,
                            ChapterObject,
                            PageObject,
                            cts.Token,
                            null)).Unwrap());
                    }

                    foreach (Task<PageObject> PageObjectDownloadTask in PageObjectDownloadTasks)
                    {
                        PageObject PageObject = await PageObjectDownloadTask;
                        Int32 index = ChapterObject.Pages.FindIndex(_PageObject => Equals(_PageObject.PageNumber, PageObject.PageNumber));
                        ChapterObject.Pages[index] = PageObject;
                    }
                    await StoreChapterObject(MangaObject, ChapterObject);

                    // Download Images
                    IEnumerable<Task> DownloadImageTasksQuery =
                    from PageObject in ChapterObject.Pages
                    select DownloadImageAsync(
                        PageObject.ImgUrl,
                        PageObject.Url,
                        App.SiteExtensions.DLLCollection.First(_SiteExtension => PageObject.Url.Contains(_SiteExtension.SiteExtensionDescriptionAttribute.URLFormat)).Cookies,
                        SavePath(MangaObject, ChapterObject),
                        Path.GetFileName(new Uri(PageObject.ImgUrl).LocalPath));
                    List<Task> DownloadImageTasks = DownloadImageTasksQuery.ToList();
                    Int32 OriginalDownloadImageTasksCount = DownloadImageTasks.Count;

                    while (DownloadImageTasks.Count > 0)
                    {
                        Task completedTask = await Task.WhenAny(DownloadImageTasks);
                        DownloadImageTasks.Remove(completedTask);

                        Int32 DownloadImageTasksProgress = (Int32)Math.Round(((Double)(OriginalDownloadImageTasksCount - DownloadImageTasks.Count) / (Double)OriginalDownloadImageTasksCount) * 75);
                        if (!Equals(ProgressReporter, null)) ProgressReporter.Report(25 + DownloadImageTasksProgress);
                    }
                }
            }
            catch (Exception ex)
            { throw ex; }
            finally
            {
                ActiveDownloadsCache.Remove(CacheKey(MangaObject, ChapterObject));
                if (!Equals(ProgressReporter, null)) ProgressReporter.Report(100);
            }
        }

        private async Task StoreChapterObject(MangaObject MangaObject, ChapterObject ChapterObject)
        {
            await App.ZipManager.Retry(() => App.ZipManager.WriteAsync(
                SavePath(MangaObject, ChapterObject),
                ChapterObject.GetType().Name,
                ChapterObject.Serialize(SerializeType: App.UserConfiguration.SerializeType)
                ), FILE_ACCESS_TIMEOUT, DEFAULT_DELAY, DELAY_INCREMENT);
        }
        #endregion

        #region Download PageObject
        public void Download(MangaObject MangaObject, ChapterObject ChapterObject, PageObject PageObject, IProgress<Int32> ProgressReporter = null)
        { Task.Run(() => DownloadAsync(MangaObject, ChapterObject, PageObject, ProgressReporter)); }

        public async Task DownloadAsync(MangaObject MangaObject, ChapterObject ChapterObject, PageObject PageObject, IProgress<Int32> ProgressReporter = null)
        {
            if (ActiveDownloadsCache.Contains(CacheKey(MangaObject, ChapterObject, PageObject))) return;
            ActiveDownloadsCache.Set(CacheKey(MangaObject, ChapterObject, PageObject), true, DateTimeOffset.MaxValue);

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                // Load the PageObject via Async and LimitedTaskFactory
                PageObject = await ContentTaskFactory.StartNew(() => LoadPageObjectAsync(
                    MangaObject,
                    ChapterObject,
                    PageObject,
                    cts.Token,
                    ProgressReporter)).Unwrap();

                ChapterObject = await StorePageObject(MangaObject, ChapterObject, PageObject);

                ISiteExtension SiteExtension = App.SiteExtensions.DLLCollection.First(_SiteExtension =>
                { return PageObject.Url.Contains(_SiteExtension.SiteExtensionDescriptionAttribute.URLFormat); });
                // Start the DownloadImage task, don't wait.
                DownloadImage(PageObject.ImgUrl, PageObject.Url, SiteExtension.Cookies, SavePath(MangaObject, ChapterObject), Path.GetFileName(new Uri(PageObject.ImgUrl).LocalPath));
            }
            catch (Exception ex)
            { throw ex; }
            finally
            {
                ActiveDownloadsCache.Remove(CacheKey(MangaObject, ChapterObject, PageObject));
                if (!Equals(ProgressReporter, null)) ProgressReporter.Report(100);
            }
        }

        private async Task<ChapterObject> StorePageObject(MangaObject MangaObject, ChapterObject ChapterObject, PageObject PageObject)
        {
            Int32 index = ChapterObject.Pages.FindIndex(_PageObject => Equals(_PageObject.PageNumber, PageObject.PageNumber));
            ChapterObject.Pages[index] = PageObject;
            // Save the ChapterObject via Async to Save Path with Retry and Timeout of 30min
            await App.ZipManager.Retry(() => App.ZipManager.WriteAsync(
                SavePath(MangaObject, ChapterObject),
                ChapterObject.GetType().Name,
                ChapterObject.Serialize(SerializeType: App.UserConfiguration.SerializeType)
                ), FILE_ACCESS_TIMEOUT, DEFAULT_DELAY, DELAY_INCREMENT);

            return ChapterObject;
        }
        #endregion

        #region Download Cover
        public void DownloadCover(MangaObject MangaObject, LocationObject LocationObject)
        {
            Uri CoverImageUri = new Uri(LocationObject.Url);
            // Load the Cover Image via Async and LimitedTaskFactory
            CookieCollection Cookies = null;
            String Referer = String.Format("{0}://{1}", CoverImageUri.Scheme, CoverImageUri.Host);
            if (App.SiteExtensions.DLLCollection.Contains(LocationObject.ExtensionName))
            {
                Cookies = App.SiteExtensions.DLLCollection[LocationObject.ExtensionName].Cookies;
                Referer = App.SiteExtensions.DLLCollection[LocationObject.ExtensionName].SiteExtensionDescriptionAttribute.RefererHeader;
            }
            else if (App.DatabaseExtensions.DLLCollection.Contains(LocationObject.ExtensionName))
            {
                Referer = App.DatabaseExtensions.DLLCollection[LocationObject.ExtensionName].DatabaseExtensionDescriptionAttribute.RefererHeader;
            }
            DownloadImage(
                LocationObject.Url,
                Referer,
                Cookies,
                SavePath(MangaObject),
                Path.GetFileName(CoverImageUri.LocalPath));
        }
        #endregion

        #region Download Image
        public void DownloadImage(String Url, String Referer, CookieCollection Cookies, String ArchiveName, String EntryName, IProgress<Int32> ProgressReporter = null)
        { Task.Run(() => DownloadImageAsync(Url, Referer, Cookies, ArchiveName, EntryName, ProgressReporter)); }

        public async Task DownloadImageAsync(String Url, String Referer, CookieCollection Cookies, String ArchiveName, String EntryName, IProgress<Int32> ProgressReporter = null)
        {
            if (ActiveDownloadsCache.Contains(Url)) return;
            ActiveDownloadsCache.Set(Url, true, DateTimeOffset.MaxValue);
            try
            {
                using (Stream ImageStream = await ContentTaskFactory.StartNew(() => LoadImageAsync(Url, Referer, Cookies, cts.Token, ProgressReporter)).Unwrap())
                {
                    // Save the Image via Async to Save Path with Retry and Timeout of 30min
                    await StoreImage(ArchiveName, EntryName, ImageStream);
                }
            }
            catch (Exception ex)
            { throw ex; }
            finally
            {
                ActiveDownloadsCache.Remove(Url);
                if (!Equals(ProgressReporter, null)) ProgressReporter.Report(100);
            }
        }

        private async Task StoreImage(String ArchiveName, String EntryName, Stream ImageStream)
        {
            await App.ZipManager.Retry(() => App.ZipManager.WriteAsync(
                ArchiveName,
                EntryName,
                ImageStream
                ), FILE_ACCESS_TIMEOUT, DEFAULT_DELAY, DELAY_INCREMENT);
        }
        #endregion

        #region Search
        public async Task<List<MangaObject>> SearchAsync(String SearchTerm, CancellationToken ct, IProgress<int> progress = null)
        {
            using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token))
            { return await Task.Factory.StartNew(() => SearchMangaObjectAsync(SearchTerm, linkedCts.Token, progress)).Unwrap(); }
        }
        #endregion

        #region Async Methods
        #region Async Method Classes
        private sealed class ExtensionContentResult
        {
            public IExtension Extension { get; set; }
            public String Content { get; set; }
        }
        #endregion

        #region Retry
        public async Task<TResult> Retry<TResult>(Func<Task<TResult>> method, TimeSpan timeout)
        { return await Retry(method: method, timeout: timeout, delay: TimeSpan.FromSeconds(1), delayIncrement: TimeSpan.Zero); }
        public async Task<TResult> Retry<TResult>(Func<Task<TResult>> method, TimeSpan timeout, TimeSpan delay)
        { return await Retry(method: method, timeout: timeout, delay: delay, delayIncrement: TimeSpan.Zero); }
        public async Task<TResult> Retry<TResult>(Func<Task<TResult>> method, TimeSpan timeout, TimeSpan delay, TimeSpan delayIncrement)
        {
            Stopwatch watch = Stopwatch.StartNew();
            do
            {
                try { return await method(); }
                catch (OperationCanceledException ocex)
                { throw ocex; } // Handle OperationCanceledException and throw it.
                catch (Exception ex)
                {
                    // If the timeout has elapsed, throw the Exception.
                    if (watch.Elapsed >= timeout)
                        throw ex;

                    // await for the delay.
                    await Task.Delay(delay);

                    // If there is a delayIncrement and it's greater than 0 add it to the delay.
                    if (delayIncrement > TimeSpan.Zero)
                        delay.Add(delayIncrement);
                }
            }
            while (watch.Elapsed < timeout);
            // A timeout occurred.
            // return the default(TResult).
            return default(TResult);
        }
        #endregion

        #region Manga
        private async Task<MangaObject> LoadMangaObjectAsync(MangaObject MangaObject, CancellationToken ct, IProgress<Int32> progress)
        {
            try
            {
                await TaskConcurrencySemaphore.WaitAsync(ct);
                ct.ThrowIfCancellationRequested();

                // Store valid ISiteExtension
                IEnumerable<ISiteExtension> ValidSiteExtensions = App.SiteExtensions.DLLCollection.Where(SiteExtension =>
                {
                    if (!SiteExtension.SiteExtensionDescriptionAttribute.SupportedObjects.HasFlag(SupportedObjects.Manga)) return false;
                    if (SiteExtension.SiteExtensionDescriptionAttribute.RequiresAuthentication)
                        if (!SiteExtension.IsAuthenticated) return false;
                    if (!App.UserConfiguration.EnabledSiteExtensions.Contains(SiteExtension.SiteExtensionDescriptionAttribute.Name)) return false;
                    return true;
                });
                // Store valid IDatabaseExtension
                IEnumerable<IDatabaseExtension> ValidDatabaseExtension = App.DatabaseExtensions.DLLCollection.Where(DatabaseExtension =>
                {
                    if (!DatabaseExtension.DatabaseExtensionDescriptionAttribute.SupportedObjects.HasFlag(SupportedObjects.Manga)) return false;
                    if (DatabaseExtension.DatabaseExtensionDescriptionAttribute.RequiresAuthentication)
                        if (!DatabaseExtension.IsAuthenticated) return false;
                    if (!App.UserConfiguration.EnabledDatabaseExtensions.Contains(DatabaseExtension.DatabaseExtensionDescriptionAttribute.Name)) return false;
                    return true;
                });
                List<IExtension> ValidExtensions = new List<IExtension>();
                ValidExtensions.AddRange(ValidSiteExtensions);
                ValidExtensions.AddRange(ValidDatabaseExtension);

                if (!Equals(progress, null)) progress.Report(5);

                IEnumerable<Task<ExtensionContentResult>> ExtensionContentTasksQuery =
                    from Extension in ValidExtensions select LoadExtensionMangaContent(Extension, MangaObject);
                List<Task<ExtensionContentResult>> ExtensionContentTasks = ExtensionContentTasksQuery.ToList();
                Int32 OriginalExtensionContentTasksCount = ExtensionContentTasks.Count;

                if (!Equals(progress, null)) progress.Report(10);
                while (ExtensionContentTasks.Count > 0)
                {   // Load Content via Async and process as they complete.
                    ct.ThrowIfCancellationRequested();
                    Task<ExtensionContentResult> completedTask = await Task.WhenAny(ExtensionContentTasks);
                    ExtensionContentTasks.Remove(completedTask);
                    ct.ThrowIfCancellationRequested();

                    ExtensionContentResult LoadedExtensionContentResult = await completedTask;
                    if (!Equals(LoadedExtensionContentResult, null))
                    {
                        String Content = LoadedExtensionContentResult.Content;
                        if (LoadedExtensionContentResult.Extension is ISiteExtension)
                        {
                            ISiteExtension SiteExtension = LoadedExtensionContentResult.Extension as ISiteExtension;
                            try
                            {
                                MangaObject DownloadedMangaObject = SiteExtension.ParseMangaObject(Content);
                                if (!Equals(DownloadedMangaObject, null)) MangaObject.Merge(DownloadedMangaObject);
                            }
                            catch (Exception)
                            {
                                Int32 idx = MangaObject.Locations.FindIndex(_LocationObject => Equals(_LocationObject.ExtensionName, SiteExtension.SiteExtensionDescriptionAttribute.Name));
                                MangaObject.Locations[idx].Enabled = false;
                            }
                        }
                        else if (LoadedExtensionContentResult.Extension is IDatabaseExtension)
                        {
                            IDatabaseExtension DatabaseExtension = LoadedExtensionContentResult.Extension as IDatabaseExtension;
                            try
                            {
                                DatabaseObject DownloadedDatabaseObject = DatabaseExtension.ParseDatabaseObject(Content);
                                if (!Equals(DownloadedDatabaseObject, null)) MangaObject.AttachDatabase(DownloadedDatabaseObject, preferDatabaseDescription: true);
                            }
                            catch (Exception)
                            {
                                Int32 idx = MangaObject.DatabaseLocations.FindIndex(_LocationObject => Equals(_LocationObject.ExtensionName, DatabaseExtension.DatabaseExtensionDescriptionAttribute.Name));
                                MangaObject.DatabaseLocations[idx].Enabled = false;
                            }
                        }
                    }

                    Int32 ExtensionContentTasksProgress = (Int32)Math.Round(((Double)(OriginalExtensionContentTasksCount - ExtensionContentTasks.Count) / (Double)OriginalExtensionContentTasksCount) * 80);
                    if (!Equals(progress, null)) progress.Report(10 + ExtensionContentTasksProgress);
                }

                ct.ThrowIfCancellationRequested();
                if (!Equals(progress, null)) progress.Report(100);
                return MangaObject;
            }
            finally { TaskConcurrencySemaphore.Release(); }
        }

        /// <summary>
        /// Download the content of the IExtension for the MangaObject
        /// </summary>
        /// <param name="Extension">The IExtension to use for Cookies and Referer</param>
        /// <param name="MangaObject">The MangaObject used</param>
        /// <returns></returns>
        private async Task<ExtensionContentResult> LoadExtensionMangaContent(IExtension Extension, MangaObject MangaObject)
        {
            // Locate the LocationObject for the requested IExtension
            List<LocationObject> MangaInfoLocationObjects = new List<LocationObject>();
            MangaInfoLocationObjects.AddRange(MangaObject.Locations);
            MangaInfoLocationObjects.AddRange(MangaObject.DatabaseLocations);
            LocationObject LocationObject = MangaInfoLocationObjects.FirstOrDefault(_LocationObject =>
            {
                if (Extension is ISiteExtension)
                { if (!Equals(_LocationObject.ExtensionName, (Extension as ISiteExtension).SiteExtensionDescriptionAttribute.Name)) return false; }
                else if (Extension is IDatabaseExtension)
                { if (!Equals(_LocationObject.ExtensionName, (Extension as IDatabaseExtension).DatabaseExtensionDescriptionAttribute.Name)) return false; }
                if (Equals(_LocationObject.Enabled, false)) return false;
                return true;
            });
            if (Equals(LocationObject, null)) // If there is not a match return null
            { return null; }
            using (WebDownloader WebDownloader = new WebDownloader(Extension.Cookies))
            {
                if (Extension is ISiteExtension)
                { WebDownloader.Referer = (Extension as ISiteExtension).SiteExtensionDescriptionAttribute.RefererHeader; }
                else if (Extension is IDatabaseExtension)
                { WebDownloader.Referer = (Extension as IDatabaseExtension).DatabaseExtensionDescriptionAttribute.RefererHeader; }
                try
                {
                    String Content = await Retry(() => WebDownloader.DownloadStringTaskAsync(LocationObject.Url), DOWNLOAD_TIMEOUT);
                    return new ExtensionContentResult() { Extension = Extension, Content = Content };
                }
                catch
                { return null; }
            }
        }
        #endregion

        #region Chapter
        private async Task<ChapterObject> LoadChapterObjectAsync(MangaObject MangaObject, ChapterObject ChapterObject, CancellationToken ct, IProgress<Int32> progress)
        {
            try
            {
                await TaskConcurrencySemaphore.WaitAsync(ct);
                ct.ThrowIfCancellationRequested();

                // Store valid ISiteExtension
                IEnumerable<ISiteExtension> ValidSiteExtensions = App.SiteExtensions.DLLCollection.Where(_ =>
                {
                    if (!_.SiteExtensionDescriptionAttribute.SupportedObjects.HasFlag(SupportedObjects.Manga)) return false;
                    if (_.SiteExtensionDescriptionAttribute.RequiresAuthentication)
                        if (!_.IsAuthenticated) return false;
                    if (!App.UserConfiguration.EnabledSiteExtensions.Contains(_.SiteExtensionDescriptionAttribute.Name)) return false;
                    return true;
                });

                IEnumerable<LocationObject> OrderedChapterObjectLocations = ChapterObject.Locations.OrderBy(_ => App.UserConfiguration.EnabledSiteExtensions.IndexOf(_.ExtensionName));
                foreach(LocationObject LocationObject in OrderedChapterObjectLocations)
                {
                    ct.ThrowIfCancellationRequested();
                    ISiteExtension SiteExtension = ValidSiteExtensions.FirstOrDefault(_ => Equals(_.SiteExtensionDescriptionAttribute.Name, LocationObject.ExtensionName));
                    if (Equals(SiteExtension, null)) continue;  // Continue with the foreach loop

                    ct.ThrowIfCancellationRequested();
                    using (WebDownloader WebDownloader = new WebDownloader(SiteExtension.Cookies))
                    {
                        WebDownloader.Referer = SiteExtension.SiteExtensionDescriptionAttribute.RefererHeader;
                        DownloadProgressChangedEventHandler ProgressEventHandler = (s, e) =>
                        {
                            if (!Equals(progress, null))
                                progress.Report((Int32)Math.Round((Double)e.ProgressPercentage * 0.9));
                            ct.ThrowIfCancellationRequested();
                        };
                        WebDownloader.DownloadProgressChanged += ProgressEventHandler;
                        ChapterObject DownloadedChapterObject = await Retry(
                            async () => SiteExtension.ParseChapterObject(await WebDownloader.DownloadStringTaskAsync(LocationObject.Url)),
                            DOWNLOAD_TIMEOUT);
                        WebDownloader.DownloadProgressChanged -= ProgressEventHandler;
                        ct.ThrowIfCancellationRequested();
                        if (!Equals(DownloadedChapterObject, null))
                        {
                            ChapterObject.Merge(DownloadedChapterObject);
                            ChapterObject.Pages = DownloadedChapterObject.Pages;
                            break;  // Break free of the foreach loop
                        }
                    }
                }
                if (!Equals(progress, null)) progress.Report(100);
                ct.ThrowIfCancellationRequested();
                return ChapterObject;
            }
            finally { TaskConcurrencySemaphore.Release(); }
        }
        #endregion

        #region Page
        private async Task<PageObject> LoadPageObjectAsync(MangaObject MangaObject, ChapterObject ChapterObject, PageObject PageObject, CancellationToken ct, IProgress<Int32> progress)
        {
            try
            {
                await TaskConcurrencySemaphore.WaitAsync(ct);
                ct.ThrowIfCancellationRequested();

                ISiteExtension SiteExtension = App.SiteExtensions.DLLCollection.First(_SiteExtension => PageObject.Url.Contains(_SiteExtension.SiteExtensionDescriptionAttribute.URLFormat));
                using (WebDownloader WebDownloader = new WebDownloader(SiteExtension.Cookies))
                {
                    WebDownloader.Referer = SiteExtension.SiteExtensionDescriptionAttribute.RefererHeader;
                    DownloadProgressChangedEventHandler ProgressEventHandler = (s, e) =>
                    {
                        if (!Equals(progress, null))
                            progress.Report((Int32)Math.Round((Double)e.ProgressPercentage * 0.9));
                        ct.ThrowIfCancellationRequested();
                    };
                    WebDownloader.DownloadProgressChanged += ProgressEventHandler;
                    PageObject = await Retry(
                        async () => SiteExtension.ParsePageObject(await WebDownloader.DownloadStringTaskAsync(PageObject.Url)),
                        DOWNLOAD_TIMEOUT);
                    WebDownloader.DownloadProgressChanged -= ProgressEventHandler;
                }
                if (!Equals(progress, null)) progress.Report(100);
                return PageObject;
            }
            finally { TaskConcurrencySemaphore.Release(); }
        }
        #endregion

        #region Image
        private async Task<Stream> LoadImageAsync(String Url, String Referer, CookieCollection Cookies, CancellationToken ct, IProgress<Int32> progress)
        {
            try
            {
                await ImageTaskConcurrencySemaphore.WaitAsync(ct);
                ct.ThrowIfCancellationRequested();

                using (WebDownloader WebDownloader = new WebDownloader(Cookies))
                {
                    WebDownloader.Referer = Referer;
                    DownloadProgressChangedEventHandler ProgressEventHandler = (s, e) =>
                    {
                        if (!Equals(progress, null)) progress.Report(e.ProgressPercentage);
                        ct.ThrowIfCancellationRequested();
                    };
                    WebDownloader.DownloadProgressChanged += ProgressEventHandler;
                    Stream ImageStream = new MemoryStream();
                    using (Stream WebStream = await WebDownloader.OpenReadTaskAsync(Url))
                    { await WebStream.CopyToAsync(ImageStream); }
                    WebDownloader.DownloadProgressChanged -= ProgressEventHandler;
                    ImageStream.Seek(0, SeekOrigin.Begin);

                    if (ct.IsCancellationRequested) ImageStream.Dispose();
                    ct.ThrowIfCancellationRequested();

                    if (!Equals(progress, null)) progress.Report(100);
                    return ImageStream;
                }
            }
            finally { ImageTaskConcurrencySemaphore.Release(); }
        }
        #endregion

        #region Search
        private readonly Regex SafeAlphaNumeric = new Regex("[^a-z0-9]", RegexOptions.IgnoreCase);

        private async Task<List<MangaObject>> SearchMangaObjectAsync(String SearchTerm, CancellationToken ct, IProgress<Int32> progress)
        {
            List<MangaObject> SearchResults = new List<MangaObject>();

            // Store valid ISiteExtension
            IEnumerable<ISiteExtension> ValidSiteExtensions = App.SiteExtensions.DLLCollection.Where(SiteExtension =>
            {
                if (!SiteExtension.SiteExtensionDescriptionAttribute.SupportedObjects.HasFlag(SupportedObjects.Manga)) return false;
                if (SiteExtension.SiteExtensionDescriptionAttribute.RequiresAuthentication)
                    if (!SiteExtension.IsAuthenticated) return false;
                if (!App.UserConfiguration.EnabledSiteExtensions.Contains(SiteExtension.SiteExtensionDescriptionAttribute.Name)) return false;
                return true;
            });
            // Store valid IDatabaseExtension
            IEnumerable<IDatabaseExtension> ValidDatabaseExtension = App.DatabaseExtensions.DLLCollection.Where(DatabaseExtension =>
            {
                if (!DatabaseExtension.DatabaseExtensionDescriptionAttribute.SupportedObjects.HasFlag(SupportedObjects.Manga)) return false;
                if (DatabaseExtension.DatabaseExtensionDescriptionAttribute.RequiresAuthentication)
                    if (!DatabaseExtension.IsAuthenticated) return false;
                if (!App.UserConfiguration.EnabledDatabaseExtensions.Contains(DatabaseExtension.DatabaseExtensionDescriptionAttribute.Name)) return false;
                return true;
            });
            List<IExtension> ValidExtensions = new List<IExtension>();
            ValidExtensions.AddRange(ValidSiteExtensions);
            ValidExtensions.AddRange(ValidDatabaseExtension);

            if (!Equals(progress, null)) progress.Report(5);

            IEnumerable<Task<ExtensionContentResult>> ExtensionContentTasksQuery =
                from Extension in ValidExtensions select LoadExtensionSearchContent(Extension, SearchTerm);
            List<Task<ExtensionContentResult>> ExtensionContentTasks = ExtensionContentTasksQuery.ToList();
            Int32 OriginalExtensionContentTasksCount = ExtensionContentTasks.Count;

            if (!Equals(progress, null)) progress.Report(10);
            while (ExtensionContentTasks.Count > 0)
            {   // Load Content via Async and process as they complete.
                ct.ThrowIfCancellationRequested();
                Task<ExtensionContentResult> completedTask = await Task.WhenAny(ExtensionContentTasks);
                ExtensionContentTasks.Remove(completedTask);
                ct.ThrowIfCancellationRequested();

                ExtensionContentResult LoadedExtensionContentResult = await completedTask;
                if (!Equals(LoadedExtensionContentResult, null))
                {
                    String Content = LoadedExtensionContentResult.Content;
                    if (LoadedExtensionContentResult.Extension is ISiteExtension)
                    {   // Extention was a ISiteExtension
                        ISiteExtension SiteExtension = LoadedExtensionContentResult.Extension as ISiteExtension;
                        List<SearchResultObject> SearchResultObjects = SiteExtension.ParseSearch(Content);
                        foreach (SearchResultObject SearchResultObject in SearchResultObjects)
                        {
                            MangaObject MangaObject = SearchResultObject.ConvertToMangaObject(),
                                ExistingMangaObject = SearchResults.FirstOrDefault(_MangaObject =>
                                {   // Locate an Existing MangaObject
                                    List<String> ExistingMangaObjectNames = new List<String>(_MangaObject.AlternateNames),
                                        MangaObjectNames = new List<String>(MangaObject.AlternateNames);
                                    ExistingMangaObjectNames.Insert(0, _MangaObject.Name);
                                    MangaObjectNames.Insert(0, MangaObject.Name);

                                    ExistingMangaObjectNames = ExistingMangaObjectNames.Select(_ExistingMangaObjectName => SafeAlphaNumeric.Replace(_ExistingMangaObjectName.ToLower(), String.Empty)).ToList();
                                    MangaObjectNames = MangaObjectNames.Select(_MangaObjectNames => SafeAlphaNumeric.Replace(_MangaObjectNames.ToLower(), String.Empty)).ToList();

                                    return ExistingMangaObjectNames.Intersect(MangaObjectNames).Any();
                                });
                            if (Equals(ExistingMangaObject, null)) SearchResults.Add(MangaObject);
                            else ExistingMangaObject.Merge(MangaObject);
                        }
                    }
                    else if (LoadedExtensionContentResult.Extension is IDatabaseExtension)
                    {   // Extention was a IDatabaseExtension
                        IDatabaseExtension DatabaseExtension = LoadedExtensionContentResult.Extension as IDatabaseExtension;
                        List<DatabaseObject> DatabaseObjects = DatabaseExtension.ParseSearch(Content);
                        foreach (DatabaseObject DatabaseObject in DatabaseObjects)
                        {
                            MangaObject ExistingMangaObject = SearchResults.FirstOrDefault(_MangaObject =>
                            {   // Locate an Existing MangaObject
                                List<String> ExistingMangaObjectNames = new List<String>(_MangaObject.AlternateNames),
                                DatabaseObjectNames = new List<String>(DatabaseObject.AlternateNames);

                                ExistingMangaObjectNames.Insert(0, _MangaObject.Name);
                                DatabaseObjectNames.Insert(0, DatabaseObject.Name);

                                ExistingMangaObjectNames = ExistingMangaObjectNames.Select(_ExistingMangaObjectName => SafeAlphaNumeric.Replace(_ExistingMangaObjectName.ToLower(), String.Empty)).ToList();
                                DatabaseObjectNames = DatabaseObjectNames.Select(_DatabaseObjectNames => SafeAlphaNumeric.Replace(_DatabaseObjectNames.ToLower(), String.Empty)).ToList();

                                Int32 IntersectCount = ExistingMangaObjectNames.Intersect(DatabaseObjectNames).Count(),
                                ExistingHalfCount = (Int32)Math.Ceiling((Double)ExistingMangaObjectNames.Count / 2);
                                return IntersectCount >= ExistingHalfCount;
                            });
                            if (!Equals(ExistingMangaObject, null))
                            {
                                if (Equals(ExistingMangaObject.DatabaseLocations.FindIndex(_DatabaseLocation => Equals(
                                    _DatabaseLocation.ExtensionName,
                                    DatabaseExtension.DatabaseExtensionDescriptionAttribute.Name)), -1))
                                {
                                    ExistingMangaObject.AttachDatabase(DatabaseObject, preferDatabaseDescription: true);
                                }
                            }
                        }
                    }
                }

                Int32 ExtensionContentTasksProgress = (Int32)Math.Round(((Double)(OriginalExtensionContentTasksCount - ExtensionContentTasks.Count) / (Double)OriginalExtensionContentTasksCount) * 70);
                if (!Equals(progress, null)) progress.Report(10 + ExtensionContentTasksProgress);
            }

            if (!Equals(progress, null)) progress.Report(850);
            ct.ThrowIfCancellationRequested();
            // Merge same items
            for (Int32 index = 0; index < SearchResults.Count; ++index)
            {
                ct.ThrowIfCancellationRequested();
                MangaObject item = SearchResults[index];
                for (Int32 sub_index = index + 1; sub_index < SearchResults.Count;)
                {
                    ct.ThrowIfCancellationRequested();
                    MangaObject sub_item = SearchResults[sub_index];
                    List<String> ExistingMangaObjectNames = new List<String>(item.AlternateNames),
                        MangaObjectNames = new List<String>(sub_item.AlternateNames);
                    ExistingMangaObjectNames.Insert(0, item.Name);
                    MangaObjectNames.Insert(0, sub_item.Name);

                    ExistingMangaObjectNames = ExistingMangaObjectNames.Select(_ExistingMangaObjectName => SafeAlphaNumeric.Replace(_ExistingMangaObjectName.ToLower(), String.Empty)).ToList();
                    MangaObjectNames = MangaObjectNames.Select(_MangaObjectNames => SafeAlphaNumeric.Replace(_MangaObjectNames.ToLower(), String.Empty)).ToList();

                    Int32 IntersectCount = ExistingMangaObjectNames.Intersect(MangaObjectNames).Count(),
                        ExistingThirdCount = (Int32)Math.Ceiling((Double)ExistingMangaObjectNames.Count / 3);
                    if (IntersectCount >= ExistingThirdCount)
                    {
                        item.Merge(sub_item);
                        SearchResults.RemoveAt(sub_index);
                    }
                    else ++sub_index;

                    Int32 MergeProgress = (Int32)Math.Round((Double)index / (Double)SearchResults.Count * 10);
                    if (!Equals(progress, null)) progress.Report(90 + MergeProgress);
                }
            }

            if (!Equals(progress, null)) progress.Report(100);
            ct.ThrowIfCancellationRequested();
            return SearchResults;
        }

        /// <summary>
        /// Download the content of the IExtension for the MangaObject
        /// </summary>
        /// <param name="Extension">The IExtension to use for Cookies and Referer</param>
        /// <param name="MangaObject">The MangaObject used</param>
        /// <returns></returns>
        private async Task<ExtensionContentResult> LoadExtensionSearchContent(IExtension Extension, String SearchTerm)
        {
            using (WebDownloader WebDownloader = new WebDownloader(Extension.Cookies))
            {
                if (Extension is ISiteExtension)
                { WebDownloader.Referer = (Extension as ISiteExtension).SiteExtensionDescriptionAttribute.RefererHeader; }
                else if (Extension is IDatabaseExtension)
                { WebDownloader.Referer = (Extension as IDatabaseExtension).DatabaseExtensionDescriptionAttribute.RefererHeader; }
                try
                {
                    String Content = null;
                    if (Extension is ISiteExtension)
                    { Content = await Retry(() => ProcessSearchRequest((Extension as ISiteExtension).GetSearchRequestObject(SearchTerm)), DOWNLOAD_TIMEOUT); }
                    else if (Extension is IDatabaseExtension)
                    { Content = await Retry(() => ProcessSearchRequest((Extension as IDatabaseExtension).GetSearchRequestObject(SearchTerm)), DOWNLOAD_TIMEOUT); }
                    return new ExtensionContentResult() { Extension = Extension, Content = Content };
                }
                catch { return null; }
            }
        }

        private async Task<String> ProcessSearchRequest(SearchRequestObject RequestObject)
        {
            using (WebDownloader WebDownloader = new WebDownloader())
            {
                WebDownloader.Referer = RequestObject.Referer;
                switch (RequestObject.Method)
                {
                    case SearchMethod.GET:
                        return await WebDownloader.DownloadStringTaskAsync(RequestObject.Url);
                }
            }
            return null;
        }
        #endregion

        #endregion
    }
}

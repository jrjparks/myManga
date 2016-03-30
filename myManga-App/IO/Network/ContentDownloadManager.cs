using myManga_App.IO.Local.Object;
using myManga_App.Objects.UserConfig;
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

        public IEnumerable<String> ActiveKeys
        { get { return ActiveDownloadsCache.Select(x => x.Key); } }
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
        { return String.Format("{0}", MangaObject.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION)); }
        public String CacheKey(MangaObject MangaObject, ChapterObject ChapterObject)
        { return String.Format("{0}/{1}", MangaObject.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION), ChapterObject.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION)); }
        public String CacheKey(MangaObject MangaObject, ChapterObject ChapterObject, PageObject PageObject)
        { return String.Format("{0}/{1}/{2}", MangaObject.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION), ChapterObject.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION), PageObject.PageNumber); }

        public Boolean IsCacheKeyActive(String CacheKey)
        { return ActiveDownloadsCache.Contains(CacheKey); }
        #endregion

        #region Download MangaObject
        public void Download(MangaObject MangaObject, Boolean Refresh = true, IProgress<Int32> ProgressReporter = null)
        { Task.Run(() => DownloadAsync(MangaObject, Refresh, ProgressReporter)); }

        public async Task DownloadAsync(MangaObject MangaObject, Boolean Refresh = true, IProgress<Int32> ProgressReporter = null)
        {
            String CK = CacheKey(MangaObject);
            if (ActiveDownloadsCache.Contains(CK)) return;
            ActiveDownloadsCache.Set(CK, true, DateTimeOffset.MaxValue);

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
                // TODO: Write Async Verify
            }
            catch (Exception ex)
            {
                App.logger.Warn(String.Format("[ContentDownloadManager] An exception was thrown while processing {0}.", MangaObject.Name), ex);
                throw ex;
            }
            finally
            {
                if (!Equals(ProgressReporter, null)) ProgressReporter.Report(100);
                ActiveDownloadsCache.Remove(CK);
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
            String CK = CacheKey(MangaObject, ChapterObject);
            if (ActiveDownloadsCache.Contains(CK)) return;
            ActiveDownloadsCache.Set(CK, true, DateTimeOffset.MaxValue);

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
                        App.SiteExtensions.First(_SiteExtension => PageObject.Url.Contains(_SiteExtension.ExtensionDescriptionAttribute.URLFormat)).Cookies,
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
            {
                App.logger.Warn(String.Format("[ContentDownloadManager] An exception was thrown while processing {0}.", MangaObject.Name), ex);
                throw ex;
            }
            finally
            {
                if (!Equals(ProgressReporter, null)) ProgressReporter.Report(100);
                ActiveDownloadsCache.Remove(CK);
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
            String CK = CacheKey(MangaObject, ChapterObject, PageObject);
            if (ActiveDownloadsCache.Contains(CK)) return;
            ActiveDownloadsCache.Set(CK, true, DateTimeOffset.MaxValue);

            try
            {
                // Only load page from we if ImgUrl is empty.
                if (String.IsNullOrWhiteSpace(PageObject.ImgUrl))
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
                }

                ISiteExtension SiteExtension = App.SiteExtensions.First(_SiteExtension =>
                { return PageObject.Url.Contains(_SiteExtension.ExtensionDescriptionAttribute.URLFormat); });
                // Start the DownloadImage task, don't wait.
                DownloadImage(PageObject.ImgUrl, PageObject.Url, SiteExtension.Cookies, SavePath(MangaObject, ChapterObject), Path.GetFileName(new Uri(PageObject.ImgUrl).LocalPath));
            }
            catch (Exception ex)
            {
                App.logger.Warn(String.Format("[ContentDownloadManager] An exception was thrown while processing {0}.", MangaObject.Name), ex);
                throw ex;
            }
            finally
            {
                if (!Equals(ProgressReporter, null)) ProgressReporter.Report(100);
                ActiveDownloadsCache.Remove(CK);
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
            if (App.Extensions.Contains(LocationObject.ExtensionName, LocationObject.ExtensionLanguage))
            {
                IExtension Extension = App.Extensions[LocationObject.ExtensionName, LocationObject.ExtensionLanguage];
                Cookies = Extension.Cookies;
                Referer = Extension.ExtensionDescriptionAttribute.RefererHeader;
            }
            else if (App.Extensions.Contains(LocationObject.ExtensionName))
            {
                IExtension Extension = App.Extensions[LocationObject.ExtensionName];
                Cookies = Extension.Cookies;
                Referer = Extension.ExtensionDescriptionAttribute.RefererHeader;
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
            {
                App.logger.Warn(String.Format("[ContentDownloadManager] An exception was thrown while processing {0}.", Url), ex);
                throw ex;
            }
            finally
            {
                if (!Equals(ProgressReporter, null)) ProgressReporter.Report(100);
                ActiveDownloadsCache.Remove(Url);
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
        /// <summary>
        /// Filter Extensions
        /// </summary>
        /// <param name="Extensions">IEnumerable<IExtension> to filter</param>
        /// <param name="EnabledExtensions">IEnumerable<EnabledExtensionObject> to use for filter</param>
        /// <returns></returns>
        public IEnumerable<IExtension> ValidExtensions(IEnumerable<IExtension> Extensions, IEnumerable<EnabledExtensionObject> EnabledExtensions) => Extensions.Where(Extension =>
        {
            if (!Extension.ExtensionDescriptionAttribute.SupportedObjects.HasFlag(SupportedObjects.Manga)) return false;
            if (Extension.ExtensionDescriptionAttribute.RequiresAuthentication) if (!Extension.IsAuthenticated) return false;

            Int32 Count = EnabledExtensions.Where(EnExt => EnExt.Enabled).Count(EnExt => EnExt.EqualsIExtension(Extension));
            if (Equals(Count, 0)) return false;
            return true;
        });

        private Boolean LocationObjectExtension(IExtension Extension, LocationObject Location)
        {
            if (Equals(Extension, null)) return false;
            if (Equals(Location, null)) return false;

            if (!Equals(Extension.ExtensionDescriptionAttribute.Name, Location.ExtensionName)) return false;
            if (!Equals(Location.ExtensionLanguage, null)) // Only check language if location has one.
                if (!Equals(Extension.ExtensionDescriptionAttribute.Language, Location.ExtensionLanguage)) return false;
            return true;
        }

        #region Async Method Classes
        private sealed class ExtensionContentResult
        {
            public IExtension Extension { get; set; }
            public LocationObject Location { get; set; }
            public String Content { get; set; }
        }
        #endregion

        #region Retry
        /// <summary>
        /// Async Task Retry
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="method">Task to Retry</param>
        /// <param name="timeout">Allowed TimeSpan timeout</param>
        /// <returns>Result from method</returns>
        public async Task<TResult> Retry<TResult>(Func<Task<TResult>> method, TimeSpan timeout)
        { return await Retry(method: method, timeout: timeout, delay: TimeSpan.FromSeconds(1), delayIncrement: TimeSpan.Zero); }
        /// <summary>
        /// Async Task Retry
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="method">Task to Retry</param>
        /// <param name="timeout">Allowed TimeSpan timeout</param>
        /// <param name="delay"></param>
        /// <returns>Result from method</returns>
        public async Task<TResult> Retry<TResult>(Func<Task<TResult>> method, TimeSpan timeout, TimeSpan delay)
        { return await Retry(method: method, timeout: timeout, delay: delay, delayIncrement: TimeSpan.Zero); }
        /// <summary>
        /// Async Task Retry
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="method">Task to Retry</param>
        /// <param name="timeout">Allowed TimeSpan timeout</param>
        /// <param name="delay"></param>
        /// <param name="delayIncrement"></param>
        /// <returns>Result from method</returns>
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

                if (!Equals(progress, null)) progress.Report(5);

                IEnumerable<Task<ExtensionContentResult>> ExtensionContentTasksQuery =
                    from Extension in ValidExtensions(App.Extensions, App.UserConfiguration.EnabledExtensions)
                    select LoadExtensionMangaContent(Extension, MangaObject);
                List<Task<ExtensionContentResult>> ExtensionContentTasks = ExtensionContentTasksQuery.ToList();
                Int32 OriginalExtensionContentTasksCount = ExtensionContentTasks.Count;
                Boolean preferDatabaseDescription = true;

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
                        try
                        {
                            if (LoadedExtensionContentResult.Extension is ISiteExtension)
                            {
                                MangaObject DownloadedMangaObject = (LoadedExtensionContentResult.Extension as ISiteExtension).ParseMangaObject(Content);
                                if (!Equals(DownloadedMangaObject, null))
                                { MangaObject.Merge(DownloadedMangaObject); }
                            }
                            else if (LoadedExtensionContentResult.Extension is IDatabaseExtension)
                            {
                                DatabaseObject DownloadedDatabaseObject = (LoadedExtensionContentResult.Extension as IDatabaseExtension).ParseDatabaseObject(Content);
                                if (!Equals(DownloadedDatabaseObject, null))
                                {
                                    MangaObject.AttachDatabase(DownloadedDatabaseObject, preferDatabaseDescription: preferDatabaseDescription);
                                    preferDatabaseDescription = false;  // Only prefer the first database
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            String Name = LoadedExtensionContentResult.Extension.ExtensionDescriptionAttribute.Name,
                                Language = LoadedExtensionContentResult.Extension.ExtensionDescriptionAttribute.Language;
                            App.logger.Warn(String.Format("Unable to parse from {0}-{1} for {2}.", Name, Language, MangaObject.Name), ex);
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
            LocationObject LocationObject = MangaInfoLocationObjects.FirstOrDefault(_ =>
            {
                if (!Equals(Extension.ExtensionDescriptionAttribute.Name, _.ExtensionName)) return false;
                if (!Equals(_.ExtensionLanguage, null)) // Only check language if location has one.
                    if (!Equals(Extension.ExtensionDescriptionAttribute.Language, _.ExtensionLanguage)) return false;
                return _.Enabled;
            });
            if (Equals(LocationObject, null)) // If there is not a match return null
            { return null; }
            using (WebDownloader WebDownloader = new WebDownloader(Extension.Cookies))
            {
                WebDownloader.Encoding = System.Text.Encoding.UTF8;
                WebDownloader.Referer = Extension.ExtensionDescriptionAttribute.RefererHeader;
                try
                {
                    String Content = await Retry(() =>
                        WebDownloader.DownloadStringTaskAsync(LocationObject.Url),
                        DOWNLOAD_TIMEOUT);
                    return new ExtensionContentResult()
                    {
                        Extension = Extension,
                        Location = LocationObject,
                        Content = Content
                    };
                }
                catch (Exception ex)
                {
                    String Name = Extension.ExtensionDescriptionAttribute.Name,
                        Language = Extension.ExtensionDescriptionAttribute.Language;
                    App.logger.Warn(String.Format("Unable to load content from {0}-{1} for {2}.", Name, Language, MangaObject.Name), ex);
                    return null;
                }
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
                IEnumerable<ISiteExtension> ValidSiteExtensions = ValidExtensions(App.SiteExtensions, App.UserConfiguration.EnabledExtensions).Cast<ISiteExtension>();

                // Re-Order the Chapter's LocationObjects to the EnabledExtensions order.
                IEnumerable<LocationObject> OrderedChapterObjectLocations = from EnExt in App.UserConfiguration.EnabledExtensions
                                                                            where ChapterObject.Locations.Exists(LocObj => EnExt.EqualsLocationObject(LocObj))
                                                                            select ChapterObject.Locations.FirstOrDefault(LocObj => EnExt.EqualsLocationObject(LocObj));

                foreach (LocationObject LocationObject in OrderedChapterObjectLocations)
                {
                    ct.ThrowIfCancellationRequested();
                    ISiteExtension SiteExtension = ValidSiteExtensions.FirstOrDefault(_ => LocationObjectExtension(_, LocationObject));
                    if (Equals(SiteExtension, null)) continue;  // Continue with the foreach loop

                    ct.ThrowIfCancellationRequested();
                    using (WebDownloader WebDownloader = new WebDownloader(SiteExtension.Cookies))
                    {
                        WebDownloader.Encoding = System.Text.Encoding.UTF8;
                        WebDownloader.Referer = SiteExtension.ExtensionDescriptionAttribute.RefererHeader;
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

                ISiteExtension SiteExtension = App.SiteExtensions.First(_SiteExtension => PageObject.Url.Contains(_SiteExtension.ExtensionDescriptionAttribute.URLFormat));
                using (WebDownloader WebDownloader = new WebDownloader(SiteExtension.Cookies))
                {
                    WebDownloader.Referer = SiteExtension.ExtensionDescriptionAttribute.RefererHeader;
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

            if (!Equals(progress, null)) progress.Report(5);

            Boolean firstResponse = true;
            IEnumerable<Task<ExtensionContentResult>> ExtensionContentTasksQuery =
                from Extension in ValidExtensions(App.Extensions, App.UserConfiguration.EnabledExtensions)
                select LoadExtensionSearchContent(Extension, SearchTerm);
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
                    {   // Extension was a ISiteExtension
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
                    {   // Extension was a IDatabaseExtension
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
                            if (Equals(ExistingMangaObject, null))
                            {
                                MangaObject databaseMangaObject = new MangaObject();
                                databaseMangaObject.AttachDatabase(DatabaseObject, true, true);
                                SearchResults.Add(databaseMangaObject);
                            }
                            else
                            {
                                if (Equals(ExistingMangaObject.DatabaseLocations.FindIndex(_DatabaseLocation => Equals(
                                    _DatabaseLocation.ExtensionName,
                                    DatabaseExtension.ExtensionDescriptionAttribute.Name)), -1))
                                { ExistingMangaObject.AttachDatabase(DatabaseObject, preferDatabaseDescription: true); }
                            }
                        }
                    }
                }
                firstResponse = Equals(SearchResults.Count, 0);
                Int32 ExtensionContentTasksProgress = (Int32)Math.Round(((Double)(OriginalExtensionContentTasksCount - ExtensionContentTasks.Count) / (Double)OriginalExtensionContentTasksCount) * 70);
                if (!Equals(progress, null)) progress.Report(10 + ExtensionContentTasksProgress);
            }

            if (!Equals(progress, null)) progress.Report(85);
            ct.ThrowIfCancellationRequested();

            // Remove Database only results
            Int32 RemoveCount = SearchResults.RemoveAll(_ => Equals(_.Locations.Count, 0));

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
                WebDownloader.Encoding = System.Text.Encoding.UTF8;
                WebDownloader.Referer = Extension.ExtensionDescriptionAttribute.RefererHeader;
                try
                {
                    SearchRequestObject sro = Extension.GetSearchRequestObject(SearchTerm);
                    String Content = await Retry(() => ProcessSearchRequest(sro), DOWNLOAD_TIMEOUT);
                    return new ExtensionContentResult()
                    {
                        Extension = Extension,
                        Location = new LocationObject()
                        {
                            Enabled = true,
                            Url = sro.Url,
                            ExtensionName = Extension.ExtensionDescriptionAttribute.Name,
                            ExtensionLanguage = Extension.ExtensionDescriptionAttribute.Language
                        },
                        Content = Content
                    };
                }
                catch (Exception ex)
                {
                    String Name = Extension.ExtensionDescriptionAttribute.Name,
                        Language = Extension.ExtensionDescriptionAttribute.Language;
                    App.logger.Warn(String.Format("Unable to load search content from {0}-{1} for {2}.", Name, Language, SearchTerm), ex);
                    return null;
                }
            }
        }

        private async Task<String> ProcessSearchRequest(SearchRequestObject RequestObject)
        {
            using (WebDownloader WebDownloader = new WebDownloader())
            {
                WebDownloader.Encoding = System.Text.Encoding.UTF8;
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

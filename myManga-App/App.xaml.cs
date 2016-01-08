using System;
using System.IO;
using System.Windows;
using System.Linq;
using Core.DLL;
using Core.IO;
using myMangaSiteExtension.Utilities;
using myMangaSiteExtension.Collections;
using myMangaSiteExtension.Interfaces;
using myManga_App.Objects;
using Core.IO.Storage.Manager.BaseInterfaceClasses;
using myManga_App.Objects.About;
using System.Collections.ObjectModel;
using myManga_App.Objects.Cache;
using myManga_App.IO.Network;
using myManga_App.Objects.UserConfig;
using System.Threading;
using System.Runtime.Caching;
using System.Windows.Threading;
using System.Threading.Tasks;
using myMangaSiteExtension.Objects;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using Core.MVVM;
using myManga_App.IO.Local;

namespace myManga_App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IDisposable
    {
        #region Logging
        private readonly log4net.ILog logger = log4net.LogManager.GetLogger(typeof(App));

        private void ConfigureLog4Net()
        {
            log4net.Appender.FileAppender appender = new log4net.Appender.FileAppender();
            appender.Layout = new log4net.Layout.SimpleLayout();
            appender.File = LOG_FILE_PATH;
            appender.AppendToFile = true;
            appender.ImmediateFlush = true;
            appender.Threshold = log4net.Core.Level.All;
            appender.ActivateOptions();
            log4net.Config.BasicConfigurator.Configure(appender);
        }
        #endregion

        #region IO
        public FileStorage FileStorage
        { get; private set; }

        public ZipStorage ZipStorage
        { get; private set; }

        public ZipManager ZipManager
        { get; private set; }

        public ContentDownloadManager ContentDownloadManager
        { get; private set; }

        //public DownloadManager DownloadManager { get; private set; }

        private FileSystemWatcher MangaObjectArchiveWatcher
        { get; set; }

        private FileSystemWatcher ChapterObjectArchiveWatcher
        { get; set; }
        #endregion

        #region DLL Storage
        private readonly EmbeddedDLL emdll;

        public DLL_Manager<ISiteExtension, ISiteExtensionCollection> SiteExtensions
        { get; private set; }

        public DLL_Manager<IDatabaseExtension, IDatabaseExtensionCollection> DatabaseExtensions
        { get; private set; }
        #endregion

        #region Cache
        public RegionedMemoryCache AppMemoryCache
        { get; private set; }
        #endregion

        #region DataObjects
        public MangaArchiveCacheObject SelectedMangaArchiveCacheObject
        { get; set; }

        public ObservableCollection<MangaArchiveCacheObject> MangaArchiveCacheCollection
        { get; private set; }

        #region MangaObject Cache
        public ObservableCollection<MangaCacheObject> MangaCacheObjects
        { get; private set; }

        private async Task<MangaCacheObject> UnsafeLoadMangaCacheObjectAsync(String ArchivePath)
        {
            try
            {
                MangaCacheObject MangaCacheObject = new MangaCacheObject();
                MangaCacheObject.ArchiveFileName = Path.GetFileName(ArchivePath);

                // Load BookmarkObject Data
                Stream BookmarkObjectStream = ZipManager.UnsafeRead(ArchivePath, nameof(BookmarkObject));
                if (!Equals(BookmarkObjectStream, null))
                { using (BookmarkObjectStream) { MangaCacheObject.BookmarkObject = BookmarkObjectStream.Deserialize<BookmarkObject>(UserConfig.SaveType); } }

                // Load MangaObject Data
                Stream MangaObjectStream = ZipManager.UnsafeRead(ArchivePath, nameof(MangaObject));
                if (!Equals(MangaObjectStream, null))
                { using (MangaObjectStream) { MangaCacheObject.MangaObject = MangaObjectStream.Deserialize<MangaObject>(UserConfig.SaveType); } }
                if (!Equals(MangaCacheObject.MangaObject, null))
                    MangaCacheObject.MangaObject = await MigrateCovers(ArchivePath, MangaCacheObject.MangaObject);

                // Load Cover Image
                String CoverImageFileName = Path.GetFileName(MangaCacheObject.MangaObject.SelectedCover().Url);
                Stream CoverImageStream = ZipManager.UnsafeRead(ArchivePath, CoverImageFileName);
                if (!Equals(CoverImageStream, null))
                {
                    using (CoverImageStream)
                    {
                        if (Equals(MangaCacheObject.CoverImage, null))
                            MangaCacheObject.CoverImage = new BitmapImage();

                        if (!Equals(MangaCacheObject.CoverImage.StreamSource, null))
                        {
                            MangaCacheObject.CoverImage.StreamSource.Close();
                            MangaCacheObject.CoverImage.StreamSource.Dispose();
                            MangaCacheObject.CoverImage.StreamSource = null;
                        }

                        MangaCacheObject.CoverImage.BeginInit();
                        MangaCacheObject.CoverImage.DecodePixelWidth = 300;
                        MangaCacheObject.CoverImage.CacheOption = BitmapCacheOption.OnLoad;
                        MangaCacheObject.CoverImage.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                        MangaCacheObject.CoverImage.StreamSource = CoverImageStream;
                        MangaCacheObject.CoverImage.EndInit();
                        MangaCacheObject.CoverImage.Freeze();
                        CoverImageStream.Close();
                    }
                }

                return MangaCacheObject;
            }
            catch (Exception ex)
            {
                logger.Warn("Unable to read Manga Archive.", ex);
                MessageBox.Show(String.Format("Unable to read Manga Archive.\nFile: {0}\nException:\n{1}\n\n{2}", ArchivePath, ex.Message, ex.StackTrace));
                return null;
            }
        }

        private async Task<MangaCacheObject> ReloadMangaCacheObjectAsync(String ArchivePath, Boolean ReloadCoverImage = false)
        {
            try
            {
                MangaCacheObject MangaCacheObject = new MangaCacheObject();
                MangaCacheObject.ArchiveFileName = Path.GetFileName(ArchivePath);

                // Load BookmarkObject Data
                Stream BookmarkObjectStream = await ZipManager.Retry(() => ZipManager.ReadAsync(ArchivePath, nameof(BookmarkObject)), TimeSpan.FromMinutes(1));
                if (!Equals(BookmarkObjectStream, null))
                { using (BookmarkObjectStream) { MangaCacheObject.BookmarkObject = BookmarkObjectStream.Deserialize<BookmarkObject>(UserConfig.SaveType); } }

                // Load MangaObject Data
                Stream MangaObjectStream = await ZipManager.Retry(() => ZipManager.ReadAsync(ArchivePath, nameof(MangaObject)), TimeSpan.FromMinutes(1));
                if (!Equals(MangaObjectStream, null))
                { using (MangaObjectStream) { MangaCacheObject.MangaObject = MangaObjectStream.Deserialize<MangaObject>(UserConfig.SaveType); } }
                if (!Equals(MangaCacheObject.MangaObject, null))
                    MangaCacheObject.MangaObject = await MigrateCovers(ArchivePath, MangaCacheObject.MangaObject);

                if (ReloadCoverImage)
                {
                    // Load Cover Image
                    String CoverImageFileName = Path.GetFileName(MangaCacheObject.MangaObject.SelectedCover().Url);
                    Stream CoverImageStream = await ZipManager.Retry(() => ZipManager.ReadAsync(ArchivePath, CoverImageFileName), TimeSpan.FromMinutes(1));
                    if (!Equals(CoverImageStream, null))
                    {
                        using (CoverImageStream)
                        {
                            if (Equals(MangaCacheObject.CoverImage, null))
                                MangaCacheObject.CoverImage = new BitmapImage();

                            if (!Equals(MangaCacheObject.CoverImage.StreamSource, null))
                            {
                                MangaCacheObject.CoverImage.StreamSource.Close();
                                MangaCacheObject.CoverImage.StreamSource.Dispose();
                                MangaCacheObject.CoverImage.StreamSource = null;
                            }

                            MangaCacheObject.CoverImage.BeginInit();
                            MangaCacheObject.CoverImage.DecodePixelWidth = 300;
                            MangaCacheObject.CoverImage.CacheOption = BitmapCacheOption.OnLoad;
                            MangaCacheObject.CoverImage.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                            MangaCacheObject.CoverImage.StreamSource = CoverImageStream;
                            MangaCacheObject.CoverImage.EndInit();
                            MangaCacheObject.CoverImage.Freeze();
                            CoverImageStream.Close();
                        }
                    }
                }

                return MangaCacheObject;
            }
            catch (Exception ex)
            {
                logger.Warn("Unable to read Manga Archive.", ex);
                MessageBox.Show(String.Format("Unable to read Manga Archive.\nFile: {0}\nException:\n{1}\n\n{2}", ArchivePath, ex.Message, ex.StackTrace));
                return null;
            }
        }

        private async Task<MangaObject> MigrateCovers(String ArchivePath, MangaObject MangaObject)
        {
            if (Equals(MangaObject.CoverLocations.Count, 0))
            {
                foreach (String Cover in MangaObject.Covers)
                {
                    ISiteExtension SiteExtension = SiteExtensions.DLLCollection.FirstOrDefault(_SiteExtension =>
                    { return Cover.Contains(_SiteExtension.SiteExtensionDescriptionAttribute.URLFormat); });
                    IDatabaseExtension DatabaseExtension = DatabaseExtensions.DLLCollection.FirstOrDefault(_DatabaseExtension =>
                    { return Cover.Contains(_DatabaseExtension.DatabaseExtensionDescriptionAttribute.URLFormat); });
                    if (Cover.Contains("mhcdn.net")) SiteExtension = SiteExtensions.DLLCollection["MangaHere"];
                    if (!Equals(SiteExtension, null))
                        MangaObject.CoverLocations.Add(new LocationObject()
                        { Url = Cover, ExtensionName = SiteExtension.SiteExtensionDescriptionAttribute.Name });
                    else if (!Equals(DatabaseExtension, null))
                        MangaObject.CoverLocations.Add(new LocationObject()
                        { Url = Cover, ExtensionName = DatabaseExtension.DatabaseExtensionDescriptionAttribute.Name });
                }

                await ZipManager.Retry(() => ZipManager.WriteAsync(ArchivePath, nameof(MangaObject), MangaObject.Serialize(UserConfig.SaveType)), TimeSpan.FromMinutes(1));
            }
            return MangaObject;
        }

        /// <summary>
        /// Warning, this will completely reload the cache.
        /// </summary>
        private async Task FullMangaCacheObject()
        {
            String[] MangaArchivePaths = Directory.GetFiles(MANGA_ARCHIVE_DIRECTORY, MANGA_ARCHIVE_FILTER, SearchOption.TopDirectoryOnly);
            Stopwatch loadWatch = Stopwatch.StartNew();

            IEnumerable<Task<MangaCacheObject>> MangaCacheObjectTasksQuery = from MangaArchivePath in MangaArchivePaths select UnsafeLoadMangaCacheObjectAsync(MangaArchivePath);
            List<Task<MangaCacheObject>> MangaCacheObjectTasks = MangaCacheObjectTasksQuery.ToList();

            MangaCacheObjects.Clear();
            while (MangaCacheObjectTasks.Count > 0)
            {
                Task<MangaCacheObject> completedTask = await Task.WhenAny(MangaCacheObjectTasks);
                MangaCacheObjectTasks.Remove(completedTask);

                MangaCacheObject LoadedMangaCacheObject = await completedTask;
                if (!Equals(LoadedMangaCacheObject, null))
                {
                    MangaCacheObjects.Add(LoadedMangaCacheObject);
                }
            }

            TimeSpan loadTime = loadWatch.Elapsed;
            loadWatch.Stop();
        }
        #endregion

        #endregion

        #region Configuration
        public readonly String
            PLUGIN_DIRECTORY = Path.Combine(Environment.CurrentDirectory, "Plugins").SafeFolder(),
            MANGA_ARCHIVE_DIRECTORY = Path.Combine(Environment.CurrentDirectory, "Manga Archives").SafeFolder(),
            CHAPTER_ARCHIVE_DIRECTORY = Path.Combine(Environment.CurrentDirectory, "Chapter Archives").SafeFolder(),
            MANGA_ARCHIVE_EXTENSION = "ma.zip",
            CHAPTER_ARCHIVE_EXTENSION = "ca.zip",
            MANGA_ARCHIVE_FILTER = "*.ma.zip",
            CHAPTER_ARCHIVE_FILTER = "*.ca.zip",
            USER_CONFIG_FILENAME = "mymanga.conf",
            USER_AUTH_FILENAME = "mymanga.auth.conf",
            USER_CONFIG_PATH = Path.Combine(Environment.CurrentDirectory, "mymanga.conf".SafeFileName()),
            USER_AUTH_PATH = Path.Combine(Environment.CurrentDirectory, "mymanga.auth.conf".SafeFileName()),
            LOG_FILE_PATH = Path.Combine(Environment.CurrentDirectory, String.Format("mymanga-{0}-{1}.log", DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString()).SafeFileName());

        public UserConfigurationObject UserConfig
        { get; private set; }

        public UserAuthenticationObject UserAuthentication
        { get; private set; }
        #endregion

        #region Theme Resource Dictionary
        public ResourceDictionary ThemeResourceDictionary
        {
            get { return Resources.MergedDictionaries[0]; }
            set { Resources.MergedDictionaries[0] = value; }
        }
        public void ApplyTheme(ThemeType theme)
        {
            switch (theme)
            {
                default:
                case ThemeType.Light:
                    ThemeResourceDictionary.Source = new Uri("/myManga;component/Themes/LightTheme.xaml", UriKind.RelativeOrAbsolute);
                    break;

                case ThemeType.Dark:
                    ThemeResourceDictionary.Source = new Uri("/myManga;component/Themes/DarkTheme.xaml", UriKind.RelativeOrAbsolute);
                    break;
            }
        }
        #endregion

        private readonly AssemblyInformation assemblyInfo;
        public AssemblyInformation AssemblyInfo
        { get { return assemblyInfo; } }

        public App()
        {
            AppMemoryCache = new RegionedMemoryCache("AppMemoryCache");

            // Configure log4net
            ConfigureLog4Net();

            // Load Embedded DLLs from Resources.
            emdll = new EmbeddedDLL();
            SiteExtensions = new DLL_Manager<ISiteExtension, ISiteExtensionCollection>();
            DatabaseExtensions = new DLL_Manager<IDatabaseExtension, IDatabaseExtensionCollection>();
            assemblyInfo = new AssemblyInformation();

            AppDomain.CurrentDomain.AssemblyResolve += emdll.ResolveAssembly;
            SiteExtensions.DLLAppDomain.AssemblyResolve += emdll.ResolveAssembly;
            DatabaseExtensions.DLLAppDomain.AssemblyResolve += emdll.ResolveAssembly;

            // Handle unhandled exceptions
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Initialize Collections
            MangaArchiveCacheCollection = new ObservableCollection<MangaArchiveCacheObject>();
            MangaCacheObjects = new ObservableCollection<MangaCacheObject>();

            // Create a File System Watcher for Manga Objects
            MangaObjectArchiveWatcher = new FileSystemWatcher(MANGA_ARCHIVE_DIRECTORY, MANGA_ARCHIVE_FILTER);
            MangaObjectArchiveWatcher.EnableRaisingEvents = false;

            // Create a File System Watcher for Manga Chapter Objects
            ChapterObjectArchiveWatcher = new FileSystemWatcher(CHAPTER_ARCHIVE_DIRECTORY, CHAPTER_ARCHIVE_FILTER);
            ChapterObjectArchiveWatcher.IncludeSubdirectories = true;
            ChapterObjectArchiveWatcher.EnableRaisingEvents = false;

            // Create IO class objects
            FileStorage = new FileStorage();
            ZipStorage = new ZipStorage(); // v1 - Thread based
            ZipManager = new ZipManager(); // v2 - Async/Await based

            // DownloadManager = new DownloadManager(); // v1 - Thread based
            ContentDownloadManager = new ContentDownloadManager(); // v2 - Async/Await based

            Startup += App_Startup;

            InitializeComponent();
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.Error(sender.GetType().FullName, e.ExceptionObject as Exception);
        }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            logger.Error(sender.GetType().FullName, e.Exception);
            e.Handled = true;
        }

        async void App_Startup(object sender, StartupEventArgs e)
        {
            SiteExtensions.LoadDLL(PLUGIN_DIRECTORY, Filter: "*.mymanga.dll");
            DatabaseExtensions.LoadDLL(PLUGIN_DIRECTORY, Filter: "*.mymanga.dll");

            LoadUserConfig();
            AuthenticateUser();
            UserConfig.UserConfigurationUpdated += (_s, _e) => SaveUserConfig();

            // Enable FileSystemWatchers
            ConfigureFileWatchers();

            // Run initial load of cache
            await FullMangaCacheObject();
            MangaObjectArchiveWatcher.EnableRaisingEvents = true;
            ChapterObjectArchiveWatcher.EnableRaisingEvents = true;
        }

        #region File Watcher Events
        private void ConfigureFileWatchers()
        {
            MangaObjectArchiveWatcher.Changed += MangaObjectArchiveWatcher_Event;
            MangaObjectArchiveWatcher.Created += MangaObjectArchiveWatcher_Event;
            MangaObjectArchiveWatcher.Deleted += MangaObjectArchiveWatcher_Event;
            MangaObjectArchiveWatcher.Renamed += MangaObjectArchiveWatcher_Event;

            ChapterObjectArchiveWatcher.Changed += ChapterObjectArchiveWatcher_Event;
            ChapterObjectArchiveWatcher.Created += ChapterObjectArchiveWatcher_Event;
            ChapterObjectArchiveWatcher.Deleted += ChapterObjectArchiveWatcher_Event;
            ChapterObjectArchiveWatcher.Renamed += ChapterObjectArchiveWatcher_Event;
        }

        private async void MangaObjectArchiveWatcher_Event(object sender, FileSystemEventArgs e)
        {
            if (Dispatcher.Thread == Thread.CurrentThread)
            {
                MangaCacheObject ExistingMangaCacheObject = MangaCacheObjects.FirstOrDefault(_ => Equals(_.ArchiveFileName, e.Name));
                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Created:
                        if (Equals(ExistingMangaCacheObject, null))
                        {
                            MangaCacheObjects.Add(ExistingMangaCacheObject = new MangaCacheObject() { ArchiveFileName = e.Name });
                        }
                        goto case WatcherChangeTypes.Changed;
                    case WatcherChangeTypes.Changed:
                        // (Re)Cache if creaded or changed
                        MangaCacheObject ReloadedMangaCacheObject = await ReloadMangaCacheObjectAsync(e.FullPath, Equals(ExistingMangaCacheObject.CoverImage, null));
                        if (!Equals(ReloadedMangaCacheObject, null))
                        {
                            if (Equals(ExistingMangaCacheObject, null))
                            { MangaCacheObjects.Add(ReloadedMangaCacheObject); }
                            else
                            { ExistingMangaCacheObject.Update(ReloadedMangaCacheObject); }
                        }
                        break;

                    case WatcherChangeTypes.Deleted:
                        // Reselect nearest neighbor after delete
                        Int32 ExistingIndex = MangaCacheObjects.IndexOf(ExistingMangaCacheObject);
                        if (ExistingIndex >= 0) MangaCacheObjects.RemoveAt(ExistingIndex);

                        // If delete was the last item subtract from index
                        if (ExistingIndex >= MangaCacheObjects.Count) --ExistingIndex;

                        Messenger.Default.Send((ExistingIndex >= 0) ? MangaCacheObjects[ExistingIndex] : null, "SelectMangaCacheObject");
                        break;

                    default:
                        break;
                }
                Messenger.Default.Send(e, "MangaObjectArchiveWatcher");
            }
            else Dispatcher.Invoke(DispatcherPriority.Send, new Action(() => MangaObjectArchiveWatcher_Event(sender, e)));
        }

        private void ChapterObjectArchiveWatcher_Event(object sender, FileSystemEventArgs e)
        {
            RunOnUiThread(() =>
            {
                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Created:
                    case WatcherChangeTypes.Deleted:
                        MangaCacheObject ExistingMangaCacheObject = MangaCacheObjects.FirstOrDefault(
                            _ => Equals(
                                _.MangaObject.MangaFileName(),
                                Path.GetDirectoryName(e.Name)));
                        if (!Equals(ExistingMangaCacheObject, null))
                        {
                            Int32 ExistingMangaCacheObjectIndex = ExistingMangaCacheObject.ChapterCacheObjects.FindIndex(_ => Equals(_.ArchiveFileName, Path.GetFileName(e.Name)));
                            if (ExistingMangaCacheObjectIndex >= 0)
                                ExistingMangaCacheObject.ChapterCacheObjects[ExistingMangaCacheObjectIndex].IsLocal = File.Exists(e.FullPath);
                        }
                        break;
                }

                Messenger.Default.Send(e, "ChapterObjectArchiveWatcher");
            });
        }
        #endregion

        #region User Config Files
        private void AuthenticateUser()
        {
            Stream UserAuthenticationStream;
            if (this.FileStorage.TryRead(this.USER_AUTH_PATH, out UserAuthenticationStream))
            {
                using (UserAuthenticationStream)
                {
                    try { this.UserAuthentication = UserAuthenticationStream.Deserialize<UserAuthenticationObject>(SaveType: SaveType.XML); }
                    catch { }
                }
            }

            if (UserAuthenticationObject.Equals(this.UserAuthentication, null))
            {
                this.UserAuthentication = new UserAuthenticationObject();
            }

            CancellationTokenSource cts = new CancellationTokenSource();
            foreach (UserPluginAuthenticationObject upa in this.UserAuthentication.UserPluginAuthentications)
            {
                try
                {
                    ISiteExtension siteExtension = this.SiteExtensions.DLLCollection[upa.PluginName];
                    siteExtension.Authenticate(new System.Net.NetworkCredential(upa.Username, upa.Password), cts.Token, null);
                }
                catch
                {
                    MessageBox.Show(String.Format("There was an error decoding {0}. Please reauthenticate.", upa.PluginName));
                }
            }
            SaveUserAuthentication();
        }

        private void LoadUserConfig()
        {
            Stream UserConfigStream;
            if (this.FileStorage.TryRead(this.USER_CONFIG_PATH, out UserConfigStream))
            {
                using (UserConfigStream)
                {
                    try
                    {
                        this.UserConfig = UserConfigStream.Deserialize<UserConfigurationObject>(SaveType: SaveType.XML);
                    }
                    catch { }
                }
            }
            if (UserConfigurationObject.Equals(this.UserConfig, null))
            {
                this.UserConfig = new UserConfigurationObject();

                // Enable all available Database Extentions
                foreach (IDatabaseExtension DatabaseExtension in DatabaseExtensions.DLLCollection)
                    this.UserConfig.EnabledDatabaseExtentions.Add(DatabaseExtension.DatabaseExtensionDescriptionAttribute.Name);

                // Enable the first Site Extention if available
                if (SiteExtensions.DLLCollection.Count > 0)
                    this.UserConfig.EnabledSiteExtensions.Add(SiteExtensions.DLLCollection[0].SiteExtensionDescriptionAttribute.Name);
                SaveUserConfig();
            }
            ApplyTheme(this.UserConfig.Theme);
        }

        public void SaveUserConfig()
        {
            if (!UserConfigurationObject.Equals(this.UserConfig, null))
                this.FileStorage.Write(this.USER_CONFIG_PATH, this.UserConfig.Serialize(SaveType: SaveType.XML));
        }

        public void SaveUserAuthentication()
        {
            if (!UserConfigurationObject.Equals(this.UserConfig, null))
                this.FileStorage.Write(this.USER_AUTH_PATH, this.UserAuthentication.Serialize(SaveType: SaveType.XML));
        }
        #endregion

        public void RunOnUiThread(Action action)
        {
            if (Dispatcher.Thread == Thread.CurrentThread) action();
            else Dispatcher.Invoke(DispatcherPriority.Send, action);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    AppMemoryCache.Dispose();
                    MangaObjectArchiveWatcher.Dispose();
                    ChapterObjectArchiveWatcher.Dispose();
                    //DownloadManager.Dispose();
                    ContentDownloadManager.Dispose();
                    ZipManager.Dispose();
                    ZipStorage.Dispose();
                    FileStorage.Dispose();
                    SiteExtensions.Unload();
                    DatabaseExtensions.Unload();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        { Dispose(true); }

        ~App()
        { Dispose(true); }
        #endregion
    }
}

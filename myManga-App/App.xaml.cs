using myManga_App.IO.DLL;
using myManga_App.IO.Local;
using myManga_App.IO.Local.Object;
using myManga_App.IO.Network;
using myManga_App.Objects;
using myManga_App.Objects.About;
using myManga_App.Objects.Cache;
using myManga_App.Objects.UserConfig;
using myMangaSiteExtension.Collections;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Communication;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace myManga_App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Logging
        private readonly log4net.ILog logger = log4net.LogManager.GetLogger(typeof(App));

        private void ConfigureLog4Net(log4net.Core.Level LogLevel = null)
        {
            if (Equals(LogLevel, null))
                LogLevel = log4net.Core.Level.All;
            log4net.Appender.RollingFileAppender appender = new log4net.Appender.RollingFileAppender();
            appender.Layout = new log4net.Layout.SimpleLayout();
            appender.File = LOG_FILE_PATH;
            appender.AppendToFile = true;
            appender.ImmediateFlush = true;
            appender.Threshold = LogLevel;
            appender.ActivateOptions();
            log4net.Config.BasicConfigurator.Configure(appender);
        }
        #endregion

        #region IO
        public ZipManager ZipManager
        { get; private set; }

        public ContentDownloadManager ContentDownloadManager
        { get; private set; }

        private FileSystemWatcher MangaObjectArchiveWatcher
        { get; set; }

        private FileSystemWatcher ChapterObjectArchiveWatcher
        { get; set; }
        #endregion

        #region DLL Management
        private readonly Embedded embedded = new Embedded();

        #region Storage
        public Manager<ISiteExtension, ISiteExtensionCollection> SiteExtensions
        { get; private set; }

        public Manager<IDatabaseExtension, IDatabaseExtensionCollection> DatabaseExtensions
        { get; private set; }
        #endregion

        private void InitializeEmbedded()
        {
            SiteExtensions = new Manager<ISiteExtension, ISiteExtensionCollection>();
            DatabaseExtensions = new Manager<IDatabaseExtension, IDatabaseExtensionCollection>();

            AppDomain.CurrentDomain.AssemblyResolve += embedded.ResolveAssembly;
            SiteExtensions.ManagerAppDomain.AssemblyResolve += embedded.ResolveAssembly;
            DatabaseExtensions.ManagerAppDomain.AssemblyResolve += embedded.ResolveAssembly;
        }

        #endregion

        #region Cache
        public RegionedMemoryCache AppMemoryCache
        { get; private set; }
        #endregion

        #region DataObjects

        #region MangaObject Cache
        public ObservableCollection<MangaCacheObject> MangaCacheObjects
        { get; private set; }

        private async Task<MangaCacheObject> UnsafeDispatcherLoadMangaCacheObjectAsync(String ArchivePath)
        {
            return await Current.Dispatcher.Invoke(() => UnsafeLoadMangaCacheObjectAsync(ArchivePath));
        }

        private async Task<MangaCacheObject> UnsafeLoadMangaCacheObjectAsync(String ArchivePath)
        {
            try
            {
                MangaCacheObject MangaCacheObject = new MangaCacheObject();
                MangaCacheObject.ArchiveFileName = Path.GetFileName(ArchivePath);

                // Load BookmarkObject Data
                Stream BookmarkObjectStream = ZipManager.UnsafeRead(ArchivePath, typeof(BookmarkObject).Name);
                if (!Equals(BookmarkObjectStream, null))
                { using (BookmarkObjectStream) { MangaCacheObject.BookmarkObject = BookmarkObjectStream.Deserialize<BookmarkObject>(UserConfiguration.SerializeType); } }

                // Load MangaObject Data
                Stream MangaObjectStream = ZipManager.UnsafeRead(ArchivePath, typeof(MangaObject).Name);
                if (!Equals(MangaObjectStream, null))
                { using (MangaObjectStream) { MangaCacheObject.MangaObject = MangaObjectStream.Deserialize<MangaObject>(UserConfiguration.SerializeType); } }
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
                        using (MangaCacheObject.CoverImage.StreamSource = CoverImageStream)
                        {
                            MangaCacheObject.CoverImage.EndInit();
                            MangaCacheObject.CoverImage.Freeze();
                        }
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

        private async Task<MangaCacheObject> DispatcherReloadMangaCacheObjectAsync(String ArchivePath, Boolean ReloadCoverImage = false)
        {
            return await Current.Dispatcher.Invoke(() => ReloadMangaCacheObjectAsync(ArchivePath, ReloadCoverImage));
        }

        private async Task<MangaCacheObject> ReloadMangaCacheObjectAsync(String ArchivePath, Boolean ReloadCoverImage = false)
        {
            try
            {
                MangaCacheObject MangaCacheObject = new MangaCacheObject();
                MangaCacheObject.ArchiveFileName = Path.GetFileName(ArchivePath);

                // Load BookmarkObject Data
                Stream BookmarkObjectStream = await ZipManager.Retry(() => ZipManager.ReadAsync(ArchivePath, typeof(BookmarkObject).Name), TimeSpan.FromMinutes(1));
                if (!Equals(BookmarkObjectStream, null))
                { using (BookmarkObjectStream) { MangaCacheObject.BookmarkObject = BookmarkObjectStream.Deserialize<BookmarkObject>(UserConfiguration.SerializeType); } }

                // Load MangaObject Data
                Stream MangaObjectStream = await ZipManager.Retry(() => ZipManager.ReadAsync(ArchivePath, typeof(MangaObject).Name), TimeSpan.FromMinutes(1));
                if (!Equals(MangaObjectStream, null))
                { using (MangaObjectStream) { MangaCacheObject.MangaObject = MangaObjectStream.Deserialize<MangaObject>(UserConfiguration.SerializeType); } }
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
                            using (MangaCacheObject.CoverImage.StreamSource = CoverImageStream)
                            {
                                MangaCacheObject.CoverImage.EndInit();
                                MangaCacheObject.CoverImage.Freeze();
                            }
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

                await ZipManager.Retry(() => ZipManager.WriteAsync(ArchivePath, typeof(MangaObject).Name, MangaObject.Serialize(UserConfiguration.SerializeType)), TimeSpan.FromMinutes(1));
            }
            return MangaObject;
        }

        /// <summary>
        /// Warning, this will completely reload the cache.
        /// </summary>
        private async Task<TimeSpan> FullMangaCacheObject()
        {
            Stopwatch loadWatch = Stopwatch.StartNew();
            String[] MangaArchivePaths = Directory.GetFiles(MANGA_ARCHIVE_DIRECTORY, MANGA_ARCHIVE_FILTER, SearchOption.TopDirectoryOnly);

            IEnumerable<Task<MangaCacheObject>> MangaCacheObjectTasksQuery =
                from MangaArchivePath in MangaArchivePaths
                select UnsafeDispatcherLoadMangaCacheObjectAsync(MangaArchivePath);
            List<Task<MangaCacheObject>> MangaCacheObjectTasks = MangaCacheObjectTasksQuery.ToList();

            await Current.Dispatcher.InvokeAsync(() => MangaCacheObjects.Clear());
            while (MangaCacheObjectTasks.Count > 0)
            {
                Task<MangaCacheObject> completedTask = await Task.WhenAny(MangaCacheObjectTasks);
                MangaCacheObjectTasks.Remove(completedTask);

                MangaCacheObject LoadedMangaCacheObject = await completedTask;
                if (!Equals(LoadedMangaCacheObject, null))
                {
                    await Current.Dispatcher.InvokeAsync(() => MangaCacheObjects.Add(LoadedMangaCacheObject));
                }
            }

            TimeSpan loadTime = loadWatch.Elapsed;
            loadWatch.Stop();
            return loadTime;
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
            LOG_DIRECTORY = Path.Combine(Environment.CurrentDirectory, "Logs").SafeFolder(),
            LOG_FILE_PATH = Path.Combine(Environment.CurrentDirectory, "Logs", "mymanga.log");

        public UserConfigurationObject UserConfiguration
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

        public AssemblyInformation AssemblyInfo
        { get; private set; }

        public App()
        {
            AppMemoryCache = new RegionedMemoryCache("AppMemoryCache");
            AssemblyInfo = new AssemblyInformation();

            // Load Embedded DLLs from Resources.
            InitializeEmbedded();

            // Configure log4net
            ConfigureLog4Net();

            // Handle unhandled exceptions
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Initialize Collection
            MangaCacheObjects = new ObservableCollection<MangaCacheObject>();

            // Create a File System Watcher for Manga Objects
            MangaObjectArchiveWatcher = new FileSystemWatcher(MANGA_ARCHIVE_DIRECTORY, MANGA_ARCHIVE_FILTER);
            MangaObjectArchiveWatcher.EnableRaisingEvents = false;

            // Create a File System Watcher for Manga Chapter Objects
            ChapterObjectArchiveWatcher = new FileSystemWatcher(CHAPTER_ARCHIVE_DIRECTORY, CHAPTER_ARCHIVE_FILTER);
            ChapterObjectArchiveWatcher.IncludeSubdirectories = true;
            ChapterObjectArchiveWatcher.EnableRaisingEvents = false;

            // Create IO class objects
            ZipManager = new ZipManager(); // v2 - Async/Await based
            ContentDownloadManager = new ContentDownloadManager(); // v2 - Async/Await based

            Startup += App_Startup;
            Exit += App_Exit;

            InitializeComponent();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.Error(sender.GetType().FullName, e.ExceptionObject as Exception);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            logger.Error(sender.GetType().FullName, e.Exception);
            e.Handled = true;
        }

        #region Application Events
        private async void App_Startup(object sender, StartupEventArgs e)
        {
            SiteExtensions.Load(PLUGIN_DIRECTORY, Filter: "*.mymanga.dll");
            DatabaseExtensions.Load(PLUGIN_DIRECTORY, Filter: "*.mymanga.dll");

            LoadUserConfig();
            LoadUserAuthenticate();
            UserConfiguration.UserConfigurationUpdated += (_s, _e) => SaveUserConfig();

            // Enable FileSystemWatchers
            ConfigureFileWatchers();

            // Run initial load of cache
            //Task.Factory.StartNew(FullMangaCacheObject);
            await FullMangaCacheObject().ConfigureAwait(false);

            MangaObjectArchiveWatcher.EnableRaisingEvents = true;
            ChapterObjectArchiveWatcher.EnableRaisingEvents = true;
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            AppMemoryCache.Dispose();
            MangaObjectArchiveWatcher.Dispose();
            ChapterObjectArchiveWatcher.Dispose();

            ContentDownloadManager.Dispose();
            ZipManager.Dispose();

            SiteExtensions.Unload();
            DatabaseExtensions.Unload();
        }
        #endregion

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
                        MangaCacheObject ReloadedMangaCacheObject = await DispatcherReloadMangaCacheObjectAsync(e.FullPath, Equals(ExistingMangaCacheObject.CoverImage, null));
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

                        Messenger.Instance.Send((ExistingIndex >= 0) ? MangaCacheObjects[ExistingIndex] : null, "SelectMangaCacheObject");
                        break;

                    default:
                        break;
                }
                Messenger.Instance.Send(e, "MangaObjectArchiveWatcher");
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

                Messenger.Instance.Send(e, "ChapterObjectArchiveWatcher");
            });
        }
        #endregion

        #region User Config Files
        private void LoadUserAuthenticate()
        {
            if (File.Exists(USER_AUTH_PATH))
                using (Stream UserAuthenticationStream = File.OpenRead(USER_AUTH_PATH))
                {
                    try { UserAuthentication = UserAuthenticationStream.Deserialize<UserAuthenticationObject>(SerializeType: SerializeType.XML); }
                    catch { }
                }

            if (UserAuthenticationObject.Equals(UserAuthentication, null))
            {
                UserAuthentication = new UserAuthenticationObject();
            }

            CancellationTokenSource cts = new CancellationTokenSource();
            foreach (UserPluginAuthenticationObject upa in this.UserAuthentication.UserPluginAuthentications)
            {
                try
                {
                    ISiteExtension siteExtension = SiteExtensions.DLLCollection[upa.PluginName];
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
            if (File.Exists(USER_CONFIG_PATH))
                using (Stream UserConfigStream = File.OpenRead(USER_CONFIG_PATH))
                {
                    try { UserConfiguration = UserConfigStream.Deserialize<UserConfigurationObject>(SerializeType: SerializeType.XML); }
                    catch { }
                }
            if (UserConfigurationObject.Equals(this.UserConfiguration, null))
            {
                UserConfiguration = new UserConfigurationObject();

                // Enable all available Database Extensions
                foreach (IDatabaseExtension DatabaseExtension in DatabaseExtensions.DLLCollection)
                    UserConfiguration.EnabledDatabaseExtensions.Add(DatabaseExtension.DatabaseExtensionDescriptionAttribute.Name);

                // Enable the first Site Extention if available
                if (SiteExtensions.DLLCollection.Count > 0)
                    UserConfiguration.EnabledSiteExtensions.Add(SiteExtensions.DLLCollection[0].SiteExtensionDescriptionAttribute.Name);
                SaveUserConfig();
            }
            ApplyTheme(UserConfiguration.Theme);
        }

        public void SaveUserConfig()
        {
            using (FileStream fs = File.Open(USER_CONFIG_PATH, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                fs.SetLength(0);
                using (Stream UserConfigStream = UserConfiguration.Serialize(SerializeType: SerializeType.XML))
                { UserConfigStream.CopyTo(fs); }
            }
        }

        public void SaveUserAuthentication()
        {
            using (FileStream fs = File.Open(USER_AUTH_PATH, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                fs.SetLength(0);
                using (Stream UserAuthenticationStream = UserAuthentication.Serialize(SerializeType: SerializeType.XML))
                { UserAuthenticationStream.CopyTo(fs); }
            }
        }
        #endregion

        public void RunOnUiThread(Action action)
        {
            if (Dispatcher.Thread == Thread.CurrentThread) action();
            else Dispatcher.Invoke(DispatcherPriority.Send, action);
        }
    }
}

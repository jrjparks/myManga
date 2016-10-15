using myManga_App.IO.Local.Object;
using myManga_App.IO.Network;
using myManga_App.Objects;
using myManga_App.Objects.About;
using myManga_App.Objects.Cache;
using myManga_App.Objects.UserConfig;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Communication;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
    public partial class App : Application, IDisposable
    {
        private Boolean IsRestarting { get; set; }

        #region Logging
        public static readonly log4net.ILog logger = log4net.LogManager.GetLogger(typeof(App));

        private void ConfigureLog4Net(log4net.Core.Level LogLevel = null)
        {
            if (Equals(LogLevel, null))
                LogLevel = log4net.Core.Level.All;
            log4net.Appender.RollingFileAppender appender = new log4net.Appender.RollingFileAppender();
            appender.Layout = new log4net.Layout.SimpleLayout();
            appender.File = CORE.LOG_FILE_PATH;
            appender.AppendToFile = true;
            appender.ImmediateFlush = true;
            appender.Threshold = LogLevel;
            appender.MaxSizeRollBackups = 3;
            appender.MaximumFileSize = "128KB";
            appender.RollingStyle = log4net.Appender.RollingFileAppender.RollingMode.Once;
            appender.ActivateOptions();
            log4net.Config.BasicConfigurator.Configure(appender);
        }
        #endregion

        #region Core
        public readonly CoreManagement CORE = new CoreManagement(logger);
        public readonly ContentDownloadManager ContentDownloadManager;

        // Used for Bindings
        public CoreManagement COREProperty { get { return CORE; } }
        #endregion

        #region IO
        private FileSystemWatcher MangaObjectArchiveWatcher
        { get; set; }

        private FileSystemWatcher ChapterObjectArchiveWatcher
        { get; set; }
        #endregion

        #region MangaObject Cache
        public ObservableCollection<MangaCacheObject> MangaCacheObjects
        { get; private set; }

        private async Task<MangaCacheObject> UnsafeDispatcherLoadMangaCacheObjectAsync(String ArchivePath)
        {
            return await Current.Dispatcher.Invoke(() => UnsafeLoadMangaCacheObjectAsync(ArchivePath), DispatcherPriority.DataBind);
        }

        private async Task<MangaCacheObject> UnsafeLoadMangaCacheObjectAsync(String ArchivePath)
        {
            try
            {
                MangaCacheObject MangaCacheObject = new MangaCacheObject();
                MangaCacheObject.ArchiveFileName = Path.GetFileName(ArchivePath);

                // Load BookmarkObject Data
                Stream BookmarkObjectStream = CORE.ZipManager.UnsafeRead(ArchivePath, typeof(BookmarkObject).Name);
                if (!Equals(BookmarkObjectStream, null))
                { using (BookmarkObjectStream) { MangaCacheObject.BookmarkObject = BookmarkObjectStream.Deserialize<BookmarkObject>(CORE.UserConfiguration.SerializeType); } }

                // Load MangaObject Data
                Stream MangaObjectStream = CORE.ZipManager.UnsafeRead(ArchivePath, typeof(MangaObject).Name);
                if (!Equals(MangaObjectStream, null))
                { using (MangaObjectStream) { MangaCacheObject.MangaObject = MangaObjectStream.Deserialize<MangaObject>(CORE.UserConfiguration.SerializeType); } }

                // Move archive to correct location if needed
                String CorrectArchivePath = Path.Combine(Path.GetDirectoryName(ArchivePath), MangaCacheObject.ArchiveFileName);
                if (!Equals(ArchivePath, CorrectArchivePath))
                {
                    File.Move(ArchivePath, CorrectArchivePath);
                    logger.Info(String.Format("MangaObject archive file was moved/renamed to '{0}'.", MangaCacheObject.ArchiveFileName));
                }

                // MangaObject update check
                Boolean VersionUpdated = false;
                if (!Equals(MangaCacheObject.MangaObject, null))
                    UpdateMangaObjectVersion(MangaCacheObject.MangaObject, ref VersionUpdated);
                if (VersionUpdated)
                {
                    logger.Info(String.Format("MangaObject version was updated for '{0}'.", MangaCacheObject.MangaObject.Name));
                    await CORE.ZipManager.WriteAsync(
                        CorrectArchivePath, typeof(MangaObject).Name,
                        MangaCacheObject.MangaObject.Serialize(CORE.UserConfiguration.SerializeType)).Retry(TimeSpan.FromMinutes(1));
                }

                // Load Cover Image
                IEnumerable<String> Entries = CORE.ZipManager.UnsafeGetEntries(CorrectArchivePath);
                LocationObject SelectedCoverLocationObject = MangaCacheObject.MangaObject.SelectedCover();
                String CoverImageFileName = Path.GetFileName(SelectedCoverLocationObject.Url);
                if (!Entries.Contains(CoverImageFileName))
                {
                    // Try to download the missing cover;
                    ContentDownloadManager.DownloadCover(MangaCacheObject.MangaObject, SelectedCoverLocationObject);
                    // If the SelectedCover is not in the archive file select a new cover.
                    String Url = (from CoverLocation in MangaCacheObject.MangaObject.CoverLocations
                                  where Entries.Contains(Path.GetFileName(CoverLocation.Url))
                                  select CoverLocation.Url).FirstOrDefault();
                    if (!Equals(Url, null))
                        CoverImageFileName = Path.GetFileName(Url);
                }
                Stream CoverImageStream = CORE.ZipManager.UnsafeRead(CorrectArchivePath, CoverImageFileName);
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
                logger.Error(String.Format("Unable to read Manga Archive.\nFile: {0}", ArchivePath), ex);
                return null;
            }
        }

        private async Task<MangaCacheObject> DispatcherReloadMangaCacheObjectAsync(String ArchivePath, Boolean ReloadCoverImage = false)
        {
            return await Current.Dispatcher.Invoke(() => ReloadMangaCacheObjectAsync(ArchivePath, ReloadCoverImage), DispatcherPriority.DataBind);
        }

        private async Task<MangaCacheObject> ReloadMangaCacheObjectAsync(String ArchivePath, Boolean ReloadCoverImage = false)
        {
            try
            {
                MangaCacheObject MangaCacheObject = new MangaCacheObject();
                MangaCacheObject.ArchiveFileName = Path.GetFileName(ArchivePath);

                // Load BookmarkObject Data
                Stream BookmarkObjectStream = await CORE.ZipManager.ReadAsync(ArchivePath, typeof(BookmarkObject).Name).Retry(TimeSpan.FromMinutes(1));
                if (!Equals(BookmarkObjectStream, null))
                { using (BookmarkObjectStream) { MangaCacheObject.BookmarkObject = BookmarkObjectStream.Deserialize<BookmarkObject>(CORE.UserConfiguration.SerializeType); } }

                // Load MangaObject Data
                Stream MangaObjectStream = await CORE.ZipManager.ReadAsync(ArchivePath, typeof(MangaObject).Name).Retry(TimeSpan.FromMinutes(1));
                if (!Equals(MangaObjectStream, null))
                { using (MangaObjectStream) { MangaCacheObject.MangaObject = MangaObjectStream.Deserialize<MangaObject>(CORE.UserConfiguration.SerializeType); } }

                // Move archive to correct location if needed
                String CorrectArchivePath = Path.Combine(Path.GetDirectoryName(ArchivePath), MangaCacheObject.ArchiveFileName);
                if (!Equals(ArchivePath, CorrectArchivePath))
                {
                    File.Move(ArchivePath, CorrectArchivePath);
                    logger.Info(String.Format("MangaObject archive file was moved/renamed to '{0}'.", MangaCacheObject.ArchiveFileName));
                }

                // MangaObject update check
                Boolean VersionUpdated = false;
                if (!Equals(MangaCacheObject.MangaObject, null))
                    UpdateMangaObjectVersion(MangaCacheObject.MangaObject, ref VersionUpdated);
                if (VersionUpdated)
                {
                    logger.Info(String.Format("MangaObject version was updated for '{0}'.", MangaCacheObject.MangaObject.Name));
                    await CORE.ZipManager.WriteAsync(
                        CorrectArchivePath, typeof(MangaObject).Name,
                        MangaCacheObject.MangaObject.Serialize(CORE.UserConfiguration.SerializeType)).Retry(TimeSpan.FromMinutes(1));
                }

                if (ReloadCoverImage)
                {
                    // Load Cover Image
                    IEnumerable<String> Entries = await CORE.ZipManager.GetEntriesAsync(CorrectArchivePath).Retry(TimeSpan.FromMinutes(1));
                    LocationObject SelectedCoverLocationObject = MangaCacheObject.MangaObject.SelectedCover();
                    String CoverImageFileName = Path.GetFileName(SelectedCoverLocationObject.Url);
                    if (!Entries.Contains(CoverImageFileName))
                    {
                        // Try to download the missing cover;
                        ContentDownloadManager.DownloadCover(MangaCacheObject.MangaObject, SelectedCoverLocationObject);
                        // If the SelectedCover is not in the archive file select a new cover.
                        String Url = (from CoverLocation in MangaCacheObject.MangaObject.CoverLocations
                                      where Entries.Contains(Path.GetFileName(CoverLocation.Url))
                                      select CoverLocation.Url).FirstOrDefault();
                        if (!Equals(Url, null))
                            CoverImageFileName = Path.GetFileName(Url);
                    }
                    Stream CoverImageStream = await CORE.ZipManager.ReadAsync(CorrectArchivePath, CoverImageFileName).Retry(TimeSpan.FromMinutes(1));
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
                logger.Error(String.Format("Unable to read Manga Archive.\nFile: {0}", ArchivePath), ex);
                return null;
            }
        }

        /// <summary>
        /// Update MangaObject to current version
        /// </summary>
        /// <param name="MangaObject">MangaObject to update</param>
        /// <param name="Updated">Was the MangaObject updated</param>
        /// <returns>Updated MangaObject</returns>
        private MangaObject UpdateMangaObjectVersion(MangaObject MangaObject, ref Boolean Updated)
        {
            Updated = false;

            #region Re-enable LocationObjects
            /*
            TODO: This is a quick fix and should be removed!
            */
            Updated = MangaObject.Locations.Count(LocObj => !LocObj.Enabled) > 0;
            MangaObject.Locations.ForEach(LocObj => LocObj.Enabled = true);

            Updated = MangaObject.DatabaseLocations.Count(LocObj => !LocObj.Enabled) > 0;
            MangaObject.DatabaseLocations.ForEach(LocObj => LocObj.Enabled = true);
            #endregion

            #region Check for old location object types.
            Regex NameLanguageSplitRegex = new Regex(@"[^\w]");
            foreach (LocationObject LocObj in MangaObject.Locations)
            {
                if (Equals(LocObj.ExtensionLanguage, null))
                {
                    Updated = true;
                    String Language = "English";
                    String[] NameLanguage = NameLanguageSplitRegex.Split(LocObj.ExtensionName);
                    if (NameLanguage.Length > 1) Language = NameLanguage[1];
                    LocObj.ExtensionLanguage = Language;
                    logger.Info(String.Format("[{0}] Setting language of '{1}' to '{2}'", MangaObject.Name, LocObj.ExtensionName, Language));
                }
            }
            foreach (LocationObject LocObj in MangaObject.DatabaseLocations)
            {
                if (Equals(LocObj.ExtensionLanguage, null))
                {
                    Updated = true;
                    String Language = "English";
                    String[] NameLanguage = NameLanguageSplitRegex.Split(LocObj.ExtensionName);
                    if (NameLanguage.Length > 1) Language = NameLanguage[1];
                    LocObj.ExtensionLanguage = Language;
                    logger.Info(String.Format("[{0}] Setting language of '{1}' to '{2}'", MangaObject.Name, LocObj.ExtensionName, Language));
                }
            }
            #endregion

            #region Migrate covers to new format.
            foreach (String Cover in MangaObject.Covers)
            {
                Updated = true;
                IExtension Extension = null;
                if (Cover.Contains("mhcdn.net")) Extension = CORE.Extensions["MangaHere", "English"];
                else Extension = CORE.Extensions.FirstOrDefault(_ => Cover.Contains(_.ExtensionDescriptionAttribute.URLFormat));
                if (!Equals(Extension, null))
                    MangaObject.CoverLocations.Add(new LocationObject()
                    {
                        Url = Cover,
                        Enabled = true,
                        ExtensionName = Extension.ExtensionDescriptionAttribute.Name,
                        ExtensionLanguage = Extension.ExtensionDescriptionAttribute.Language,
                    });
                logger.Info(String.Format("[{0}] Migrating cover to location: {1}", MangaObject.Name, Cover));
            }

            // Remove duplicates
            MangaObject.CoverLocations = (from CoverLocation in MangaObject.CoverLocations
                                          group CoverLocation by CoverLocation.Url
                                         into CoverLocationGroups
                                          select CoverLocationGroups.FirstOrDefault()).ToList();
            MangaObject.CoverLocations.RemoveAll(_ => Equals(_, null));
            MangaObject.Covers.Clear();
            #endregion

            return MangaObject;
        }

        private async Task RenameSchema()
        {
            // Rename old schemas to new schema format
            IEnumerable<String> chapterFileZipPaths = Directory.EnumerateFiles(CORE.CHAPTER_ARCHIVE_DIRECTORY, "*.ca.*", SearchOption.AllDirectories),
                mangaFileZipPaths = Directory.EnumerateFiles(CORE.MANGA_ARCHIVE_DIRECTORY, "*.ma.*", SearchOption.AllDirectories);

            await Task.WhenAll(
                Task.Factory.StartNew(() => Parallel.ForEach(mangaFileZipPaths, mangaFileZipPath =>
                {
                    // Manga Archives
                    Int32 indexOfCA = mangaFileZipPath.LastIndexOf(".ma.");
                    String fileName = mangaFileZipPath.Substring(0, indexOfCA),
                        fileExtension = mangaFileZipPath.Substring(indexOfCA + 1);
                    if (!Equals(fileExtension, CORE.MANGA_ARCHIVE_EXTENSION))
                    {
                        File.Move(
                            mangaFileZipPath,
                            String.Format("{0}.{1}", fileName, CORE.MANGA_ARCHIVE_EXTENSION));
                    }
                })),
                Task.Factory.StartNew(() => Parallel.ForEach(chapterFileZipPaths, chapterFileZipPath =>
                {
                    // Chapter Archives
                    Int32 indexOfCA = chapterFileZipPath.LastIndexOf(".ca.");
                    String fileName = chapterFileZipPath.Substring(0, indexOfCA),
                        fileExtension = chapterFileZipPath.Substring(indexOfCA + 1);
                    if (!Equals(fileExtension, CORE.CHAPTER_ARCHIVE_EXTENSION))
                    {
                        File.Move(
                            chapterFileZipPath,
                            String.Format("{0}.{1}", fileName, CORE.CHAPTER_ARCHIVE_EXTENSION));
                    }
                }))
            );
        }

        /// <summary>
        /// Warning, this will completely reload the cache.
        /// </summary>
        /// <returns>Time taken to load cache.</returns>
        private async Task<TimeSpan> FullMangaCacheObject()
        {
            Boolean ErrorOccured = false;
            Stopwatch loadWatch = Stopwatch.StartNew();
            await RenameSchema();

            String[] MangaArchivePaths = Directory.GetFiles(CORE.MANGA_ARCHIVE_DIRECTORY, CORE.MANGA_ARCHIVE_FILTER, SearchOption.TopDirectoryOnly);

            IEnumerable<Task<MangaCacheObject>> MangaCacheObjectTasksQuery =
                from MangaArchivePath in MangaArchivePaths
                    // select UnsafeDispatcherLoadMangaCacheObjectAsync(MangaArchivePath);
                select DispatcherReloadMangaCacheObjectAsync(MangaArchivePath, true);
            List<Task<MangaCacheObject>> MangaCacheObjectTasks = MangaCacheObjectTasksQuery.ToList();

            await Current.Dispatcher.InvokeAsync(MangaCacheObjects.Clear);
            while (MangaCacheObjectTasks.Count > 0)
            {
                Task<MangaCacheObject> completedTask = await Task.WhenAny(MangaCacheObjectTasks);
                MangaCacheObjectTasks.Remove(completedTask);

                MangaCacheObject LoadedMangaCacheObject = await completedTask;
                if (!Equals(LoadedMangaCacheObject, null))
                { await Current.Dispatcher.InvokeAsync(() => MangaCacheObjects.Add(LoadedMangaCacheObject)); }
                else ErrorOccured = true;
            }

            TimeSpan loadTime = loadWatch.Elapsed;
            loadWatch.Stop();
            if (ErrorOccured)
            {
                String[] ErrorMessageArray = {
                        "There was an error when attempting to load a manga on startup.",
                        "Please check the most recent log file for the full error."
                    };
                String ErrorMessage = String.Join(Environment.NewLine, ErrorMessageArray);
                MessageBox.Show(
                    ErrorMessage,
                    "Error During Loading Manga!",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    MessageBoxResult.OK);
            }
            return loadTime;
        }
        #endregion

        #region Theme Resource Dictionary
        public ResourceDictionary ThemeResourceDictionary
        {
            get { return Resources.MergedDictionaries[0]; }
            set { Resources.MergedDictionaries[0] = value; }
        }
        public void ApplyTheme(ThemeType theme)
        {
            RunOnUiThread(delegate
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
            });
        }
        #endregion

        #region Localization Resource Dictionary
        public ResourceDictionary LocalizationResourceDictionary
        {
            get { return Resources.MergedDictionaries[1]; }
            set { Resources.MergedDictionaries[1] = value; }
        }
        public void ApplyLocalization(CultureInfo cultureInfo)
        {
            RunOnUiThread(delegate
            {
                String[] SupportedLanguages = {
                    "en",
                };
                if (SupportedLanguages.Contains(cultureInfo.Name))
                    ThemeResourceDictionary.Source = new Uri(String.Format("/myManga;component/Resources/Localization/Dictionary_{0}.xaml", cultureInfo.Name), UriKind.RelativeOrAbsolute);
            });
        }
        #endregion

        public AssemblyInformation AssemblyInfo
        { get; private set; }

        public App()
        {
            AssemblyInfo = new AssemblyInformation();

            // Configure log4net
            ConfigureLog4Net();

            // Handle unhandled exceptions
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Initialize Collection
            MangaCacheObjects = new ObservableCollection<MangaCacheObject>();

            // Create a File System Watcher for Manga Objects
            MangaObjectArchiveWatcher = new FileSystemWatcher(CORE.MANGA_ARCHIVE_DIRECTORY, CORE.MANGA_ARCHIVE_FILTER);
            MangaObjectArchiveWatcher.EnableRaisingEvents = false;

            // Create a File System Watcher for Manga Chapter Objects
            ChapterObjectArchiveWatcher = new FileSystemWatcher(CORE.CHAPTER_ARCHIVE_DIRECTORY, CORE.CHAPTER_ARCHIVE_FILTER);
            ChapterObjectArchiveWatcher.IncludeSubdirectories = true;
            ChapterObjectArchiveWatcher.EnableRaisingEvents = false;

            // Load UserConfig
            LoadUserConfig();

            // Initialize the ContentDownloadManager v2
            ContentDownloadManager = new ContentDownloadManager(CORE: CORE);

            Startup += App_Startup;
            Exit += App_Exit;

            InitializeComponent();
        }

        ~App()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #region Application Events

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.Error(sender.GetType().FullName, e.ExceptionObject as Exception);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            logger.Error(sender.GetType().FullName, e.Exception);
#if !DEBUG
            // Handle error if in release mode.
            e.Handled = true;
#endif
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            ApplyTheme(CORE.UserConfiguration.Theme);
            CORE.UserConfiguration.UserConfigurationUpdated += UserConfiguration_UserConfigurationUpdated;
            LoadUserAuthenticate();

            // Run initial load of cache
            Task.Factory.StartNew(FullMangaCacheObject);

            // Enable FileSystemWatchers
            ConfigureFileWatchers();

            MangaObjectArchiveWatcher.EnableRaisingEvents = true;
            ChapterObjectArchiveWatcher.EnableRaisingEvents = true;
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            Dispose(true);
            if (IsRestarting) { Process.Start(ResourceAssembly.Location); }
        }

        private void UserConfiguration_UserConfigurationUpdated(Object sender, GenericEventArgs<String> e)
        {
            String[] PropertyWhitelist = {
                    "WindowSizeHeight",
                    "WindowSizeWidth",
                    "WindowState",
                    "ViewTypes"
                },
                PropertyRestartlist = {
                    "SerializeType",
                    "ConcurrencyMultiplier"
                };
            if (PropertyWhitelist.Contains(e.Value)) { SaveUserConfiguration(); }
            if (PropertyRestartlist.Contains(e.Value))
            {
                String[] RestartMessageArray = {
                        "You have made a change that requires a restart of myManga to take affect.",
                        "Would you like to restart myManga now?"
                    };
                String RestartMessage = String.Join(Environment.NewLine, RestartMessageArray);
                if (MessageBox.Show(
                    RestartMessage,
                    "Restart Required!",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.Yes) == MessageBoxResult.Yes)
                { Restart(TimeSpan.FromSeconds(1)); }
            }
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
                        if (!Equals(ExistingMangaCacheObject, null))
                        {
                            // If ExistingMangaCacheObject is null we are probably still loading.
                            MangaCacheObject ReloadedMangaCacheObject = await DispatcherReloadMangaCacheObjectAsync(e.FullPath, Equals(ExistingMangaCacheObject.CoverImage, null));
                            if (!Equals(ReloadedMangaCacheObject, null))
                            {
                                if (Equals(ExistingMangaCacheObject, null))
                                { MangaCacheObjects.Add(ReloadedMangaCacheObject); }
                                else
                                { ExistingMangaCacheObject.Update(ReloadedMangaCacheObject); }
                            }
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
            if (File.Exists(CORE.USER_AUTH_PATH))
                using (Stream UserAuthenticationStream = File.OpenRead(CORE.USER_AUTH_PATH))
                {
                    try { CORE.UpdateUserAuthentication(UserAuthenticationStream.Deserialize<UserAuthenticationObject>(SerializeType: SerializeType.XML)); }
                    catch { }
                }

            if (UserAuthenticationObject.Equals(CORE.UserAuthentication, null))
            { CORE.UpdateUserAuthentication(new UserAuthenticationObject()); }

            CancellationTokenSource cts = new CancellationTokenSource();
            foreach (UserPluginAuthenticationObject upa in CORE.UserAuthentication.UserPluginAuthentications)
            {
                try
                {
                    IExtension extension = CORE.Extensions[upa.PluginName, upa.PluginLanguage];
                    extension.Authenticate(new System.Net.NetworkCredential(upa.Username, upa.Password), cts.Token, null);
                }
                catch
                {
                    MessageBox.Show(String.Format("There was an error decoding {0} ({1}). Please reauthenticate.", upa.PluginName, upa.PluginLanguage));
                }
            }
            SaveUserAuthentication();
        }

        private void LoadUserConfig()
        {
            if (File.Exists(CORE.USER_CONFIG_PATH))
                using (Stream UserConfigStream = File.OpenRead(CORE.USER_CONFIG_PATH))
                {
                    try { CORE.UpdateUserConfiguration(UserConfigStream.Deserialize<UserConfigurationObject>(SerializeType: SerializeType.XML)); }
                    catch { }
                }
            if (UserConfigurationObject.Equals(CORE.UserConfiguration, null))
            {
                CORE.UpdateUserConfiguration(new UserConfigurationObject());

                // Enable all available Database Extensions
                foreach (IDatabaseExtension DatabaseExtension in CORE.DatabaseExtensions)
                    CORE.UserConfiguration.EnabledExtensions.Add(new EnabledExtensionObject(DatabaseExtension) { Enabled = true });

                // Enable the first site
                foreach (var o in CORE.SiteExtensions.Select((sExt, idx) => new { Index = idx, Value = sExt }))
                { CORE.UserConfiguration.EnabledExtensions.Add(new EnabledExtensionObject(o.Value) { Enabled = Equals(o.Index, 0) }); }
                SaveUserConfiguration();
            }
        }

        public void SaveUserConfiguration()
        {
            using (FileStream fs = File.Open(CORE.USER_CONFIG_PATH, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                fs.SetLength(0);
                using (Stream UserConfigStream = CORE.UserConfiguration.Serialize(SerializeType: SerializeType.XML))
                { UserConfigStream.CopyTo(fs); }
            }
        }

        public void SaveUserAuthentication()
        {
            using (FileStream fs = File.Open(CORE.USER_AUTH_PATH, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                fs.SetLength(0);
                using (Stream UserAuthenticationStream = CORE.UserAuthentication.Serialize(SerializeType: SerializeType.XML))
                { UserAuthenticationStream.CopyTo(fs); }
            }
        }
        #endregion

        public void RunOnUiThread(Action action)
        {
            if (Dispatcher.Thread == Thread.CurrentThread) action();
            else Dispatcher.Invoke(DispatcherPriority.Send, action);
        }

        public void Restart() =>
            Restart(TimeSpan.FromSeconds(1));
        public void Restart(TimeSpan Delay) =>
            Task.Factory.StartNew(async state =>
            {
                TimeSpan _Delay = (TimeSpan)state;
                if (_Delay > TimeSpan.Zero) await Task.Delay(_Delay);
                RunOnUiThread(delegate
                {
                    IsRestarting = true;
                    Shutdown();
                });
            }, Delay);

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    MangaObjectArchiveWatcher.Dispose();
                    ChapterObjectArchiveWatcher.Dispose();
                    CORE.Dispose();
                    ContentDownloadManager.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}

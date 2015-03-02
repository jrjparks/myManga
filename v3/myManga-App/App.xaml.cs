﻿using System;
using System.IO;
using System.Windows;
using Core.DLL;
using Core.IO;
using myManga_App.Properties;
using myMangaSiteExtension;
using myMangaSiteExtension.Collections;
using Core.Other.Singleton;
using myMangaSiteExtension.Interfaces;
using myManga_App.Objects;
using Core.IO.Storage.Manager.BaseInterfaceClasses;
using myManga_App.Objects.About;
using System.Collections.ObjectModel;
using myManga_App.Objects.Cache;

namespace myManga_App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly FileSystemWatcher mangaObjectArchiveWatcher;
        public FileSystemWatcher MangaObjectArchiveWatcher
        { get { return mangaObjectArchiveWatcher; } }

        private readonly FileSystemWatcher chapterObjectArchiveWatcher;
        public FileSystemWatcher ChapterObjectArchiveWatcher
        { get { return chapterObjectArchiveWatcher; } }

        public UserConfigurationObject UserConfig
        { get; private set; }

        private readonly EmbeddedDLL emdll;
        public DLL_Manager<ISiteExtension, ISiteExtensionCollection> SiteExtensions
        { get { return Singleton<DLL_Manager<ISiteExtension, ISiteExtensionCollection>>.Instance; } }
        public DLL_Manager<IDatabaseExtension, IDatabaseExtensionCollection> DatabaseExtensions
        { get { return Singleton<DLL_Manager<IDatabaseExtension, IDatabaseExtensionCollection>>.Instance; } }

        private readonly ObservableCollection<MangaArchiveCacheObject> mangaArchiveCacheCollection;
        public ObservableCollection<MangaArchiveCacheObject> MangaArchiveCacheCollection
        { get { return this.mangaArchiveCacheCollection; } }

        public readonly String
            PLUGIN_DIRECTORY = Path.Combine(Environment.CurrentDirectory, "Plugins").SafeFolder(),
            MANGA_ARCHIVE_DIRECTORY = Path.Combine(Environment.CurrentDirectory, "Manga Archives").SafeFolder(),
            CHAPTER_ARCHIVE_DIRECTORY = Path.Combine(Environment.CurrentDirectory, "Chapter Archives").SafeFolder(),
            MANGA_ARCHIVE_EXTENSION = "ma.zip",
            CHAPTER_ARCHIVE_EXTENSION = "ca.zip",
            MANGA_ARCHIVE_FILTER = "*.ma.zip",
            CHAPTER_ARCHIVE_FILTER = "*.ca.zip",
            USER_CONFIG_FILENAME = "mymanga.conf",
            USER_CONFIG_PATH = Path.Combine(Environment.CurrentDirectory, "mymanga.conf".SafeFileName()),
            LOG_FILE_PATH = Path.Combine(Environment.CurrentDirectory, String.Format("mymanga-{0}-{1}.log", DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString()).SafeFileName());

        public AssemblyInformation AssemblyInfo { get { return AssemblyInformation.Default; } }

        public App()
        {
            // Load Embedded DLLs from Resources.
            emdll = new EmbeddedDLL();

            AppDomain.CurrentDomain.AssemblyResolve += emdll.ResolveAssembly;
            SiteExtensions.DLLAppDomain.AssemblyResolve += emdll.ResolveAssembly;
            DatabaseExtensions.DLLAppDomain.AssemblyResolve += emdll.ResolveAssembly;

            // Initialize Collections
            mangaArchiveCacheCollection = new ObservableCollection<MangaArchiveCacheObject>();

            // Create a File System Watcher for Manga Objects
            mangaObjectArchiveWatcher = new FileSystemWatcher(MANGA_ARCHIVE_DIRECTORY, MANGA_ARCHIVE_FILTER);
            mangaObjectArchiveWatcher.EnableRaisingEvents = false;

            // Create a File System Watcher for Manga Chapter Objects
            chapterObjectArchiveWatcher = new FileSystemWatcher(CHAPTER_ARCHIVE_DIRECTORY, CHAPTER_ARCHIVE_FILTER);
            chapterObjectArchiveWatcher.IncludeSubdirectories = true;
            chapterObjectArchiveWatcher.EnableRaisingEvents = false;

            Startup += App_Startup;
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            InitializeComponent();
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            using (Stream log_stream = Singleton<FileStorage>.Instance.Read(LOG_FILE_PATH))
            {
                using (StreamWriter log_stream_writer = new StreamWriter(log_stream))
                {
                    log_stream_writer.WriteLine(String.Format("========== {0} ==========", DateTime.Now.ToShortDateString()));

                    log_stream_writer.WriteLine(String.Format("========== DISPATCHER ==========", DateTime.Now.ToShortDateString()));
                    log_stream_writer.WriteLine(String.Format("Thread Name: {0}", e.Dispatcher.Thread.Name));
                    log_stream_writer.WriteLine(String.Format("Is ThreadPool Thread: {0}", e.Dispatcher.Thread.IsThreadPoolThread));

                    Exception ex = e.Exception;
                    while (ex != null)
                    {
                        log_stream_writer.WriteLine(String.Format("========== MESSAGE ==========", DateTime.Now.ToShortDateString()));
                        log_stream_writer.WriteLine(ex.Message);
                        log_stream_writer.WriteLine(String.Format("========== STACK TRACE ==========", DateTime.Now.ToShortDateString()));
                        log_stream_writer.WriteLine(ex.StackTrace);
                        log_stream_writer.WriteLine(String.Format("========== TARGET SITE ==========", DateTime.Now.ToShortDateString()));
                        log_stream_writer.WriteLine(ex.TargetSite);
                        ex = ex.InnerException;
                        log_stream_writer.WriteLine();
                    }
                }
            }
            e.Handled = true;
        }

        void App_Startup(object sender, StartupEventArgs e)
        {
            SiteExtensions.LoadDLL(PLUGIN_DIRECTORY, Filter: "*.mymanga.dll");
            DatabaseExtensions.LoadDLL(PLUGIN_DIRECTORY, Filter: "*.mymanga.dll");

            LoadUserConfig();
            UserConfig.UserConfigurationUpdated += (_s, _e) => SaveUserConfig();

            // Enable FileSystemWatchers
            mangaObjectArchiveWatcher.EnableRaisingEvents = true;
            chapterObjectArchiveWatcher.EnableRaisingEvents = true;
        }

        private void LoadUserConfig()
        {
            Stream UserConfigStream;
            if (Singleton<FileStorage>.Instance.TryRead(this.USER_CONFIG_PATH, out UserConfigStream))
            { using (UserConfigStream) { try { this.UserConfig = UserConfigStream.Deserialize<UserConfigurationObject>(SaveType: SaveType.XML); } catch { } } }
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
        }

        public void SaveUserConfig()
        { if (!UserConfigurationObject.Equals(this.UserConfig, null)) Singleton<FileStorage>.Instance.Write(this.USER_CONFIG_PATH, this.UserConfig.Serialize(SaveType: SaveType.XML)); }
    }
}

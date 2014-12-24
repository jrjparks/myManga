using System;
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

        public readonly String
            PLUGIN_DIRECTORY = Path.Combine(Environment.CurrentDirectory, "Plugins").SafeFolder(),
            MANGA_ARCHIVE_DIRECTORY = Path.Combine(Environment.CurrentDirectory, "Manga Archives").SafeFolder(),
            CHAPTER_ARCHIVE_DIRECTORY = Path.Combine(Environment.CurrentDirectory, "Chapter Archives").SafeFolder(),
            MANGA_ARCHIVE_EXTENSION = "ma.zip",
            CHAPTER_ARCHIVE_EXTENSION = "ca.zip",
            MANGA_ARCHIVE_FILTER = "*.ma.zip",
            CHAPTER_ARCHIVE_FILTER = "*.ca.zip",
            USER_CONFIG_FILENAME = "mymanga.conf",
            USER_CONFIG_PATH = Path.Combine(Environment.CurrentDirectory, "mymanga.conf");

        public AssemblyInformation AssemblyInfo { get { return AssemblyInformation.Default; } }

        public App()
        {
            // Load Embedded DLLs from Resources.
            emdll = new EmbeddedDLL();

            AppDomain.CurrentDomain.AssemblyResolve += emdll.ResolveAssembly;
            SiteExtensions.DLLAppDomain.AssemblyResolve += emdll.ResolveAssembly;
            DatabaseExtensions.DLLAppDomain.AssemblyResolve += emdll.ResolveAssembly;

            // Create a File System Watcher for Manga Objects
            mangaObjectArchiveWatcher = new FileSystemWatcher(MANGA_ARCHIVE_DIRECTORY, MANGA_ARCHIVE_FILTER);
            mangaObjectArchiveWatcher.EnableRaisingEvents = false;

            // Create a File System Watcher for Manga Chapter Objects
            chapterObjectArchiveWatcher = new FileSystemWatcher(CHAPTER_ARCHIVE_DIRECTORY, CHAPTER_ARCHIVE_FILTER);
            chapterObjectArchiveWatcher.IncludeSubdirectories = true;
            chapterObjectArchiveWatcher.EnableRaisingEvents = false;
            
            Startup += App_Startup;

            InitializeComponent();
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
            { using (UserConfigStream) this.UserConfig = UserConfigStream.Deserialize<UserConfigurationObject>(SaveType: SaveType.XML); }
            else
            {
                // Generate a default User Configuration
                this.UserConfig = new UserConfigurationObject();
                this.UserConfig.WindowSizeWidth = 640;
                this.UserConfig.WindowSizeHeight = 480;
                this.UserConfig.WindowState = WindowState.Normal;
                this.UserConfig.SaveType = SaveType.XML;
                SaveUserConfig();
            }
        }

        public void SaveUserConfig()
        { if(!UserConfigurationObject.Equals(this.UserConfig, null)) Singleton<FileStorage>.Instance.Write(this.USER_CONFIG_PATH, this.UserConfig.Serialize(SaveType: SaveType.XML)); }
    }
}

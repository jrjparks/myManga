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

        private readonly UserConfigurationObject userConfig;
        public UserConfigurationObject UserConfig
        { get { return userConfig; } }

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

        public App()
        {
            // Load Embedded DLLs from Resources.
            emdll = new EmbeddedDLL("Resources.DLL");

            userConfig = LoadUserConfig();

            // Create a File System Watcher for Manga Objects
            mangaObjectArchiveWatcher = new FileSystemWatcher(MANGA_ARCHIVE_DIRECTORY, MANGA_ARCHIVE_FILTER);
            mangaObjectArchiveWatcher.EnableRaisingEvents = true;

            // Create a File System Watcher for Manga Chapter Objects
            chapterObjectArchiveWatcher = new FileSystemWatcher(CHAPTER_ARCHIVE_DIRECTORY, CHAPTER_ARCHIVE_FILTER);
            chapterObjectArchiveWatcher.IncludeSubdirectories = true;
            chapterObjectArchiveWatcher.EnableRaisingEvents = true;

            AppDomain.CurrentDomain.AssemblyResolve += emdll.ResolveAssembly;
            SiteExtensions.DLLAppDomain.AssemblyResolve += emdll.ResolveAssembly;
            DatabaseExtensions.DLLAppDomain.AssemblyResolve += emdll.ResolveAssembly;

            Settings.Default.PropertyChanged += Default_PropertyChanged;

            Startup += App_Startup;

            InitializeComponent();
        }

        void App_Startup(object sender, StartupEventArgs e)
        {
            SiteExtensions.LoadDLL(PLUGIN_DIRECTORY, Filter: "*.mymanga.dll");
            DatabaseExtensions.LoadDLL(PLUGIN_DIRECTORY, Filter: "*.mymanga.dll");
        }

        private void Default_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        { Settings.Default.Save(); SaveUserConfig(); }

        private UserConfigurationObject LoadUserConfig()
        {
            UserConfigurationObject config = new UserConfigurationObject();
            if (File.Exists(USER_CONFIG_PATH))
                config = config.LoadObject(USER_CONFIG_PATH, SaveType.XML);
            else
                config.SaveObject(USER_CONFIG_PATH, SaveType.XML);
            Settings.Default.WindowWidth = (Int32)config.WindowSize.Width;
            Settings.Default.WindowHeight = (Int32)config.WindowSize.Height;
            Settings.Default.WindowState = config.WindowState;
            Settings.Default.SaveType = config.SaveType;
            return config;
        }

        public void SaveUserConfig()
        {
            String configPath = PathSafety.SafeFileName(USER_CONFIG_FILENAME);
            UserConfig.WindowSize = new Size(Settings.Default.WindowWidth, Settings.Default.WindowHeight);
            UserConfig.WindowState = Settings.Default.WindowState;
            UserConfig.SaveType = Settings.Default.SaveType;
            UserConfig.SaveObject(configPath, SaveType.XML);
        }
    }
}

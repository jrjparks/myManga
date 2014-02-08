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
            MANGA_ARCHIVE_EXTENSION = "ma",
            CHAPTER_ARCHIVE_EXTENSION = "ca",
            MANGA_ARCHIVE_FILTER = "*.ma",
            CHAPTER_ARCHIVE_FILTER = "*.ca",
            USER_CONFIG_FILENAME = "mymanga.conf";

        public App()
        {
            emdll = new EmbeddedDLL("Resources.DLL");

            mangaObjectArchiveWatcher = new FileSystemWatcher(MANGA_ARCHIVE_DIRECTORY, MANGA_ARCHIVE_FILTER);
            mangaObjectArchiveWatcher.EnableRaisingEvents = true;

            userConfig = LoadUserConfig();

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
        { Settings.Default.Save(); }

        private UserConfigurationObject LoadUserConfig()
        {
            UserConfigurationObject config = new UserConfigurationObject();
            String configPath = PathSafety.SafeFileName(USER_CONFIG_FILENAME);
            if (File.Exists(configPath))
                config.LoadObject(configPath, SaveType.XML);
            else
                config.SaveObject(configPath, SaveType.XML);
            return config;
        }

        public void SaveUserConfig()
        {
            String configPath = PathSafety.SafeFileName(USER_CONFIG_FILENAME);
            UserConfig.SaveObject(configPath, SaveType.XML);
        }
    }
}

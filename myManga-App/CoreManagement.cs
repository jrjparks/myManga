using myManga_App.IO.DLL;
using myManga_App.IO.Local;
using myManga_App.IO.Network;
using myManga_App.Objects.UserConfig;
using myMangaSiteExtension.Collections;
using myMangaSiteExtension.Interfaces;
using System;
using System.IO;
using System.Linq;

namespace myManga_App
{
    /// <summary>
    /// This class stores the common objects used
    /// </summary>
    public sealed class CoreManagement : IDisposable
    {
        public CoreManagement()
            :this(log4net.LogManager.GetLogger("Default Logger"))
        {}
        public CoreManagement(log4net.ILog logger)
        {
            Logger = logger;
            InitializeEmbedded();
            ExtensionsManager.Load(PLUGIN_DIRECTORY, PLUGIN_FILTER);
        }

        ~CoreManagement()
        {
            Dispose();
        }

        public void Dispose()
        {
            DeinitializeEmbedded();
            ExtensionsManager.Unload();
            ZipManager.Dispose();
        }

        #region IO
        public readonly log4net.ILog Logger;
        public readonly ZipManager ZipManager = new ZipManager();
        #endregion

        #region DLL Management
        private readonly Embedded embedded = new Embedded();

        #region DLL Management Storage
        public readonly Manager<IExtension, IExtensionCollection<IExtension>> ExtensionsManager = new Manager<IExtension, IExtensionCollection<IExtension>>();

        public IExtensionCollection<IExtension> Extensions =>
            new IExtensionCollection<IExtension>(ExtensionsManager.DLLCollection.OfType<IExtension>());
        public IExtensionCollection<ISiteExtension> SiteExtensions =>
            new IExtensionCollection<ISiteExtension>(ExtensionsManager.DLLCollection.OfType<ISiteExtension>());
        public IExtensionCollection<IDatabaseExtension> DatabaseExtensions =>
            new IExtensionCollection<IDatabaseExtension>(ExtensionsManager.DLLCollection.OfType<IDatabaseExtension>());
        #endregion

        #region De/Initialization of AssemblyResolve
        private void InitializeEmbedded()
        {
            AppDomain.CurrentDomain.AssemblyResolve += embedded.ResolveAssembly;
            ExtensionsManager.ManagerAppDomain.AssemblyResolve += embedded.ResolveAssembly;
        }

        private void DeinitializeEmbedded()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= embedded.ResolveAssembly;
            ExtensionsManager.ManagerAppDomain.AssemblyResolve -= embedded.ResolveAssembly;
        }
        #endregion

        #endregion

        #region Configuration
        public readonly String

            // Plugin consts
            PLUGIN_DIRECTORY = Path.Combine(Environment.CurrentDirectory, "Plugins").SafeFolder(),
            PLUGIN_FILTER = "*.mymanga.dll",

            // Manga consts
            MANGA_ARCHIVE_DIRECTORY = Path.Combine(Environment.CurrentDirectory, "Manga Archives").SafeFolder(),
            MANGA_ARCHIVE_EXTENSION = "ma.zip",
            MANGA_ARCHIVE_FILTER = "*.ma.zip",

            // Chapter consts
            CHAPTER_ARCHIVE_DIRECTORY = Path.Combine(Environment.CurrentDirectory, "Chapter Archives").SafeFolder(),
            CHAPTER_ARCHIVE_EXTENSION = "ca.cbz",
            CHAPTER_ARCHIVE_FILTER = "*.ca.cbz",

            // Config files consts
            USER_CONFIG_FILENAME = "mymanga.conf",
            USER_AUTH_FILENAME = "mymanga.auth.conf",
            USER_CONFIG_PATH = Path.Combine(Environment.CurrentDirectory, "mymanga.conf".SafeFileName()),
            USER_AUTH_PATH = Path.Combine(Environment.CurrentDirectory, "mymanga.auth.conf".SafeFileName()),

            // Logging consts
            LOG_DIRECTORY = Path.Combine(Environment.CurrentDirectory, "Logs").SafeFolder(),
            LOG_FILE_PATH = Path.Combine(Environment.CurrentDirectory, "Logs", "mymanga.log");

        public UserConfigurationObject UserConfiguration
        { get; private set; }
        public void UpdateUserConfiguration(UserConfigurationObject UserConfiguration)
        { this.UserConfiguration = UserConfiguration; }

        public UserAuthenticationObject UserAuthentication
        { get; private set; }
        public void UpdateUserAuthentication(UserAuthenticationObject UserAuthentication)
        { this.UserAuthentication = UserAuthentication; }
        #endregion
    }
}

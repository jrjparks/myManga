using System;
using System.IO;
using System.Windows;
using Core.DLL;
using Core.IO;
using myManga_App.Properties;
using myMangaSiteExtension;
using myMangaSiteExtension.Collections;

namespace myManga_App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly EmbeddedDLL emdll;
        protected DLL_Manager<ISiteExtension, ISiteExtensionCollection> siteExtensions;
        public DLL_Manager<ISiteExtension, ISiteExtensionCollection> SiteExtensions
        {
            get { return siteExtensions ?? (siteExtensions = new DLL_Manager<ISiteExtension, ISiteExtensionCollection>()); }
            protected set { siteExtensions = value; }
        }

        public readonly String PLUGIN_DIRECTORY;

        public App()
        {
            PLUGIN_DIRECTORY = Path.Combine(Environment.CurrentDirectory, "Plugins").SafeFolder();
            emdll = new EmbeddedDLL("Resources.DLL");
            SiteExtensions.LoadDLL(PLUGIN_DIRECTORY);

            AppDomain.CurrentDomain.AssemblyResolve += emdll.ResolveAssembly;
            SiteExtensions.DLLAppDomain.AssemblyResolve += emdll.ResolveAssembly;

            Settings.Default.PropertyChanged += Default_PropertyChanged;

            InitializeComponent();
        }

        void Default_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Settings.Default.Save();
        }
    }
}

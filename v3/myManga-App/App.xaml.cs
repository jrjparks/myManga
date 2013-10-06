using System;
using System.IO;
using System.Windows;
using Core.DLL;
using Core.IO;
using myManga_App.Properties;
using myMangaSiteExtension;
using myMangaSiteExtension.Collections;
using Core.Other.Singleton;

namespace myManga_App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly EmbeddedDLL emdll;
        public DLL_Manager<ISiteExtension, ISiteExtensionCollection> SiteExtensions
        {
            get { return Singleton<DLL_Manager<ISiteExtension, ISiteExtensionCollection>>.Instance; }
        }

        public readonly String PLUGIN_DIRECTORY;

        public App()
        {
            PLUGIN_DIRECTORY = Path.Combine(Environment.CurrentDirectory, "Plugins").SafeFolder();
            emdll = new EmbeddedDLL("Resources.DLL");

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

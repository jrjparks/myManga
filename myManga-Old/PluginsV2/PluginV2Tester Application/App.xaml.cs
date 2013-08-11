using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using BakaBox;
using BakaBox.DLL;
using IMangaSite;
using System.IO;

namespace PluginV2Tester_Application
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly EmbeddedDLLs eDLLs;
        public App()
        {
            eDLLs = new EmbeddedDLLs("Resources.DLLs");
            Singleton<IMangaPluginManager>.Instance.PluginAppDomain.AssemblyResolve += eDLLs.ResolveAssembly;
            Singleton<IMangaPluginManager>.Instance.LoadPluginDirectory(Path.Combine(Environment.CurrentDirectory, "Plugins"));
            InitializeComponent();
        }
    }
}

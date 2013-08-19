using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using Core.DLL;
using myManga_App.Properties;

namespace myManga_App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly EmbeddedDLL emdll;
        public App()
        {
            emdll = new EmbeddedDLL("Resources.DLL");
            AppDomain.CurrentDomain.AssemblyResolve += emdll.ResolveAssembly;

            Settings.Default.PropertyChanged += Default_PropertyChanged;

            InitializeComponent();
        }

        void Default_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Settings.Default.Save();
        }
    }
}

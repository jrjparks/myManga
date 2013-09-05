using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using Core.DLL;
using myMangaSiteExtension;
using myMangaSiteExtension.Collections;

namespace myManga_App.ViewModels
{
    public sealed class MainViewModel : DependencyObject, IDisposable
    {
        private App app;
        private App App { get { return app ?? (app = Application.Current as App); } }

        public MainViewModel()
        {
            if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                App.SiteExtensions.LoadDLL(App.PLUGIN_DIRECTORY);
        }

        public void Dispose() { App.SiteExtensions.Unload(); }
    }
}

using System;
using System.ComponentModel;
using System.Windows;

namespace myManga_App.ViewModels
{
    public sealed class MainViewModel : DependencyObject, IDisposable
    {
        #region Content
        private HomeViewModel homeViewModel;
        public HomeViewModel HomeViewModel
        {
            get
            {
                return homeViewModel ?? (homeViewModel = new HomeViewModel());
            }
        }
        #endregion

        private App App = App.Current as App;

        public MainViewModel()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
                App.SiteExtensions.LoadDLL(App.PLUGIN_DIRECTORY, "*.mymanga.dll");
        }

        public void Dispose() { App.SiteExtensions.Unload(); }
    }
}

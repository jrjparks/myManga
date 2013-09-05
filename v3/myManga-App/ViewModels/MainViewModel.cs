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
    public sealed class MainViewModel : INotifyPropertyChanged, INotifyPropertyChanging, IDisposable
    {
        #region NotifyPropertyChange
        public event PropertyChangingEventHandler PropertyChanging;
        protected void OnPropertyChanging([CallerMemberName] String caller = "")
        {
            if (PropertyChanging != null)
                PropertyChanging(this, new PropertyChangingEventArgs(caller));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] String caller = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }
        #endregion

        public MainViewModel()
        {
            App app = (Application.Current as myManga_App.App);
            app.SiteExtensions.LoadDLL(app.PLUGIN_DIRECTORY);
        }

        public void Dispose()
        {

        }
    }
}

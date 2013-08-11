using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using BakaBox.DLL;
using IMangaSite;
using BakaBox;
using System.Windows.Controls;
using BakaBox.Net.Downloader;
using System.Text;
using System.Collections.ObjectModel;

namespace PluginV2Tester_Application
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        [DebuggerStepThrough]
        public void VerifyPropertyName(String propertyName)
        {
            // Verify that the property name matches a real,  
            // public, instance property on this object.
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                String msg = "Invalid property name: " + propertyName;

                if (this.ThrowOnInvalidPropertyName)
                    throw new Exception(msg);
                else
                    Debug.Fail(msg);
            }
        }
        protected virtual bool ThrowOnInvalidPropertyName { get; private set; }

        private Dictionary<Guid, IMangaSite.IMangaSite> PluginRequests
        {
            get;
            set;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(String Name)
        {
            this.VerifyPropertyName(Name);

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }

        private ObservableCollection<IMangaSiteDataAttribute> pluginInfoModelCollection;
        public ObservableCollection<IMangaSiteDataAttribute> PluginInfoModelCollection
        {
            get
            {
                if (pluginInfoModelCollection == null)
                    pluginInfoModelCollection = new ObservableCollection<IMangaSiteDataAttribute>();
                return pluginInfoModelCollection;
            }
        }

        private ObservableCollection<IMangaPluginManagerUpdate> iMangaPluginManagerUpdates;
        public ObservableCollection<IMangaPluginManagerUpdate> IMangaPluginManagerUpdates
        {
            get
            {
                if (iMangaPluginManagerUpdates == null)
                    iMangaPluginManagerUpdates = new ObservableCollection<IMangaPluginManagerUpdate>();
                return iMangaPluginManagerUpdates;
            }
        }
        public MainWindow()
        {
            PluginRequests = new Dictionary<Guid, IMangaSite.IMangaSite>();
            foreach (IMangaSite.IMangaSite plugin in Singleton<IMangaPluginManager>.Instance.PluginCollection)
            {
                PluginInfoModelCollection.Add(plugin.IMangaSiteData);
            }
            Singleton<IMangaPluginManager>.Instance.DownloadUpdate += DownloadUpdate;
            InitializeComponent();
        }

        void DownloadUpdate(object sender, IMangaPluginManagerUpdate e)
        {
            Boolean found = false;
            foreach (IMangaPluginManagerUpdate IMangaUpdate in IMangaPluginManagerUpdates)
                if (IMangaUpdate.Id.Equals(e.Id))
                {
                    found = true;
                    IMangaUpdate.Progress = e.Progress;
                    IMangaUpdate.Data = e.Data;
                    IMangaUpdate.Error = e.Error;
                    break;
                }
            if (!found)
                IMangaPluginManagerUpdates.Add(e);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button)
            {
                Button button = (sender as Button);
                Random rand = new Random((int)DateTime.Now.Ticks);
                switch (button.Content.ToString())
                {
                    default:
                        break;

                    case "Test Info Download":
                        Singleton<IMangaPluginManager>.Instance.PluginCollection[0].RequestInfo(String.Format("http://www.mangareader.net/actions/selector/?id={0}&which=0", rand.Next(2000) + 100));
                        break;

                    case "Test Info Download (RAND Count)":
                        int t = rand.Next(5);
                        for (int i = 0; i < t; ++i)
                            Singleton<IMangaPluginManager>.Instance.PluginCollection[0].RequestInfo(String.Format("http://www.mangareader.net/actions/selector/?id={0}&which=0", rand.Next(2000) + 100));
                        break;
                }
            }
        }
    }
}

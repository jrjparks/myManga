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

        private List<IMangaSiteDataAttribute> pluginInfoModelCollection;
        public List<IMangaSiteDataAttribute> PluginInfoModelCollection
        {
            get
            {
                if (pluginInfoModelCollection == null)
                    pluginInfoModelCollection = new List<IMangaSiteDataAttribute>();
                return pluginInfoModelCollection;
            }
            set { pluginInfoModelCollection = value; OnPropertyChanged("PluginInfoModelCollection"); }
        }
        public MainWindow()
        {
            PluginRequests = new Dictionary<Guid, IMangaSite.IMangaSite>();
            foreach (IMangaSite.IMangaSite plugin in Singleton<PluginManager<IMangaSite.IMangaSite, IMangaSiteCollection>>.Instance.PluginCollection)
            {
                PluginInfoModelCollection.Add(plugin.IMangaSiteData);
                plugin.DownloadRequested += DownloadRequested;
            }
            Singleton<Downloader>.Instance.DownloadUpdate += DownloadUpdate;
            InitializeComponent();
        }

        void DownloadUpdate(object sender, DownloadData e)
        {
            if (PluginRequests.ContainsKey(e.Id))
            {
                switch (e.State)
                {
                    default:
                        break;

                    case BakaBox.Tasks.State.Active:
                        break;

                    case BakaBox.Tasks.State.Completed:
                        PluginRequests[e.Id].ParseResponse(e.ResultStream);
                        PluginRequests.Remove(e.Id);
                        break;

                    case BakaBox.Tasks.State.CompletedWithError:
                        PluginRequests.Remove(e.Id);
                        break;

                    case BakaBox.Tasks.State.Pending:
                        break;
                }
            }
        }

        void DownloadRequested(object sender, DownloadRequest e)
        {
            DownloadData dd = new DownloadData()
            {
                Id = e.Id,
                RemoteURL = e.RemoteURL,
                WebEncoding = Encoding.UTF8
            };
            dd.WebHeaders.Add(System.Net.HttpRequestHeader.Referer, (sender as IMangaSite.IMangaSite).IMangaSiteData.RefererHeader);
            PluginRequests.Add(e.Id, (sender as IMangaSite.IMangaSite));
            Singleton<Downloader>.Instance.Download(dd);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button)
            {
                Button button = (sender as Button);
                switch (button.Content.ToString())
                {
                    default:
                        break;

                    case "Test Info Download":
                        Singleton<PluginManager<IMangaSite.IMangaSite, IMangaSiteCollection>>.Instance.PluginCollection[0].RequestInfo("http://www.mangareader.net/actions/selector/?id=4112&which=0");
                        break;

                    case "Test Chapter Download":
                        break;
                }
            }
        }
    }
}

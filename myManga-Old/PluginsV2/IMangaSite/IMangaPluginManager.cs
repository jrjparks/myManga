using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BakaBox.DLL;
using BakaBox.Controls;
using System.IO;
using BakaBox.Net.Downloader;
using System.ComponentModel;

namespace IMangaSite
{
    public class IMangaPluginManager : PluginManager<IMangaSite, IMangaSiteCollection>
    {
        #region Events
        public event EventHandler<IMangaPluginManagerUpdate> DownloadUpdate;
        protected virtual void OnDownloadUpdate(IMangaPluginManagerUpdate e)
        {
            if (DownloadUpdate != null)
            {
                if (SyncContext == null)
                    DownloadUpdate(this, e);
                else
                    foreach (EventHandler<IMangaPluginManagerUpdate> del in DownloadUpdate.GetInvocationList())
                        SyncContext.Post((s) => del(this, (IMangaPluginManagerUpdate)s), e);
            }
        }
        #endregion

        private Dictionary<String, WebClient> PluginWebClients { get; set; }
        private Dictionary<String, Queue<DownloadRequest>> PluginWebClientQueue { get; set; }
        private Dictionary<Guid, String> WebClientPluginLinks { get; set; }

        public IMangaPluginManager()
        {
            PluginWebClients = new Dictionary<String, WebClient>();
            PluginWebClientQueue = new Dictionary<String, Queue<DownloadRequest>>();
            WebClientPluginLinks = new Dictionary<Guid, String>();
        }

        public void LoadPlugin(String FilePath)
        {
            base.LoadPlugin(FilePath);
            PreloadPlugins();
        }

        public void LoadPluginDirectory(String Directory)
        {
            base.LoadPluginDirectory(Directory);
            PreloadPlugins();
        }

        protected void PreloadPlugins()
        {
            foreach (IMangaSite iMangaSite in PluginCollection)
                if (!PluginWebClients.ContainsKey(iMangaSite.IMangaSiteData.Name))
                {
                    PluginWebClients.Add(iMangaSite.IMangaSiteData.Name, new WebClient());
                    PluginWebClientQueue.Add(iMangaSite.IMangaSiteData.Name, new Queue<DownloadRequest>());
                    iMangaSite.DownloadRequested += DownloadRequested;

                    PluginWebClients[iMangaSite.IMangaSiteData.Name].Headers.Clear();
                    PluginWebClients[iMangaSite.IMangaSiteData.Name].Headers.Add(System.Net.HttpRequestHeader.Referer, iMangaSite.IMangaSiteData.RefererHeader);
                    PluginWebClients[iMangaSite.IMangaSiteData.Name].Encoding = Encoding.UTF8;

                    PluginWebClients[iMangaSite.IMangaSiteData.Name].DownloadDataCompleted += DownloadDataCompleted;
                    PluginWebClients[iMangaSite.IMangaSiteData.Name].DownloadProgressChanged += DownloadProgressChanged;
                }
        }

        void DownloadDataCompleted(object sender, System.Net.DownloadDataCompletedEventArgs e)
        {
            using (Stream dataStream = new MemoryStream(e.Result))
            {
                Object Data = PluginCollection[WebClientPluginLinks[(Guid)e.UserState]].ParseResponse(dataStream);
                OnDownloadUpdate(new IMangaPluginManagerUpdate() { Id = (Guid)e.UserState, Data = Data, Error = e.Error, Progress = 100 });
            }
            WebClientPluginLinks.Remove((Guid)e.UserState);
            CheckForDownloadSlot();
        }

        void DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e)
        {
            OnDownloadUpdate(new IMangaPluginManagerUpdate() { Id = (Guid)e.UserState, Progress = e.ProgressPercentage });
        }

        protected void DownloadRequested(object sender, DownloadRequest e)
        {
            IMangaSite iMangaSite = (sender as IMangaSite);
            WebClientPluginLinks.Add(e.Id, iMangaSite.IMangaSiteData.Name);
            PluginWebClientQueue[iMangaSite.IMangaSiteData.Name].Enqueue(e);
            OnDownloadUpdate(new IMangaPluginManagerUpdate() { Id = e.Id, Progress = 0, Data = e.RemoteURL });
            CheckForDownloadSlot();
        }

        protected void CheckForDownloadSlot()
        {
            foreach (String Key in PluginWebClients.Keys)
                if (PluginWebClientQueue[Key].Count > 0)
                    if (!PluginWebClients[Key].IsBusy)
                    {
                        DownloadRequest downloadRequest = PluginWebClientQueue[Key].Dequeue();
                        PluginWebClients[Key].DownloadDataAsync(new Uri(downloadRequest.RemoteURL), downloadRequest.Id);
                    }
        }
    }

    public class IMangaPluginManagerUpdate : EventArgs, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(String Name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }

        public IMangaPluginManagerUpdate()
        {
            Id = Guid.NewGuid();
            Data = null;
            Progress = 0;
        }

        private Guid id;
        public Guid Id
        {
            get { return id; }
            set { id = value; OnPropertyChanged("Id"); }
        }

        private Object data;
        public Object Data
        {
            get { return data; }
            set { data = value; OnPropertyChanged("Data"); }
        }

        private Int32 progress;
        public Int32 Progress
        {
            get { return progress; }
            set { progress = value; OnPropertyChanged("Progress"); }
        }

        private Exception error;
        public Exception Error
        {
            get { return error; }
            set { error = value; OnPropertyChanged("Error"); }
        }
    }
}

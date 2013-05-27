using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using BakaBox.Tasks;
using System.Collections.Specialized;

namespace BakaBox.Net.Downloader
{
    public class Downloader
    {
        #region Instance
        public Downloader()
        {
            SyncContext = SynchronizationContext.Current;
        }
        #endregion

        #region Events
        public event EventHandler<DownloadData> DownloadUpdate;
        protected virtual void OnDownloadUpdate(DownloadData e)
        {
            if (DownloadUpdate != null)
            {
                if (SyncContext == null)
                    DownloadUpdate(this, e);
                else
                    foreach (EventHandler<DownloadData> del in DownloadUpdate.GetInvocationList())
                        SyncContext.Post((s) => del(this, (DownloadData)s), e);
            }
        }
        #endregion

        #region Fields
        private SynchronizationContext SyncContext { get; set; }
        #endregion

        #region Members
        #region Private
        private void RunDownload(Object state)
        {
            if (state is DownloadData)
            {
                using (DownloaderWebClient downloaderWebClient = new DownloaderWebClient())
                {
                    DownloadData downloadData = state as DownloadData;

                    downloaderWebClient.Headers.Clear();
                    foreach (NameValueCollection header in downloadData.WebHeaders)
                        downloaderWebClient.Headers.Add(header);
                    downloaderWebClient.Encoding = downloadData.WebEncoding;

                    downloadData.State = State.Active;
                    OnDownloadUpdate(downloadData);
                    try
                    {
                        Byte[] data = downloaderWebClient.DownloadData(downloadData.RemoteURL);
                        downloadData.ResultStream = new MemoryStream(data);
                        downloadData.State = State.Completed;
                    }
                    catch (Exception ex)
                    {
                        downloadData.Error = ex;
                        downloadData.State = State.CompletedWithError;
                    }
                    OnDownloadUpdate(downloadData);
                }
            }
        }
        #endregion

        #region Public
        public Guid Download(String RemoteURL)
        {
            return Download(new DownloadData() { RemoteURL = RemoteURL });
        }
        public Guid Download(DownloadData DownloadData)
        {
            ThreadPool.QueueUserWorkItem(RunDownload, DownloadData);
            OnDownloadUpdate(DownloadData);
            return DownloadData.Id;
        }
        #endregion
        #endregion
    }
}

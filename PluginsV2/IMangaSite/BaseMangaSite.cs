using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using BakaBox.Tasks;
using System.IO;

namespace IMangaSite
{
    [DebuggerStepThrough]
    public class BaseMangaSite : IMangaSite
    {
        public event EventHandler<DownloadRequest> DownloadRequested;
        protected virtual void OnDownloadRequested(Object sender, DownloadRequest e)
        {
            if (DownloadRequested != null)
            {
                if (SyncContext == null)
                    DownloadRequested(this, e);
                else
                    foreach (EventHandler<DownloadRequest> del in DownloadRequested.GetInvocationList())
                        SyncContext.Post((s) => del(this, (DownloadRequest)s), e);
            }
        }

        public event EventHandler<FileData> FileIORequested;
        protected virtual void OnFileIORequested(Object sender, FileData e)
        {
            if (DownloadRequested != null)
            {
                if (SyncContext == null)
                    FileIORequested(this, e);
                else
                    foreach (EventHandler<FileData> del in FileIORequested.GetInvocationList())
                        SyncContext.Post((s) => del(this, (FileData)s), e);
            }
        }

        private SynchronizationContext SyncContext { get; set; }

        private Dictionary<Guid, Action<Stream>> requestCallbackLink;
        public Dictionary<Guid, Action<Stream>> RequestCallbackLink
        {
            get
            {
                if (requestCallbackLink != null)
                    requestCallbackLink = new Dictionary<Guid, Action<Stream>>();
                return requestCallbackLink;
            }
        }

        public BaseMangaSite()
        {
            SyncContext = SynchronizationContext.Current;
        }
    }
}

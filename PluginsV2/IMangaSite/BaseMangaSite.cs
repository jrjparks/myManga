using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using BakaBox.Tasks;
using System.IO;
using System.Reflection;

namespace IMangaSite
{
    [DebuggerStepThrough]
    public class BaseMangaSite
    {
        public event EventHandler InfoEvent;

        public event EventHandler ChapterListEvent;

        public event EventHandler ChapterImageListEvent;
        
        public event EventHandler<DownloadRequest> DownloadRequested;
        protected virtual void OnDownloadRequested(DownloadRequest e)
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
        protected virtual void OnFileIORequested(FileData e)
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

        protected SynchronizationContext SyncContext { get; set; }

        protected Dictionary<Guid, Action<Guid, Stream>> requestCallbackLinks;
        public Dictionary<Guid, Action<Guid, Stream>> RequestCallbackLinks
        {
            get
            {
                if (requestCallbackLinks != null)
                    requestCallbackLinks = new Dictionary<Guid, Action<Guid, Stream>>();
                return requestCallbackLinks;
            }
        }

        public BaseMangaSite()
        {
            SyncContext = SynchronizationContext.Current;
        }

        protected IMangaSiteDataAttribute iMangaSiteData { get; set; }
        public IMangaSiteDataAttribute IMangaSiteData
        {
            get
            {
                if (iMangaSiteData == null)
                {
                    MemberInfo Info = this.GetType();
                    Object[] Attribs = Info.GetCustomAttributes(typeof(IMangaSiteDataAttribute), true);
                    if (Attribs.Length > 0)
                        iMangaSiteData = Attribs[0] as IMangaSiteDataAttribute;
                }
                return iMangaSiteData;
            }
        }
    }
}

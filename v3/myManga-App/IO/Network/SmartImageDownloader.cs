using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Reflection;
using System.Windows.Documents;
using Amib.Threading;
using myMangaSiteExtension;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Collections;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;

namespace myManga_App.IO.Network
{
    public sealed class SmartImageDownloader : SmartDownloader
    {
        public sealed class SmartImageDownloadObject
        {
            public String URL { get; private set; }
            public String Referer { get; private set; }
            public String LocalPath { get; private set; }
            public String Filename { get; private set; }

            public Stream Stream { get; private set; }
            public Guid Id { get; private set; }

            public SmartImageDownloadObject(String url, String local_path, String referer = null, String filename = null)
            {
                this.Id = Guid.NewGuid();
                this.Stream = new MemoryStream();
                this.Filename = filename != null ? filename : Path.GetFileName(new Uri(url).LocalPath);

                this.URL = url;
                this.Referer = referer;
                this.LocalPath = local_path;
            }
        }

        public event EventHandler<SmartImageDownloadObject> SmartImageDownloadObjectComplete;
        private void OnSmartImageDownloadObjectComplete(SmartImageDownloadObject e)
        {
            if (SmartImageDownloadObjectComplete != null)
            {
                if (synchronizationContext == null)
                    SmartImageDownloadObjectComplete(this, e);
                else
                    foreach (EventHandler<SmartImageDownloadObject> del in SmartImageDownloadObjectComplete.GetInvocationList())
                        synchronizationContext.Post((s) => del(this, s as SmartImageDownloadObject), e);
            }
        }

        public SmartImageDownloader() : this(null) { }
        public SmartImageDownloader(STPStartInfo stpThredPool) : base(stpThredPool ?? new STPStartInfo() { MaxWorkerThreads = 5, ThreadPoolName = "SmartImageDownloader" }) { }

        public Guid Download(String url, String local_path, String referer = null)
        {
            if (referer == null)
                referer = url;
            SmartImageDownloadObject smart_image_download_object = new SmartImageDownloadObject(url, local_path, referer);
            this.smartThreadPool.QueueWorkItem(new WorkItemCallback(ImageObjectWorker), smart_image_download_object, new PostExecuteWorkItemCallback(ImageObjectWorkerCallback));
            return smart_image_download_object.Id;
        }

        private void ImageObjectWorkerCallback(IWorkItemResult wir)
        { OnSmartImageDownloadObjectComplete(wir.Result as SmartImageDownloadObject); }

        private object ImageObjectWorker(object state)
        { return ImageObjectWorker(state as SmartImageDownloadObject); }
        private SmartImageDownloadObject ImageObjectWorker(SmartImageDownloadObject smart_image_download_object)
        {
            using (Stream image_stream = base.GetRawContent(smart_image_download_object.URL, smart_image_download_object.Referer))
            {
                image_stream.Seek(0, SeekOrigin.Begin);
                image_stream.CopyTo(smart_image_download_object.Stream);
                smart_image_download_object.Stream.Seek(0, SeekOrigin.Begin);
            }
            return smart_image_download_object;
        }
    }
}

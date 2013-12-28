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
    public sealed class SmartMangaDownloader : SmartDownloader
    {
        public event EventHandler<MangaObject> MangaObjectComplete;
        protected void OnMangaObjectComplete(MangaObject e)
        {
            if (MangaObjectComplete != null)
            {
                if (synchronizationContext == null)
                    MangaObjectComplete(this, e);
                else
                    foreach (EventHandler<MangaObject> del in MangaObjectComplete.GetInvocationList())
                        synchronizationContext.Post((s) => del(this, s as MangaObject), e);
            }
        }

        public SmartMangaDownloader() : this(null) { }
        public SmartMangaDownloader(STPStartInfo stpThredPool) : base(stpThredPool ?? new STPStartInfo() { MaxWorkerThreads = 5, ThreadPoolName = "SmartMangaDownloader" }) { }

        public IWorkItemResult DownloadMangaObject(MangaObject mangaObject)
        { return smartThreadPool.QueueWorkItem(new WorkItemCallback(MangaObjectWorker), mangaObject, new PostExecuteWorkItemCallback(MangaObjectWorkerCallback)); }

        public ICollection<IWorkItemResult> DownloadMangaObject(ICollection<MangaObject> mangaObjects)
        { return (from mangaObject in mangaObjects select smartThreadPool.QueueWorkItem(new WorkItemCallback(MangaObjectWorker), mangaObject, new PostExecuteWorkItemCallback(MangaObjectWorkerCallback))).ToList(); }

        protected void MangaObjectWorkerCallback(IWorkItemResult wir)
        { OnMangaObjectComplete(wir.Result as MangaObject); }

        protected object MangaObjectWorker(object state)
        { return MangaObjectWorker(state as MangaObject); }
        protected MangaObject MangaObjectWorker(MangaObject mangaObject)
        {
            IWorkItemsGroup MangaObjectWig = smartThreadPool.CreateWorkItemsGroup(2);

            Dictionary<ISiteExtension, IWorkItemResult<String>> MangaObjectWorkItems = new Dictionary<ISiteExtension, IWorkItemResult<String>>();
            foreach (LocationObject LocationObj in mangaObject.Locations.FindAll(l => l.Enabled))
            {
                ISiteExtension ise = App.SiteExtensions.DLLCollection[LocationObj.ExtensionName];
                ISiteExtensionDescriptionAttribute isea = ise.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);
                if (isea.SupportedObjects.HasFlag(SupportedObjects.Search))
                    MangaObjectWorkItems.Add(ise, MangaObjectWig.QueueWorkItem<String, String, String>(GetHtmlContent, LocationObj.Url, isea.RefererHeader));
            }
            MangaObjectWig.WaitForIdle();
            foreach (KeyValuePair<ISiteExtension, IWorkItemResult<String>> val in MangaObjectWorkItems)
            {
                MangaObject dmObj = val.Key.ParseMangaObject(val.Value.Result);
                if (dmObj != null)
                    mangaObject.Merge(dmObj);
            }
            return mangaObject;
        }
    }
}

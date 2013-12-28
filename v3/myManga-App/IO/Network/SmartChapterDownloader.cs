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
    public sealed class SmartChapterDownloader : SmartDownloader
    {
        public event EventHandler<ChapterObject> ChapterObjectComplete;
        protected void OnChapterObjectComplete(ChapterObject e)
        {
            if (ChapterObjectComplete != null)
            {
                if (synchronizationContext == null)
                    ChapterObjectComplete(this, e);
                else
                    foreach (EventHandler<ChapterObject> del in ChapterObjectComplete.GetInvocationList())
                        synchronizationContext.Post((s) => del(this, s as ChapterObject), e);
            }
        }

        public SmartChapterDownloader() : this(null) { }
        public SmartChapterDownloader(STPStartInfo stpThredPool) : base(stpThredPool ?? new STPStartInfo() { MaxWorkerThreads = 5, ThreadPoolName = "SmartMangaDownloader" }) { }

        public IWorkItemResult DownloadChapterObject(ChapterObject chapterObject)
        { return smartThreadPool.QueueWorkItem(new WorkItemCallback(ChapterObjectWorker), chapterObject, new PostExecuteWorkItemCallback(ChapterObjectWorkerCallback)); }

        public ICollection<IWorkItemResult> DownloadChapterObject(ICollection<ChapterObject> chapterObjects)
        { return (from chapterObject in chapterObjects select smartThreadPool.QueueWorkItem(new WorkItemCallback(ChapterObjectWorker), chapterObject, new PostExecuteWorkItemCallback(ChapterObjectWorkerCallback))).ToList(); }

        protected void ChapterObjectWorkerCallback(IWorkItemResult wir)
        { OnChapterObjectComplete(wir.Result as ChapterObject); }

        protected object ChapterObjectWorker(object state)
        { return ChapterObjectWorker(state as ChapterObject); }
        protected ChapterObject ChapterObjectWorker(ChapterObject chapterObject)
        {
            IWorkItemsGroup ChapterObjectWig = smartThreadPool.CreateWorkItemsGroup(2);

            Dictionary<ISiteExtension, IWorkItemResult<String>> ChapterObjectWorkItems = new Dictionary<ISiteExtension, IWorkItemResult<String>>();
            foreach (LocationObject LocationObj in chapterObject.Locations.FindAll(l => l.Enabled))
            {
                ISiteExtension ise = App.SiteExtensions.DLLCollection[LocationObj.ExtensionName];
                ISiteExtensionDescriptionAttribute isea = ise.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);
                if (isea.SupportedObjects.HasFlag(SupportedObjects.Search))
                    ChapterObjectWorkItems.Add(ise, ChapterObjectWig.QueueWorkItem<String, String, String>(GetHtmlContent, LocationObj.Url, isea.RefererHeader));
            }
            ChapterObjectWig.WaitForIdle();
            foreach (KeyValuePair<ISiteExtension, IWorkItemResult<String>> val in ChapterObjectWorkItems)
            {
                ChapterObject dmObj = val.Key.ParseChapterObject(val.Value.Result);
                if (dmObj != null)
                    chapterObject.Merge(dmObj);
            }
            return chapterObject;
        }
    }
}

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
        private void OnChapterObjectComplete(ChapterObject e)
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
        { return (from chapterObject in chapterObjects select DownloadChapterObject(chapterObject)).ToList(); }

        private void ChapterObjectWorkerCallback(IWorkItemResult wir)
        { OnChapterObjectComplete(wir.Result as ChapterObject); }

        private object ChapterObjectWorker(object state)
        { return ChapterObjectWorker(state as ChapterObject); }
        private ChapterObject ChapterObjectWorker(ChapterObject chapterObject)
        {
            IWorkItemsGroup ChapterObjectWig = smartThreadPool.CreateWorkItemsGroup(2);
            ISiteExtension ise = null;
            LocationObject LocationObj = null;
            foreach (String ExtentionName in App.UserConfig.EnabledSiteExtentions)
            {
                LocationObj = chapterObject.Locations.FirstOrDefault((l) => l.ExtensionName == ExtentionName);
                if (LocationObj != null)
                {
                    ise = App.SiteExtensions.DLLCollection[LocationObj.ExtensionName];
                    break;
                }
            }
            if (ise == null)
            {
                LocationObj = chapterObject.Locations.First();
                ise = App.SiteExtensions.DLLCollection[LocationObj.ExtensionName];
            }
            ISiteExtensionDescriptionAttribute isea = ise.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);

            ChapterObject dcObj = ise.ParseChapterObject(GetHtmlContent(LocationObj.Url, isea.RefererHeader));
            chapterObject.Merge(dcObj);
            chapterObject.Pages = dcObj.Pages;

            return chapterObject;
        }
    }
}

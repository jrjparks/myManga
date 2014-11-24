using Amib.Threading;
using myMangaSiteExtension;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace myManga_App.IO.Network
{
    public sealed class SmartPageDownloader : SmartDownloader
    {
        public sealed class PageObjectCompleted
        {
            public ChapterObject ChapterObject { get; private set; }
            public PageObject PageObject { get; private set; }
            public ISiteExtension SiteExtension { get; private set; }

            public PageObjectCompleted(ChapterObject ChapterObject, PageObject PageObject, ISiteExtension SiteExtension = null)
            {
                this.ChapterObject = ChapterObject;
                this.PageObject = PageObject;
                this.SiteExtension = SiteExtension;
            }
        }

        public event EventHandler<PageObjectCompleted> PageObjectComplete;
        private void OnPageObjectComplete(PageObjectCompleted e)
        {
            if (PageObjectComplete != null)
            {
                if (synchronizationContext == null)
                    PageObjectComplete(this, e);
                else
                    foreach (EventHandler<PageObjectCompleted> del in PageObjectComplete.GetInvocationList())
                        synchronizationContext.Post((s) => del(this, s as PageObjectCompleted), e);
            }
        }

        public SmartPageDownloader() : this(null) { }
        public SmartPageDownloader(STPStartInfo stpThredPool) : base(stpThredPool ?? new STPStartInfo() { MaxWorkerThreads = 5, ThreadPoolName = "SmartPageDownloader" }) { }

        public ICollection<IWorkItemResult> DownloadPageObjectPages(ChapterObject ChapterObject)
        {
            String PageUrl = ChapterObject.Pages.First().Url;
            ISiteExtension ise = App.SiteExtensions.DLLCollection.First((_ise) => PageUrl.Contains(_ise.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false).URLFormat));
            return (from PageObject in ChapterObject.Pages select smartThreadPool.QueueWorkItem(new WorkItemCallback(PageObjectWorker), new PageObjectCompleted(ChapterObject, PageObject, ise), new PostExecuteWorkItemCallback(PageObjectWorkerCallback))).ToList();
        }

        public ICollection<ICollection<IWorkItemResult>> DownloadPageObjectPages(ICollection<ChapterObject> ChapterObjects)
        { return (from ChapterObject in ChapterObjects select DownloadPageObjectPages(ChapterObject)).ToList(); }

        private void PageObjectWorkerCallback(IWorkItemResult wir)
        { OnPageObjectComplete(wir.Result as PageObjectCompleted); }

        private object PageObjectWorker(object state)
        { return PageObjectWorker(state as PageObjectCompleted); }
        private object PageObjectWorker(PageObjectCompleted pageObjectCompleted)
        {
            PageObject dpObj = pageObjectCompleted.SiteExtension.ParsePageObject(base.GetHtmlContent(pageObjectCompleted.PageObject.Url, pageObjectCompleted.PageObject.Url));
            Int32 index = pageObjectCompleted.ChapterObject.Pages.FindIndex((po) => po.Url == dpObj.Url);
            pageObjectCompleted.ChapterObject.Pages[index] = dpObj;
            return new PageObjectCompleted(pageObjectCompleted.ChapterObject, dpObj);
        }
    }
}

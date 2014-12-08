using Amib.Threading;
using Core.IO;
using Core.IO.Storage.Manager.BaseInterfaceClasses;
using Core.Other.Singleton;
using myMangaSiteExtension;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace myManga_App.IO.Network
{
    public sealed class DownloadManager
    {
        /// <summary>
        /// This is the recommended use.
        /// </summary>
        public static DownloadManager Default
        { get { return Singleton<DownloadManager>.Instance; } }

        #region Classes
        private sealed class Downloader
        {
            public static Stream GetRawContent(String url, String referer = null)
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Referer = referer ?? request.Host;
                request.Method = "GET";
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                return GetResponse(request);
            }

            public static String GetHtmlContent(String url, String referer = null)
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Referer = referer ?? request.Host;
                request.Method = "GET";
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                return GetResponseString(request);
            }

            public static Stream GetResponse(HttpWebRequest request)
            {
                Stream content = new MemoryStream();
                try
                {
                    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                    {
                        using (Stream response_stream = response.GetResponseStream())
                        { response_stream.CopyTo(content); }
                    }
                }
                catch (WebException webEx)
                {
                    using (HttpWebResponse response = webEx.Response as HttpWebResponse)
                    {
                        using (Stream response_stream = response.GetResponseStream())
                        { response_stream.CopyTo(content); }
                    }
                }
                content.Seek(0, SeekOrigin.Begin);
                return content;
            }

            public static String GetResponseString(HttpWebRequest request)
            {
                String content = null;
                using (StreamReader streamReader = new StreamReader(GetResponse(request)))
                { content = streamReader.ReadToEnd(); }
                return HttpUtility.HtmlDecode(content);
            }
        }

        private abstract class WorkerClass<T, R>
            where T : class
            where R : class
        {
            protected readonly App App = App.Current as App;
            protected readonly SynchronizationContext SynchronizationContext;
            public event EventHandler<WorkerResult<R>> WorkComplete;
            private void OnWorkComplete(WorkerResult<R> e)
            {
                if (WorkComplete != null)
                {
                    if (SynchronizationContext == null) { WorkComplete(this, e); }
                    else { foreach (EventHandler<WorkerResult<R>> del in WorkComplete.GetInvocationList()) { SynchronizationContext.Post(_e => del(this, _e as WorkerResult<R>), e); } }
                }
            }

            public WorkerClass(SynchronizationContext SynchronizationContext) 
            { this.SynchronizationContext = SynchronizationContext ?? SynchronizationContext.Current ?? new SynchronizationContext(); }

            public IWorkItemResult RunWork(SmartThreadPool SmartThreadPool, T Value)
            { return SmartThreadPool.QueueWorkItem(new WorkItemCallback(WorkerMethod), Value, new PostExecuteWorkItemCallback(WorkerCallback)); }

            public IEnumerable<IWorkItemResult> RunWork(SmartThreadPool SmartThreadPool, IEnumerable<T> Values)
            {
                List<IWorkItemResult> Results = new List<IWorkItemResult>(Values.Count());
                foreach (T Value in Values)
                    Results.Add(SmartThreadPool.QueueWorkItem(new WorkItemCallback(WorkerMethod), Value, new PostExecuteWorkItemCallback(WorkerCallback)));
                return Results;
            }

            public IEnumerable<IWorkItemResult> RunWork(SmartThreadPool SmartThreadPool, params T[] Values)
            {
                List<IWorkItemResult> Results = new List<IWorkItemResult>(Values.Count());
                foreach (T Value in Values)
                    Results.Add(SmartThreadPool.QueueWorkItem(new WorkItemCallback(WorkerMethod), Value, new PostExecuteWorkItemCallback(WorkerCallback)));
                return Results;
            }

            private void WorkerCallback(IWorkItemResult WorkItemResult) { WorkerCallback(WorkItemResult.Result as WorkerResult<R>); }
            public virtual void WorkerCallback(WorkerResult<R> WorkItemResult) { OnWorkComplete(WorkItemResult); }

            private object WorkerMethod(object state) { return WorkerMethod(state as T); }
            public virtual WorkerResult<R> WorkerMethod(T Value) { return null; }
        }

        private sealed class WorkerItem<T> where T : class
        {
            public Guid Id { get; private set; }

            public T Data { get; private set; }

            public WorkerItem(T Data)
            {
                this.Id = Guid.NewGuid(); ;
                this.Data = Data;
            }
        }

        private sealed class WorkerResult<R> where R : class
        {
            public Guid Id { get; private set; }

            public Boolean Success { get; private set; }
            public Exception Exception { get; private set; }

            public R Result { get; private set; }

            public WorkerResult(R Result, Boolean Success = true, Exception Exception = null)
            {
                this.Id = Guid.NewGuid(); ;
                this.Success = Success;
                this.Exception = Exception;
                this.Result = Result;
            }
        }

        #region Worker Classes
        private class MangaObjectWorkerClass : WorkerClass<MangaObject, MangaObject>
        {
            public MangaObjectWorkerClass() : base(null) { }
            public MangaObjectWorkerClass(SynchronizationContext SynchronizationContext) : base(SynchronizationContext) { }

            public override WorkerResult<MangaObject> WorkerMethod(MangaObject Value)
            {
                Dictionary<ISiteExtension, String> SiteExtensionContent = new Dictionary<ISiteExtension, String>(Value.Locations.Count);
                foreach (LocationObject LocationObj in Value.Locations.FindAll(l => l.Enabled))
                {
                    ISiteExtension SiteExtension = App.SiteExtensions.DLLCollection[LocationObj.ExtensionName];
                    ISiteExtensionDescriptionAttribute SiteExtensionDescriptionAttribute = SiteExtension.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);
                    SiteExtensionContent.Add(SiteExtension, Downloader.GetHtmlContent(LocationObj.Url, SiteExtensionDescriptionAttribute.RefererHeader));
                }
                foreach (System.Collections.Generic.KeyValuePair<ISiteExtension, String> Content in SiteExtensionContent)
                {
                    try
                    {
                        MangaObject DownloadedMangaObject = Content.Key.ParseMangaObject(Content.Value);
                        if (DownloadedMangaObject != null) Value.Merge(DownloadedMangaObject);
                    }
                    catch (Exception ex) { return new WorkerResult<MangaObject>(Value, false, ex); }
                }
                return new WorkerResult<MangaObject>(Value);
            }
        }

        public sealed class ChapterObjectDownloadRequest
        {
            public MangaObject MangaObject { get; private set; }
            public ChapterObject ChapterObject { get; private set; }
            public ChapterObjectDownloadRequest(MangaObject MangaObject, ChapterObject ChapterObject)
            { this.MangaObject = MangaObject; this.ChapterObject = ChapterObject; }
        }
        private class ChapterObjectWorkerClass : WorkerClass<ChapterObjectDownloadRequest, ChapterObjectDownloadRequest>
        {
            public ChapterObjectWorkerClass() : base(null) { }
            public ChapterObjectWorkerClass(SynchronizationContext SynchronizationContext) : base(SynchronizationContext) { }

            public override WorkerResult<ChapterObjectDownloadRequest> WorkerMethod(ChapterObjectDownloadRequest Value)
            {
                try
                {
                    ISiteExtension SiteExtension = null;
                    LocationObject LocationObj = null;
                    foreach (String ExtentionName in App.UserConfig.EnabledSiteExtensions)
                    {
                        LocationObj = Value.ChapterObject.Locations.FirstOrDefault((l) => l.ExtensionName == ExtentionName);
                        if (LocationObj != null)
                        { SiteExtension = App.SiteExtensions.DLLCollection[LocationObj.ExtensionName]; break; }
                    }
                    if (SiteExtension == null)
                    {
                        LocationObj = Value.ChapterObject.Locations.First();
                        SiteExtension = App.SiteExtensions.DLLCollection[LocationObj.ExtensionName];
                    }
                    ISiteExtensionDescriptionAttribute SiteExtensionDescriptionAttribute = SiteExtension.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);

                    ChapterObject DownloadedChapterObject = SiteExtension.ParseChapterObject(Downloader.GetHtmlContent(LocationObj.Url, SiteExtensionDescriptionAttribute.RefererHeader));
                    Value.ChapterObject.Merge(DownloadedChapterObject);
                    Value.ChapterObject.Pages = DownloadedChapterObject.Pages;
                }
                catch (Exception ex) { return new WorkerResult<ChapterObjectDownloadRequest>(Value, false, ex); }
                return new WorkerResult<ChapterObjectDownloadRequest>(Value);
            }
        }

        public sealed class PageObjectDownloadRequest
        {
            public MangaObject MangaObject { get; private set; }
            public ChapterObject ChapterObject { get; private set; }
            public PageObject PageObject { get; private set; }
            public PageObjectDownloadRequest(MangaObject MangaObject, ChapterObject ChapterObject, PageObject PageObject)
            { this.MangaObject = MangaObject; this.ChapterObject = ChapterObject; this.PageObject = PageObject; }
        }
        private class PageObjectWorkerClass : WorkerClass<PageObjectDownloadRequest, PageObjectDownloadRequest>
        {
            public PageObjectWorkerClass() : base(null) { }
            public PageObjectWorkerClass(SynchronizationContext SynchronizationContext) : base(SynchronizationContext) { }

            public override WorkerResult<PageObjectDownloadRequest> WorkerMethod(PageObjectDownloadRequest Value)
            {
                try
                {
                    ISiteExtension SiteExtension = App.SiteExtensions.DLLCollection.First(_SiteExtension => Value.PageObject.Url.Contains(_SiteExtension.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false).URLFormat));
                    PageObject DownloadedPageObject = SiteExtension.ParsePageObject(Downloader.GetHtmlContent(Value.PageObject.Url, Value.PageObject.Url));
                    Int32 index = Value.ChapterObject.Pages.FindIndex((po) => po.Url == DownloadedPageObject.Url);
                    Value.ChapterObject.Pages[index] = DownloadedPageObject;
                    return new WorkerResult<PageObjectDownloadRequest>(new PageObjectDownloadRequest(Value.MangaObject, Value.ChapterObject, DownloadedPageObject));
                }
                catch (Exception ex) { return new WorkerResult<PageObjectDownloadRequest>(Value, false, ex); }
            }
        }

        public sealed class ImageDownloadRequest
        {
            public String URL { get; private set; }
            public String Referer { get; private set; }
            public String LocalPath { get; private set; }
            public String Filename { get; private set; }

            public Stream Stream { get; private set; }

            public ImageDownloadRequest(String url, String local_path, String referer = null, String filename = null)
            {
                this.Stream = new MemoryStream();
                this.Filename = filename != null ? filename : Path.GetFileName(new Uri(url).LocalPath);     // Use the LocalPath from the url if filename is null

                this.URL = url;
                this.Referer = referer ?? url;                                                              // Use the URL if referer in null
                this.LocalPath = local_path;
            }
        }
        private class ImageWorkerClass : WorkerClass<ImageDownloadRequest, ImageDownloadRequest>
        {
            public ImageWorkerClass() : base(null) { }
            public ImageWorkerClass(SynchronizationContext SynchronizationContext) : base(SynchronizationContext) { }

            public override WorkerResult<ImageDownloadRequest> WorkerMethod(ImageDownloadRequest Value)
            {
                try
                {
                    using (Stream image_stream = Downloader.GetRawContent(Value.URL, Value.Referer))
                    {
                        image_stream.Seek(0, SeekOrigin.Begin);
                        image_stream.CopyTo(Value.Stream);
                        Value.Stream.Seek(0, SeekOrigin.Begin);
                    }
                }
                catch (Exception ex) { return new WorkerResult<ImageDownloadRequest>(Value, false, ex); }
                return new WorkerResult<ImageDownloadRequest>(Value);
            }
        }
        #endregion
        #endregion

        #region Events
        public event EventHandler StatusChange;
        private void OnStatusChange(EventArgs e)
        {
            if (StatusChange != null)
            {
                if (SynchronizationContext == null) { StatusChange(this, e); }
                else { foreach (EventHandler del in StatusChange.GetInvocationList()) { SynchronizationContext.Post(_e => del(this, _e as EventArgs), e); } }
            }
        }
        #endregion

        private readonly SmartThreadPool SmartThreadPool;
        private readonly SynchronizationContext SynchronizationContext;
        private readonly App App = App.Current as App;
        public Int32 Concurrency { get { return SmartThreadPool.Concurrency; } }
        public Boolean IsIdle { get { return SmartThreadPool.IsIdle; } }

        private readonly MangaObjectWorkerClass MangaObjectWorker;
        private readonly ChapterObjectWorkerClass ChapterObjectWorker;
        private readonly PageObjectWorkerClass PageObjectWorker;
        private readonly ImageWorkerClass ImageWorker;

        public DownloadManager() : this(null) { }
        public DownloadManager(STPStartInfo STPStartInfo, SynchronizationContext SynchronizationContext = null)
        {
            this.SmartThreadPool = new SmartThreadPool(STPStartInfo ?? new STPStartInfo()
            {   // Default STPStartInfo

                // Default name: DownloadManager-00000000-0000-0000-0000-000000000000
                ThreadPoolName = String.Format("{0}-{1}", this.GetType().Name, Guid.NewGuid()),

                // Default MaxWorkerThreads: System ProcessorCount * 2
                MaxWorkerThreads = Environment.ProcessorCount * 2,

                // We're not in a rush here for system resources.
                ThreadPriority = ThreadPriority.BelowNormal
            });

            this.SynchronizationContext = SynchronizationContext ?? SynchronizationContext.Current;

            this.MangaObjectWorker = new MangaObjectWorkerClass(this.SynchronizationContext);
            this.ChapterObjectWorker = new ChapterObjectWorkerClass(this.SynchronizationContext);
            this.PageObjectWorker = new PageObjectWorkerClass(this.SynchronizationContext);
            this.ImageWorker = new ImageWorkerClass(this.SynchronizationContext);

            this.MangaObjectWorker.WorkComplete += MangaObjectWorker_WorkComplete;
            this.ChapterObjectWorker.WorkComplete += ChapterObjectWorker_WorkComplete;
            this.PageObjectWorker.WorkComplete += PageObjectWorker_WorkComplete;
            this.ImageWorker.WorkComplete += ImageWorker_WorkComplete;
        }

        #region Download Methods
        public void Download(MangaObject MangaObject)
        { MangaObjectWorker.RunWork(SmartThreadPool, MangaObject); OnStatusChange(null); }
        public void Download(MangaObject MangaObject, ChapterObject ChapterObject)
        { ChapterObjectWorker.RunWork(SmartThreadPool, new ChapterObjectDownloadRequest(MangaObject, ChapterObject)); OnStatusChange(null); }
        public void Download(MangaObject MangaObject, ChapterObject ChapterObject, PageObject PageObject)
        { PageObjectWorker.RunWork(SmartThreadPool, new PageObjectDownloadRequest(MangaObject, ChapterObject, PageObject)); OnStatusChange(null); }
        public void Download(String url, String local_path, String referer = null, String filename = null)
        { ImageWorker.RunWork(SmartThreadPool, new ImageDownloadRequest(url, local_path, referer, filename)); OnStatusChange(null); }
        #endregion

        #region Event Handlers
        private void MangaObjectWorker_WorkComplete(object sender, WorkerResult<MangaObject> e)
        {
            if (e.Success)
            {
                String save_path = Path.Combine(App.MANGA_ARCHIVE_DIRECTORY, e.Result.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION));
                Singleton<ZipStorage>.Instance.Write(save_path, e.Result.GetType().Name, e.Result.Serialize(SaveType: App.UserConfig.SaveType));
                ImageWorker.RunWork(SmartThreadPool, from String url in e.Result.Covers select new ImageDownloadRequest(url, save_path));
            }
            OnStatusChange(null);
        }

        private void ChapterObjectWorker_WorkComplete(object sender, WorkerResult<DownloadManager.ChapterObjectDownloadRequest> e)
        {
            if (e.Success)
            {
                String save_path = Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, e.Result.MangaObject.MangaFileName(), e.Result.ChapterObject.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION));
                Singleton<ZipStorage>.Instance.Write(save_path, e.Result.ChapterObject.GetType().Name, e.Result.ChapterObject.Serialize(SaveType: App.UserConfig.SaveType));
                PageObjectWorker.RunWork(SmartThreadPool, from PageObject page_object in e.Result.ChapterObject.Pages select new PageObjectDownloadRequest(e.Result.MangaObject, e.Result.ChapterObject, page_object));
            }
            OnStatusChange(null);
        }

        private void PageObjectWorker_WorkComplete(object sender, WorkerResult<DownloadManager.PageObjectDownloadRequest> e)
        {
            if (e.Success)
            {
                String save_path = Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, e.Result.MangaObject.MangaFileName(), e.Result.ChapterObject.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION));
                Singleton<ZipStorage>.Instance.Write(save_path, e.Result.ChapterObject.GetType().Name, e.Result.ChapterObject.Serialize(SaveType: App.UserConfig.SaveType));
                ImageWorker.RunWork(SmartThreadPool, new ImageDownloadRequest(e.Result.PageObject.ImgUrl, save_path));
            }
            OnStatusChange(null);
        }

        private void ImageWorker_WorkComplete(object sender, WorkerResult<DownloadManager.ImageDownloadRequest> e)
        {
            if (e.Success) { using (e.Result.Stream) { Singleton<ZipStorage>.Instance.Write(e.Result.LocalPath, e.Result.Filename, e.Result.Stream); } }
            OnStatusChange(null);
        }
        #endregion
    }
}
using Amib.Threading;
using Core.IO;
using Core.IO.Storage.Manager.BaseInterfaceClasses;
using Core.Other.Singleton;
using Core.MVVM;
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
using System.Text.RegularExpressions;
using System.Threading;
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
            private readonly SynchronizationContext SynchronizationContext;
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

            public IWorkItemResult RunWork(SmartThreadPool SmartThreadPool, T Value, Guid? Id = null, params Core.IO.KeyValuePair<String, Object>[] Args)
            { return SmartThreadPool.QueueWorkItem(new WorkItemCallback(WorkerMethod), new WorkerItem<T>(Value, Id, Args), new PostExecuteWorkItemCallback(WorkerCallback)); }

            public IEnumerable<IWorkItemResult> RunWork(SmartThreadPool SmartThreadPool, IEnumerable<T> Values, IEnumerable<Guid?> Ids = null, params Core.IO.KeyValuePair<String, Object>[] Args)
            {
                List<IWorkItemResult> Results = new List<IWorkItemResult>(Values.Count());
                List<Guid?> ResultIds = new List<Guid?>(Values.Count());
                if (Ids != null)
                    ResultIds.AddRange(Ids);
                if (ResultIds.Count < Values.Count())
                    ResultIds.AddRange(Enumerable.Repeat<Guid?>(null, Values.Count() - ResultIds.Count));
                Int32 value_index = 0;
                foreach (T Value in Values)
                    Results.Add(SmartThreadPool.QueueWorkItem(new WorkItemCallback(WorkerMethod), new WorkerItem<T>(Value, ResultIds[value_index++], Args), new PostExecuteWorkItemCallback(WorkerCallback)));
                return Results.AsEnumerable();
            }

            /*//
            public IEnumerable<IWorkItemResult> RunWork(SmartThreadPool SmartThreadPool, params T[] Values)
            { return RunWork(SmartThreadPool: SmartThreadPool, Values: Values.AsEnumerable()); }
            //*/

            private void WorkerCallback(IWorkItemResult WorkItemResult) { WorkerCallback(WorkItemResult.Result as WorkerResult<R>); }
            public virtual void WorkerCallback(WorkerResult<R> WorkItemResult) { OnWorkComplete(WorkItemResult); }

            private object WorkerMethod(object state) { return WorkerMethod(state as WorkerItem<T>); }
            public virtual WorkerResult<R> WorkerMethod(WorkerItem<T> Value) { return null; }
        }

        private sealed class WorkerItem<T> where T : class
        {
            public Guid Id { get; private set; }

            public T Data { get; private set; }

            public Core.IO.KeyValuePair<String, Object>[] Args { get; private set; }

            public WorkerItem(T Data, Guid? Id = null, params Core.IO.KeyValuePair<String, Object>[] Args)
            {
                this.Id = Id ?? Guid.NewGuid(); ;
                this.Data = Data;
                this.Args = Args;
            }
        }

        private sealed class WorkerResult<R> where R : class
        {
            public Guid Id { get; private set; }

            public Boolean Success { get; private set; }
            public Exception Exception { get; private set; }

            public Core.IO.KeyValuePair<String, Object>[] Args { get; private set; }

            public R Result { get; private set; }

            public WorkerResult(R Result, Boolean Success = true, Exception Exception = null, Guid? Id = null, params Core.IO.KeyValuePair<String, Object>[] Args)
            {
                this.Id = Id ?? Guid.NewGuid(); ;
                this.Success = Success;
                this.Exception = Exception;
                this.Result = Result;
                this.Args = Args;
            }
        }

        #region Item Worker Classes
        private class MangaObjectWorkerClass : WorkerClass<MangaObject, MangaObject>
        {
            public MangaObjectWorkerClass() : base(null) { }
            public MangaObjectWorkerClass(SynchronizationContext SynchronizationContext) : base(SynchronizationContext) { }

            public override WorkerResult<MangaObject> WorkerMethod(WorkerItem<MangaObject> Value)
            {
                Dictionary<ISiteExtension, String> SiteExtensionContent = new Dictionary<ISiteExtension, String>(Value.Data.Locations.Count);
                foreach (LocationObject LocationObj in Value.Data.Locations.FindAll(l => l.Enabled))
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
                        if (!MangaObject.Equals(DownloadedMangaObject, null)) Value.Data.Merge(DownloadedMangaObject);
                    }
                    catch { }
                }
                return new WorkerResult<MangaObject>(Result: Value.Data, Id: Value.Id, Args: Value.Args);
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

            public override WorkerResult<ChapterObjectDownloadRequest> WorkerMethod(WorkerItem<ChapterObjectDownloadRequest> Value)
            {
                try
                {
                    ISiteExtension SiteExtension = null;
                    LocationObject LocationObj = null;
                    foreach (String ExtentionName in App.UserConfig.EnabledSiteExtensions)
                    {
                        LocationObj = Value.Data.ChapterObject.Locations.FirstOrDefault((l) => l.ExtensionName == ExtentionName);
                        if (LocationObj != null)
                        { SiteExtension = App.SiteExtensions.DLLCollection[LocationObj.ExtensionName]; break; }
                    }
                    if (SiteExtension == null)
                    {
                        LocationObj = Value.Data.ChapterObject.Locations.First();
                        SiteExtension = App.SiteExtensions.DLLCollection[LocationObj.ExtensionName];
                    }
                    ISiteExtensionDescriptionAttribute SiteExtensionDescriptionAttribute = SiteExtension.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);

                    ChapterObject DownloadedChapterObject = SiteExtension.ParseChapterObject(Downloader.GetHtmlContent(LocationObj.Url, SiteExtensionDescriptionAttribute.RefererHeader));
                    Value.Data.ChapterObject.Merge(DownloadedChapterObject);
                    Value.Data.ChapterObject.Pages = DownloadedChapterObject.Pages;
                }
                catch (Exception ex) { return new WorkerResult<ChapterObjectDownloadRequest>(Result: Value.Data, Id: Value.Id, Args: Value.Args, Success: false, Exception: ex); }
                return new WorkerResult<ChapterObjectDownloadRequest>(Result: Value.Data, Id: Value.Id, Args: Value.Args);
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

            public override WorkerResult<PageObjectDownloadRequest> WorkerMethod(WorkerItem<PageObjectDownloadRequest> Value)
            {
                try
                {
                    ISiteExtension SiteExtension = App.SiteExtensions.DLLCollection.First(_SiteExtension => Value.Data.PageObject.Url.Contains(_SiteExtension.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false).URLFormat));
                    PageObject DownloadedPageObject = SiteExtension.ParsePageObject(Downloader.GetHtmlContent(Value.Data.PageObject.Url, Value.Data.PageObject.Url));
                    Int32 index = Value.Data.ChapterObject.Pages.FindIndex((po) => po.Url == DownloadedPageObject.Url);
                    Value.Data.ChapterObject.Pages[index] = DownloadedPageObject;
                    return new WorkerResult<PageObjectDownloadRequest>(new PageObjectDownloadRequest(Value.Data.MangaObject, Value.Data.ChapterObject, DownloadedPageObject));
                }
                catch (Exception ex) { return new WorkerResult<PageObjectDownloadRequest>(Result: Value.Data, Id: Value.Id, Args: Value.Args, Success: false, Exception: ex); }
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

            public override WorkerResult<ImageDownloadRequest> WorkerMethod(WorkerItem<ImageDownloadRequest> Value)
            {
                try
                {
                    using (Stream image_stream = Downloader.GetRawContent(Value.Data.URL, Value.Data.Referer))
                    {
                        image_stream.Seek(0, SeekOrigin.Begin);
                        image_stream.CopyTo(Value.Data.Stream);
                        Value.Data.Stream.Seek(0, SeekOrigin.Begin);
                    }
                }
                catch (Exception ex) { return new WorkerResult<ImageDownloadRequest>(Result: Value.Data, Id: Value.Id, Args: Value.Args, Success: false, Exception: ex); }
                return new WorkerResult<ImageDownloadRequest>(Result: Value.Data, Id: Value.Id, Args: Value.Args);
            }
        }
        #endregion

        #region Search Worker Classes
        private sealed class SearchWorkerClass : WorkerClass<String, List<MangaObject>>
        {
            private readonly Regex SafeAlphaNumeric = new Regex("[^a-z0-9]", RegexOptions.IgnoreCase);

            public SearchWorkerClass() : base(null) { }
            public SearchWorkerClass(SynchronizationContext SynchronizationContext) : base(SynchronizationContext) { }

            public override WorkerResult<List<MangaObject>> WorkerMethod(WorkerItem<String> Value)
            {
                List<MangaObject> SearchResultItems = new List<MangaObject>();

                Dictionary<ISiteExtension, String> SiteExtensionSearchContents = new Dictionary<ISiteExtension, String>(App.UserConfig.EnabledSiteExtensions.Count);
                Dictionary<IDatabaseExtension, String> DatabaseExtensionSearchContents = new Dictionary<IDatabaseExtension, String>(App.UserConfig.EnabledDatabaseExtentions.Count);

                // Search Enabled SiteExtensions for searchTerm: Value.Data
                foreach (ISiteExtension SiteExtension in App.SiteExtensions.DLLCollection.Where(SiteExtension =>
                {
                    if (!SiteExtension.SiteExtensionDescriptionAttribute.SupportedObjects.HasFlag(SupportedObjects.Search)) return false;
                    if (!App.UserConfig.EnabledSiteExtensions.Contains(SiteExtension.SiteExtensionDescriptionAttribute.Name)) return false;
                    return true;
                }))
                { SiteExtensionSearchContents.Add(SiteExtension, ProcessSearchRequest(SiteExtension.GetSearchRequestObject(searchTerm: Value.Data))); }

                // Search Enabled DatabaseExtensions for searchTerm: Value.Data
                foreach (IDatabaseExtension DatabaseExtension in App.DatabaseExtensions.DLLCollection.Where(DatabaseExtension =>
                {
                    if (!DatabaseExtension.DatabaseExtensionDescriptionAttribute.SupportedObjects.HasFlag(SupportedObjects.Search)) return false;
                    if (!App.UserConfig.EnabledDatabaseExtentions.Contains(DatabaseExtension.DatabaseExtensionDescriptionAttribute.Name)) return false;
                    return true;
                }))
                { DatabaseExtensionSearchContents.Add(DatabaseExtension, ProcessSearchRequest(DatabaseExtension.GetSearchRequestObject(searchTerm: Value.Data))); }

                // Convert SearchResultObjects from SiteExtensions to MangaObjects and add/merge into SearchResultItems
                foreach (System.Collections.Generic.KeyValuePair<ISiteExtension, String> SiteExtensionSearchContent in SiteExtensionSearchContents)
                {
                    try
                    {
                        foreach (SearchResultObject SearchResult in SiteExtensionSearchContent.Key.ParseSearch(SiteExtensionSearchContent.Value))
                        {
                            MangaObject manga_object = SearchResult.ConvertToMangaObject(),
                                existing_manga_object = SearchResultItems.FirstOrDefault(
                                mo => SafeAlphaNumeric.Replace(mo.Name.ToLower(), String.Empty).Equals(SafeAlphaNumeric.Replace(manga_object.Name.ToLower(), String.Empty)));

                            // Add new manga_object or merge into existing_manga_object
                            if (MangaObject.Equals(existing_manga_object, null)) SearchResultItems.Add(manga_object);
                            else existing_manga_object.Merge(manga_object);
                        }
                    }
                    catch { }
                }

                // Attach SearchResultObjects from DatabaseExtensions to MangaObjects in SearchResultItems
                foreach (System.Collections.Generic.KeyValuePair<IDatabaseExtension, String> DatabaseExtensionSearchContent in DatabaseExtensionSearchContents)
                {
                    try
                    {
                        foreach (DatabaseObject SearchResult in DatabaseExtensionSearchContent.Key.ParseSearch(DatabaseExtensionSearchContent.Value))
                        {
                            MangaObject existing_manga_object = SearchResultItems.FirstOrDefault(
                                mo => SafeAlphaNumeric.Replace(mo.Name.ToLower(), String.Empty).Equals(SafeAlphaNumeric.Replace(SearchResult.Name.ToLower(), String.Empty)));
                            if (!MangaObject.Equals(existing_manga_object, null))
                            { existing_manga_object.AttachDatabase(SearchResult); }
                        }
                    }
                    catch { }
                }

                return new WorkerResult<List<MangaObject>>(SearchResultItems, Id: Value.Id);
            }

            private String ProcessSearchRequest(SearchRequestObject RequestObject)
            {
                HttpWebRequest request = WebRequest.Create(RequestObject.Url) as HttpWebRequest;
                request.Referer = RequestObject.Referer ?? request.Host;
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                switch (RequestObject.Method)
                {
                    default:
                    case myMangaSiteExtension.Enums.SearchMethod.GET:
                        request.Method = "GET";
                        break;

                    case myMangaSiteExtension.Enums.SearchMethod.POST:
                        request.Method = "POST";
                        request.ContentType = "application/x-www-form-urlencoded";
                        using (var requestWriter = new StreamWriter(request.GetRequestStream()))
                        { requestWriter.Write(RequestObject.RequestContent); }
                        break;
                }
                return Downloader.GetResponseString(request);
            }
        }
        #endregion
        #endregion

        #region Events
        public event EventHandler<Exception> StatusChange;
        private void OnStatusChange(Exception e = null)
        {
            if (StatusChange != null)
            {
                if (SynchronizationContext == null) { StatusChange(this, e); }
                else { foreach (EventHandler<Exception> del in StatusChange.GetInvocationList()) { SynchronizationContext.Post(_e => del(this, _e as Exception), e); } }
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
        private readonly SearchWorkerClass SearchWorker;

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
            this.SearchWorker = new SearchWorkerClass(this.SynchronizationContext);

            this.MangaObjectWorker.WorkComplete += MangaObjectWorker_WorkComplete;
            this.ChapterObjectWorker.WorkComplete += ChapterObjectWorker_WorkComplete;
            this.PageObjectWorker.WorkComplete += PageObjectWorker_WorkComplete;
            this.ImageWorker.WorkComplete += ImageWorker_WorkComplete;
            this.SearchWorker.WorkComplete += SearchWorker_WorkComplete;
        }

        #region Download Methods
        public void Download(MangaObject MangaObject, params Core.IO.KeyValuePair<String, Object>[] Args)
        { MangaObjectWorker.RunWork(SmartThreadPool, MangaObject, Args: Args); OnStatusChange(); }
        public void Download(MangaObject MangaObject, ChapterObject ChapterObject, params Core.IO.KeyValuePair<String, Object>[] Args)
        { ChapterObjectWorker.RunWork(SmartThreadPool, new ChapterObjectDownloadRequest(MangaObject, ChapterObject), Args: Args); OnStatusChange(); }
        public void Download(MangaObject MangaObject, ChapterObject ChapterObject, PageObject PageObject, params Core.IO.KeyValuePair<String, Object>[] Args)
        { PageObjectWorker.RunWork(SmartThreadPool, new PageObjectDownloadRequest(MangaObject, ChapterObject, PageObject), Args: Args); OnStatusChange(); }
        public void Download(String url, String local_path, String referer = null, String filename = null, params Core.IO.KeyValuePair<String, Object>[] Args)
        { ImageWorker.RunWork(SmartThreadPool, new ImageDownloadRequest(url, local_path, referer, filename), Args: Args); OnStatusChange(); }
        #endregion

        #region Search Methods
        public Guid Search(String SearchTerm)
        {
            WorkerItem<String> SearchItem = new WorkerItem<String>(SearchTerm);
            SearchWorker.RunWork(SmartThreadPool, SearchItem.Data, SearchItem.Id, SearchItem.Args); OnStatusChange();
            return SearchItem.Id;
        }
        #endregion

        #region Event Handlers
        private void MangaObjectWorker_WorkComplete(object sender, DownloadManager.WorkerResult<MangaObject> e)
        {
            if (e.Success)
            {
                String save_path = Path.Combine(App.MANGA_ARCHIVE_DIRECTORY, e.Result.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION));
                Singleton<ZipStorage>.Instance.Write(save_path, e.Result.GetType().Name, e.Result.Serialize(SaveType: App.UserConfig.SaveType));
                Core.IO.KeyValuePair<String, Object> IsRefresh = e.Args.FirstOrDefault(x => x.Key.Equals("IsRefresh"));
                if (IsRefresh != null && (IsRefresh.Value is Boolean && (Boolean)IsRefresh.Value == false))
                    ImageWorker.RunWork(SmartThreadPool, (from String url in e.Result.Covers select new ImageDownloadRequest(url, save_path)).AsEnumerable());
            }
            OnStatusChange(e.Exception);
        }

        private void ChapterObjectWorker_WorkComplete(object sender, DownloadManager.WorkerResult<DownloadManager.ChapterObjectDownloadRequest> e)
        {
            if (e.Success)
            {
                String save_path = Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, e.Result.MangaObject.MangaFileName(), e.Result.ChapterObject.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION));
                Singleton<ZipStorage>.Instance.Write(save_path, e.Result.ChapterObject.GetType().Name, e.Result.ChapterObject.Serialize(SaveType: App.UserConfig.SaveType));
                PageObjectWorker.RunWork(SmartThreadPool, from PageObject page_object in e.Result.ChapterObject.Pages select new PageObjectDownloadRequest(e.Result.MangaObject, e.Result.ChapterObject, page_object));
            }
            OnStatusChange(e.Exception);
        }

        private void PageObjectWorker_WorkComplete(object sender, DownloadManager.WorkerResult<DownloadManager.PageObjectDownloadRequest> e)
        {
            if (e.Success)
            {
                String save_path = Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, e.Result.MangaObject.MangaFileName(), e.Result.ChapterObject.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION));
                Singleton<ZipStorage>.Instance.Write(save_path, e.Result.ChapterObject.GetType().Name, e.Result.ChapterObject.Serialize(SaveType: App.UserConfig.SaveType));
                ImageWorker.RunWork(SmartThreadPool, new ImageDownloadRequest(e.Result.PageObject.ImgUrl, save_path));
            }
            OnStatusChange(e.Exception);
        }

        private void ImageWorker_WorkComplete(object sender, DownloadManager.WorkerResult<DownloadManager.ImageDownloadRequest> e)
        {
            if (e.Success) { using (e.Result.Stream) { Singleton<ZipStorage>.Instance.Write(e.Result.LocalPath, e.Result.Filename, e.Result.Stream); } }
            OnStatusChange(e.Exception);
        }

        private void SearchWorker_WorkComplete(object sender, DownloadManager.WorkerResult<List<MangaObject>> e)
        {
            Messenger.Default.Send(e.Result, String.Format("SearchResult-{0}", e.Id.ToString()));
            OnStatusChange(e.Exception);
        }
        #endregion
    }
}
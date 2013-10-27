using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Amib.Threading;
using myMangaSiteExtension;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;

namespace myManga_App.IO.Network
{
    public class SmartSearch : SmartDownloader
    {
        public event EventHandler<List<MangaObject>> SearchComplete;
        protected void OnSearchComplete(List<MangaObject> e)
        {
            if (SearchComplete != null)
            {
                if (synchronizationContext == null)
                    SearchComplete(this, e);
                else
                    foreach (EventHandler<List<MangaObject>> del in SearchComplete.GetInvocationList())
                        synchronizationContext.Post((s) => del(this, s as List<MangaObject>), e);
            }
        }

        public SmartSearch() : base() { }
        public SmartSearch(STPStartInfo stpThredPool) : base(stpThredPool) { }

        // public IWorkItemResult<List<MangaObject>> SearchManga(String search, Boolean Start = true)
        // { return smartThreadPool.QueueWorkItem<String, List<MangaObject>>(SearchWorker, search); }

        public IWorkItemResult SearchManga(String search, Boolean Start = true)
        { return smartThreadPool.QueueWorkItem(new WorkItemCallback(SearchWorker), search, new PostExecuteWorkItemCallback(SearchWorkerCallback)); }

        public void SearchWorkerCallback(IWorkItemResult wir)
        { OnSearchComplete(wir.Result as List<MangaObject>); }

        protected object SearchWorker(object state)
        { return SearchWorker(state as String); }
        protected List<MangaObject> SearchWorker(String search)
        {
            IWorkItemsGroup SearchWig = smartThreadPool.CreateWorkItemsGroup(2);
            Dictionary<String, MangaObject> MangaObjectSearchResults = new Dictionary<String, MangaObject>();
            Regex safeAlphaNumeric = new Regex("[^a-z0-9]", RegexOptions.IgnoreCase);
            Dictionary<ISiteExtension, IWorkItemResult<String>> MangaSearchWorkItems = new Dictionary<ISiteExtension, IWorkItemResult<String>>(App.SiteExtensions.DLLCollection.Count);
            foreach (ISiteExtension SiteExtension in App.SiteExtensions.DLLCollection)
            {
                ISiteExtensionDescriptionAttribute isea = SiteExtension.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);
                if (isea.SupportedObjects.HasFlag(SupportedObjects.Search))
                    MangaSearchWorkItems.Add(SiteExtension, SearchWig.QueueWorkItem<String, String, String>(DownloadHtmlContent, SiteExtension.GetSearchUri(searchTerm: search), isea.RefererHeader));
            }
            // Wait for the searches to complete
            SearchWig.WaitForIdle();
            foreach (KeyValuePair<ISiteExtension, IWorkItemResult<String>> val in MangaSearchWorkItems)
            {
                List<SearchResultObject> SearchResults = val.Key.ParseSearch(val.Value.Result);
                foreach (SearchResultObject SearchResult in SearchResults)
                {
                    String key = safeAlphaNumeric.Replace(SearchResult.Name.ToLower(), String.Empty);
                    if (MangaObjectSearchResults.ContainsKey(key))
                        MangaObjectSearchResults[key].Merge(SearchResult.ConvertToMangaObject());
                    else
                        MangaObjectSearchResults.Add(key, SearchResult.ConvertToMangaObject());
                }
            }
            SearchWig.Concurrency = 4;
            Dictionary<KeyValuePair<String, ISiteExtension>, IWorkItemResult<String>> MangaObjectWorkItems = new Dictionary<KeyValuePair<String, ISiteExtension>, IWorkItemResult<String>>();
            foreach (KeyValuePair<String, MangaObject> MangaObject in MangaObjectSearchResults)
            {
                foreach (LocationObject LocationObj in MangaObject.Value.Locations.FindAll(l => l.Enabled))
                {
                    ISiteExtension ise = App.SiteExtensions.DLLCollection[LocationObj.ExtensionName];
                    ISiteExtensionDescriptionAttribute isea = ise.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);
                    if (isea.SupportedObjects.HasFlag(SupportedObjects.Search))
                        MangaObjectWorkItems.Add(new KeyValuePair<String, ISiteExtension>(MangaObject.Key, ise), SearchWig.QueueWorkItem<String, String, String>(DownloadHtmlContent, LocationObj.Url, isea.RefererHeader));
                }
            }
            // Wait for the mangaobjects to load
            SearchWig.WaitForIdle();
            foreach (KeyValuePair<KeyValuePair<String, ISiteExtension>, IWorkItemResult<String>> val in MangaObjectWorkItems)
            {
                MangaObject dmObj = val.Key.Value.ParseMangaObject(val.Value.Result);
                if (dmObj != null)
                    MangaObjectSearchResults[val.Key.Key].Merge(dmObj);
            }
            Dictionary<IDatabaseExtension, IWorkItemResult<String>> DatabaseSearchWorkItems = new Dictionary<IDatabaseExtension, IWorkItemResult<String>>(App.DatabaseExtensions.DLLCollection.Count);
            foreach (IDatabaseExtension DatabaseExtension in App.DatabaseExtensions.DLLCollection)
            {
                IDatabaseExtensionAttribute idea = DatabaseExtension.GetType().GetCustomAttribute<IDatabaseExtensionAttribute>(false);
                if (idea.SupportedObjects.HasFlag(SupportedObjects.Search))
                    DatabaseSearchWorkItems.Add(DatabaseExtension, SearchWig.QueueWorkItem<String, String, String>(DownloadHtmlContent, DatabaseExtension.GetSearchUri(searchTerm: search), idea.RefererHeader));
            }
            // Wait for the database searches to complete
            SearchWig.WaitForIdle();
            foreach (KeyValuePair<IDatabaseExtension, IWorkItemResult<String>> val in DatabaseSearchWorkItems)
            {
                List<DatabaseObject> DatabaseResults = val.Key.ParseSearch(val.Value.Result);
                foreach (DatabaseObject DatabaseResult in DatabaseResults)
                {
                    String key = safeAlphaNumeric.Replace(DatabaseResult.Name.ToLower(), String.Empty);
                    if (MangaObjectSearchResults.ContainsKey(key))
                        MangaObjectSearchResults[key].AttachDatabase(DatabaseResult, true);
                }
            }
            List<MangaObject> MangaObjectResults = new List<MangaObject>(MangaObjectSearchResults.Values);
            MangaObjectResults.RemoveAll(mo => mo.Locations.FindAll(lo => !lo.Enabled).Count == mo.Locations.Count);
            MangaObjectResults.ForEach(mo => mo.Genres.ForEach(g => Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(g.ToLower())));
            MangaObjectResults.TrimExcess();
            return MangaObjectResults;
        }

        protected String DownloadHtmlContent(String url, String referer = null)
        {
            String content = null;

            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Referer = referer ?? request.Host;
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    content = streamReader.ReadToEnd();
                }
            }
            return content;
        }
    }
}

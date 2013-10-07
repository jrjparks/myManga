using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using Amib.Threading;
using myMangaSiteExtension;
using myMangaSiteExtension.Attributes.ISiteExtension;
using myMangaSiteExtension.Collections;
using myMangaSiteExtension.Objects;

namespace myManga_App.IO.Network
{
    public class SmartSearch : SmartDownloader
    {
        public SmartSearch() : base() { }
        public SmartSearch(STPStartInfo stpThredPool) : base(stpThredPool) { }

        public IWorkItemResult SearchManga(String search)
        {
            return smartThreadPool.QueueWorkItem<String, ISiteExtensionCollection>(Search, search, App.SiteExtensions.DLLCollection);
        }

        protected void Search(String search, ISiteExtensionCollection collection)
        {
            Dictionary<String, List<String>> SearchResults = new Dictionary<String, List<String>>();
            foreach (ISiteExtension ise in collection)
            {
                ISiteExtensionAttribute isea = ise.GetType().GetCustomAttribute<ISiteExtensionAttribute>(false);
                if (isea.SupportedObjects.HasFlag(SupportedObjects.Search))
                {
                    String SearchURL = ise.GetSearchUri(searchTerm: search);

                    HttpWebRequest request = WebRequest.Create(SearchURL) as HttpWebRequest;
                    request.Referer = isea.RefererHeader ?? request.Host;
                    request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                    {
                        using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                        {
                            foreach (KeyValuePair<String, SearchResultObject> SearchResult in ise.ParseSearch(streamReader.ReadToEnd()))
                            {

                            }
                        }
                    }
                }
            }
        }
    }
}

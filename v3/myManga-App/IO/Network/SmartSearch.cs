using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using Amib.Threading;
using myMangaSiteExtension;
using myMangaSiteExtension.Attributes.ISiteExtension;
using myMangaSiteExtension.Objects;

namespace myManga_App.IO.Network
{
    public class SmartSearch : SmartDownloader
    {
        public SmartSearch() : base() { }
        public SmartSearch(STPStartInfo stpThredPool) : base(stpThredPool) { }

        public IWorkItemsGroup SearchManga(String search, Boolean Start = true)
        {
            IWorkItemsGroup searchWorkGroup = smartThreadPool.CreateWorkItemsGroup(2, new WIGStartInfo() { StartSuspended = !Start });
            searchWorkGroup.Name = String.Format("%s:SearchWorkGroup", search);
            foreach (ISiteExtension SiteExtension in App.SiteExtensions.DLLCollection)
                searchWorkGroup.QueueWorkItem<String, ISiteExtension, List<SearchResultObject>>(Search, search, SiteExtension);
            return searchWorkGroup;
        }

        protected List<SearchResultObject> Search(String search, ISiteExtension SiteExtension)
        {
            List<SearchResultObject> SearchResults = new List<SearchResultObject>();
            ISiteExtensionDescriptionAttribute isea = SiteExtension.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);
            if (isea.SupportedObjects.HasFlag(SupportedObjects.Search))
            {
                String SearchURL = SiteExtension.GetSearchUri(searchTerm: search);

                HttpWebRequest request = WebRequest.Create(SearchURL) as HttpWebRequest;
                request.Referer = isea.RefererHeader ?? request.Host;
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        SearchResults = SiteExtension.ParseSearch(streamReader.ReadToEnd());
                    }
                }
            }
            SearchResults.TrimExcess();
            return SearchResults;
        }
    }
}

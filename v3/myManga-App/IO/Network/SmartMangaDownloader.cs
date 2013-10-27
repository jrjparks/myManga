using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows.Documents;
using Amib.Threading;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Collections;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;

namespace myManga_App.IO.Network
{
    public class SmartMangaDownloader : SmartDownloader
    {
        public SmartMangaDownloader() : base() { }
        public SmartMangaDownloader(STPStartInfo stpThredPool) : base(stpThredPool) { }

        public IWorkItemResult<MangaObject> DownloadMangaObject(MangaObject MangaObject)
        {
            return smartThreadPool.QueueWorkItem<MangaObject, MangaObject>(DownloadMangaObjectWorker, MangaObject);
        }

        public SmartGroupObject<MangaObject> DownloadMangaObjects(IEnumerable<MangaObject> MangaObjects, Boolean Start = true)
        {
            SmartGroupObject<MangaObject> downloadMangaObjectWorkers = new SmartGroupObject<MangaObject>(smartThreadPool.CreateWorkItemsGroup(2, new WIGStartInfo() { StartSuspended = !Start }));
            downloadMangaObjectWorkers.WorkItemsGroup.Name = String.Format("{0}:DownloadMangaObjectsGroup", Guid.NewGuid());
            foreach (MangaObject MangaObj in MangaObjects)
                downloadMangaObjectWorkers.WorkItemResults.Add(downloadMangaObjectWorkers.WorkItemsGroup.QueueWorkItem<MangaObject, MangaObject>(DownloadMangaObjectWorker, MangaObj));
            return downloadMangaObjectWorkers;
        }

        private MangaObject DownloadMangaObjectWorker(MangaObject mangaObject)
        {
            ISiteExtensionCollection isec = (App.Current as App).SiteExtensions.DLLCollection;
            foreach (LocationObject location in mangaObject.Locations.FindAll(l => l.Enabled))
            {
                ISiteExtension ise = isec[location.ExtensionName];
                ISiteExtensionDescriptionAttribute isea = ise.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);
                HttpWebRequest request = WebRequest.Create(location.Url) as HttpWebRequest;
                request.Referer = isea.RefererHeader ?? request.Host;
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        MangaObject remoteMangaObject = ise.ParseMangaObject(streamReader.ReadToEnd());
                        location.Enabled = remoteMangaObject != null;
                        if (location.Enabled)
                            mangaObject.Merge(remoteMangaObject);
                    }
                }
            }
            return mangaObject;
        }
    }
}

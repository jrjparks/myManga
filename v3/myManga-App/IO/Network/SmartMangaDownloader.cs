using System.IO;
using System.Net;
using System.Reflection;
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

        public IWorkItemResult<MangaObject> DownloadManga(MangaObject mangaObject)
        {
            return smartThreadPool.QueueWorkItem<MangaObject, MangaObject>(DownloadMangaObject, mangaObject);
        }

        private MangaObject DownloadMangaObject(MangaObject mangaObject)
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

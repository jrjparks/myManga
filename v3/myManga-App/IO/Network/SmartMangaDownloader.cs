using Amib.Threading;
using Core.IO;
using myMangaSiteExtension;
using myMangaSiteExtension.Collections;
using myMangaSiteExtension.Objects;
using System;
using System.Net;

namespace myManga_App.IO.Network
{
    public class SmartMangaDownloader : SmartDownloader
    {
        public SmartMangaDownloader() : base() { }
        public SmartMangaDownloader(STPStartInfo stpThredPool) : base(stpThredPool) { }

        public IWorkItemResult DownloadManga(MangaObject mangaObject)
        {
            return smartThreadPool.QueueWorkItem<MangaObject>(DownloadMangaObject, mangaObject);
        }

        private void DownloadMangaObject(MangaObject mangaObject)
        {
            ISiteExtensionCollection isec = (App.Current as App).SiteExtensions.DLLCollection;
            foreach (KeyValuePair<String, String> kvp in mangaObject.Locations)
            {
                ISiteExtension ise = isec[kvp.Key];
                HttpWebRequest request = WebRequest.Create(kvp.Value) as HttpWebRequest;
                request.Referer = ise. ?? request.Host;
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (Stream webStream = response.GetResponseStream())
                    {
                        int read;
                        byte[] buffer = new byte[1024]; // Read in 1K chunks
                        while ((read = webStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            downloadData.Stream.Write(buffer, 0, read);
                            downloadData.Progress = (Int32)Math.Ceiling(((double)downloadData.Stream.Position / (double)response.ContentLength) * 100D);
                            OnDownloadTaskProgress(downloadData);
                        }
                        downloadData.Stream.Seek(0, SeekOrigin.Begin);
                    }
                }
                ise.ParseMangaObject("");
            }
        }
    }
}

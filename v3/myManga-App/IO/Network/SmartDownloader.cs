using Amib.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace myManga_App.IO.Network
{
    public class SmartDownloader
    {
        protected readonly SmartThreadPool smartThreadPool;
        protected readonly SynchronizationContext synchronizationContext;
        protected readonly App App = App.Current as App;
        public Int32 Concurrency
        { get { return smartThreadPool.Concurrency; } }
        public Boolean IsIdle
        { get { return smartThreadPool.IsIdle; } }

        public SmartDownloader() : this(null) { }

        public SmartDownloader(STPStartInfo stpThredPool)
        {
            smartThreadPool = new SmartThreadPool(stpThredPool ?? new STPStartInfo() { MaxWorkerThreads = 5 });
            synchronizationContext = SynchronizationContext.Current;
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

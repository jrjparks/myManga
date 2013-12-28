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

        protected String GetHtmlContent(String url, String referer = null)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Referer = referer ?? request.Host;
            request.Method = "GET";
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            return GetResponseString(request);
        }

        protected String GetResponseString(HttpWebRequest request)
        {
            String content = null;
            try
            {
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                    { content = streamReader.ReadToEnd(); }
                }
            }
            catch (WebException webEx)
            {
                using (HttpWebResponse response = webEx.Response as HttpWebResponse)
                {
                    using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                    { content = streamReader.ReadToEnd(); }
                }
            }
            return System.Web.HttpUtility.HtmlDecode(content);
        }
    }
}

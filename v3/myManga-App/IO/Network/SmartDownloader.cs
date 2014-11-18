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

        protected Stream GetRawContent(String url, String referer = null)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Referer = referer ?? request.Host;
            request.Method = "GET";
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            return GetResponse(request);
        }

        protected String GetHtmlContent(String url, String referer = null)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Referer = referer ?? request.Host;
            request.Method = "GET";
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            return GetResponseString(request);
        }

        protected Stream GetResponse(HttpWebRequest request)
        {
            Stream content = new MemoryStream();
            try
            {
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (Stream response_stream = response.GetResponseStream())
                    {
                        response_stream.CopyTo(content);
                    }
                }
            }
            catch (WebException webEx)
            {
                using (HttpWebResponse response = webEx.Response as HttpWebResponse)
                {
                    using (Stream response_stream = response.GetResponseStream())
                    {
                        response_stream.CopyTo(content);
                    }
                }
            }
            content.Seek(0, SeekOrigin.Begin);
            return content;
        }

        protected String GetResponseString(HttpWebRequest request)
        {
            String content = null;
            using (StreamReader streamReader = new StreamReader(GetResponse(request)))
            {
                content = streamReader.ReadToEnd();
            }
            return System.Web.HttpUtility.HtmlDecode(content);
        }
    }
}

using System;
using System.IO;
using System.Net;
using System.Web;

namespace myManga_App.IO.Network
{
    public sealed class WebDownloader : WebClient
    {
        public CookieContainer CookieContainer
        { get; private set; }
        public String Referer
        { get; set; }
        public Int32 Timeout
        { get; set; }
        public Int32 ContinueTimeout
        { get; set; }
        public Int32 ReadWriteTimeout
        { get; set; }

        /// <summary>
        /// Empty WebDownloader constructor
        /// </summary>
        public WebDownloader() : this(new CookieContainer(), null)
        { }

        /// <summary>
        /// WebDownloader constructor with cookies
        /// </summary>
        /// <param name="cookies">Cookies</param>
        public WebDownloader(CookieCollection cookies) : this(new CookieContainer(), cookies)
        { }

        /// <summary>
        /// WebDownloader constructor with cookies and cookie container
        /// </summary>
        /// <param name="cookieContainer">Cookie Container</param>
        /// <param name="cookies">Cookies</param>
        public WebDownloader(CookieContainer cookieContainer, CookieCollection cookies) : base()
        {
            CookieContainer = cookieContainer;
            if (!Equals(cookies, null))
            { CookieContainer.Add(cookies); }
            Proxy = SystemWebProxy();
        }

        private IWebProxy SystemWebProxy()
        {
            IWebProxy proxy = WebRequest.DefaultWebProxy;
            proxy.Credentials = CredentialCache.DefaultCredentials;
            return proxy;
        }

        /// <summary>
        /// Returns a System.Net.WebRequest object for the specified resource.
        /// </summary>
        /// <param name="address">A System.Uri that identifies the resource to request.</param>
        /// <returns>A new System.Net.WebRequest object for the specified resource.</returns>
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = base.GetWebRequest(address) as HttpWebRequest;
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            request.CookieContainer = CookieContainer;
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Referer = Referer ?? request.Host;
            if (Timeout > 0) request.Timeout = Timeout;
            if (ContinueTimeout > 0) request.ContinueTimeout = ContinueTimeout;
            if (ReadWriteTimeout > 0) request.ReadWriteTimeout = ReadWriteTimeout;
            return request;
        }

        /// <summary>
        /// Returns the System.Net.WebResponse for the specified System.Net.WebRequest.
        /// </summary>
        /// <param name="request">A System.Net.WebRequest that is used to obtain the response.</param>
        /// <returns>A System.Net.WebResponse containing the response for the specified System.Net.WebRequest.</returns>
        protected override WebResponse GetWebResponse(WebRequest request)
        {
            HttpWebResponse response = base.GetWebResponse(request) as HttpWebResponse;
            CookieContainer.Add(response.Cookies);
            return response;
        }

        /// <summary>
        /// Method for creating copy of response stream.
        /// </summary>
        /// <param name="request">A System.Net.WebRequest that is used to obtain the response.</param>
        /// <returns>A System.IO.Stream of the response.</returns>
        public Stream GetWebResponseStream(WebRequest request)
        {
            Stream content = new MemoryStream();
            try
            {
                using (HttpWebResponse response = this.GetWebResponse(request) as HttpWebResponse)
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

        /// <summary>
        /// Method for reading response stream to string.
        /// </summary>
        /// <param name="request">A System.Net.WebRequest that is used to obtain the response.</param>
        /// <returns>A System.String of the response</returns>
        public String GetWebResponseString(WebRequest request)
        {
            String content = String.Empty;
            using (StreamReader streamReader = new StreamReader(GetWebResponseStream(request: request)))
            { content = streamReader.ReadToEnd(); }
            return HttpUtility.HtmlDecode(content);
        }

        /// <summary>
        /// Get a System.IO.Stream from a url
        /// </summary>
        /// <param name="url">The resource to request.</param>
        /// <param name="referer">The resource referer.</param>
        /// <returns></returns>
        public Stream GetRawContent(String url, String referer = null)
        {
            HttpWebRequest request = this.GetWebRequest(new Uri(url)) as HttpWebRequest;
            request.Referer = referer ?? request.Host;
            request.Method = "GET";
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            return GetWebResponseStream(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="referer"></param>
        /// <returns></returns>
        public String GetStringContent(String url, String referer = null)
        {
            HttpWebRequest request = this.GetWebRequest(new Uri(url)) as HttpWebRequest;
            request.Referer = referer ?? request.Host;
            request.Method = "GET";
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            return GetWebResponseString(request);
        }
    }
}

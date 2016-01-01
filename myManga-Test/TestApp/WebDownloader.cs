using System;
using System.IO;
using System.Net;
using System.Web;

namespace TestApp
{
    public sealed class WebDownloader : WebClient
    {
        public CookieContainer CookieContainer
        { get; private set; }
        public String Referer
        { get; set; }

        /// <summary>
        /// Empty WebDownloader constructor
        /// </summary>
        public WebDownloader() : this(new CookieContainer(), new CookieCollection())
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
            this.CookieContainer = cookieContainer;
            this.CookieContainer.Add(cookies);
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
            request.CookieContainer = this.CookieContainer;
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Referer = Referer ?? request.Host;
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
            this.CookieContainer.Add(response.Cookies);
            return response;
        }
    }
}

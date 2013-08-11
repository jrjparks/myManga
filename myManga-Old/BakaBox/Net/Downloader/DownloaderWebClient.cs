using System;
using System.Diagnostics;
using System.Net;

namespace BakaBox.Net.Downloader
{
    [DebuggerStepThrough]
    public class DownloaderWebClient : System.Net.WebClient
    {
        private Boolean _UseCookies;
        public Boolean UseCookies
        {
            get { return _UseCookies; }
            private set
            {
                if (Cookies == null && value)
                    Cookies = new CookieContainer();
                else if (Cookies != null)
                    Cookies = null;

                _UseCookies = value;
            }
        }
        public CookieContainer Cookies { get; private set; }

        public DownloaderWebClient()
            : base()
        {
            UseCookies = false;
        }

        protected override WebRequest GetWebRequest(Uri Address)
        {
            WebRequest WebRequestBase = base.GetWebRequest(Address);
            if (WebRequestBase is HttpWebRequest)
            {
                HttpWebRequest HttpWebRequest = WebRequestBase as HttpWebRequest;
                HttpWebRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                if (UseCookies)
                    HttpWebRequest.CookieContainer = Cookies;
                WebRequestBase = HttpWebRequest;
            }
            return WebRequestBase;
        }
    }
}

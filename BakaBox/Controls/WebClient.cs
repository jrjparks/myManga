using System;
using System.Net;
using System.Diagnostics;

namespace BakaBox.Controls
{
    [DebuggerStepThrough]
    public class WebClient : System.Net.WebClient
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

        public WebClient()
            : base()
        {
            UseCookies = false;
        }

        protected override WebRequest GetWebRequest(Uri Address)
        {
            WebRequest WebRequest = base.GetWebRequest(Address);
            if (WebRequest is HttpWebRequest)
            {
                (WebRequest as HttpWebRequest).AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                if (UseCookies)
                    (WebRequest as HttpWebRequest).CookieContainer = Cookies;
            }
            return WebRequest;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Core.Attributes;

namespace Core.NET
{
    public class AdvWebClient : WebClient
    {
        public enum Method
        {
            [Description("GET")]
            GET = 0x01,
            [Description("HEAD")]
            HEAD = 0x02,
            [Description("POST")]
            POST = 0x04,
            [Description("PUT")]
            PUT = 0x08,
            [Description("DELETE")]
            DELETE = 0x16,
            [Description("TRACE")]
            TRACE = 0x32,
            [Description("OPTIONS")]
            OPTIONS = 0x64
        }
        public Method RequestMethod { get; set; }

        public CookieContainer Cookies { get; protected set; }

        protected Boolean useCookies;
        public Boolean UseCookies
        {
            get { return useCookies; }
            protected set
            {
                if (Cookies == null && value)
                    Cookies = new CookieContainer();
                else if (Cookies != null)
                    Cookies = null;
                useCookies = value;
            }
        }

        public AdvWebClient()
            : base()
        {
            UseCookies = false;
            RequestMethod = Method.GET;
        }

        protected override WebRequest GetWebRequest(Uri Address)
        {
            WebRequest WebRequest = base.GetWebRequest(Address);
            if (WebRequest is HttpWebRequest)
            {
                (WebRequest as HttpWebRequest).Method = RequestMethod.GetAttributeOfEnum<DescriptionAttribute>().FirstOrDefault().Description;
                (WebRequest as HttpWebRequest).AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                if (UseCookies)
                    (WebRequest as HttpWebRequest).CookieContainer = Cookies;
            }
            return WebRequest;
        }
    }
}

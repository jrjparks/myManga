using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Manga.Core
{
    public class GzipWebClient : WebClient
    {
        public Int32 RequestTimeOut { get; set; }

        public GzipWebClient()
            : base()
        {
            RequestTimeOut = 100000;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest GzipRequest = (HttpWebRequest)base.GetWebRequest(address);
            GzipRequest.Credentials = CredentialCache.DefaultCredentials;
            GzipRequest.Timeout = RequestTimeOut;
            GzipRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            return GzipRequest;
        }
    }
}

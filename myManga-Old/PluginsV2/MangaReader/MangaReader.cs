using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IMangaSite;
using System.IO;

namespace MangaReader
{
    [IMangaSiteData("MangaReader", "James R Parks", "2.0.0", @"mangareader\.net", "http://www.mangareader.net/", SupportedMethods.All)]
    public class MangaReader : BaseMangaSite, IMangaSite.IMangaSite
    {
        public MangaReader()
            : base()
        {

        }

        public Guid RequestInfo(string InfoURL)
        {
            DownloadRequest dr = new DownloadRequest() { RemoteURL = InfoURL };
            OnDownloadRequested(dr);
            return dr.Id;
        }

        public Guid RequestChapterList()
        {
            throw new NotImplementedException();
        }

        public Guid RequestChapterImageList()
        {
            throw new NotImplementedException();
        }

        public object ParseResponse(Stream Content)
        {
            String content = "No Content";
            try
            {
                using (StreamReader contentReader = new StreamReader(Content))
                {
                    content = contentReader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                content = ex.ToString();
            }
            return content;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IMangaSite;

namespace MangaReader
{
    [IMangaSiteData("MangaReader", "James R Parks", "2.0.0", @"mangareader\.net", "http://www.mangareader.net/", SupportedMethods.All)]
    public class MangaReader : BaseMangaSite, IMangaSite.IMangaSite
    {
        public MangaReader()
            : base()
        {

        }
    }
}

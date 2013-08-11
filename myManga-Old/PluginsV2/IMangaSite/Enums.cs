using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IMangaSite
{
    [Flags]
    public enum SupportedMethods
    {
        None = 0x01,
        ChapterInfo = 0x02,
        MangaInfo = 0x04,
        Search = 0x08,
        CoverImage = 0x16,
        All = ChapterInfo | MangaInfo | Search | CoverImage
    }
}

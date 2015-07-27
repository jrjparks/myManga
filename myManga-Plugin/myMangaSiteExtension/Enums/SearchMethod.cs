using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myMangaSiteExtension.Enums
{
    [Flags]
    public enum SearchMethod
    {
        None = 0x00,
        GET = 0x01,
        POST = 0x02,
    }
}

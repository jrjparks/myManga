using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace myMangaSiteExtension.Objects
{
    public class MangaObject
    {
        protected String name;
        protected String[] alternate_names;
        
        protected Dictionary<ISiteExtension, String> locations;
        protected String[] covers;

        protected List<Object> chapters;
    }
}

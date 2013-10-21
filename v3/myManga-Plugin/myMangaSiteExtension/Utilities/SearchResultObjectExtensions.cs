using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using myMangaSiteExtension.Objects;

namespace myMangaSiteExtension.Utilities
{
    public static class SearchResultObjectExtensions
    {
        public static MangaObject ConvertToMangaObject(this SearchResultObject value)
        {
            return new MangaObject()
            {
                Name = value.Name,
                Locations = { new LocationObject() { Url = value.Url, ExtensionName = value.ExtensionName } },
                Covers = { value.CoverUrl },
                Authors = value.Authors,
                Artists = value.Artists,
                Rating = value.Rating
            };
        }
    }
}

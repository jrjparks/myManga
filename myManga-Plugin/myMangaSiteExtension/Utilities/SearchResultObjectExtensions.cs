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
                Description = value.Description,
                Locations = { new LocationObject() { Url = value.Url, ExtensionName = value.ExtensionName } },
                CoverLocations = { value.Cover },
                Authors = value.Authors,
                Artists = value.Artists,
                Rating = value.Rating
            };
        }
    }
}

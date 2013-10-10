using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Other;
using HtmlAgilityPack;
using myMangaSiteExtension;
using myMangaSiteExtension.Attributes.ISiteExtension;
using myMangaSiteExtension.Objects;

namespace MangaHere
{
    [ISiteExtensionDescription(
        "MangaHere",
        "mangahere.com",
        "http://www.mangahere.com/",
        RootUrl = "http://www.mangahere.com",
        Author = "James Parks",
        Version = "0.0.1",
        SupportedObjects = SupportedObjects.None,
        Language = "English")]
    public class MangaHere : ISiteExtension
    {
        ISiteExtensionDescriptionAttribute isea;
        private ISiteExtensionDescriptionAttribute ISEA { get { return isea ?? (isea = this.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false)); } }

        public string GetSearchUri(string searchTerm)
        {
            throw new NotImplementedException();
        }

        public MangaObject ParseMangaObject(string content)
        {
            throw new NotImplementedException();
        }

        public ChapterObject ParseChapterObject(string content)
        {
            throw new NotImplementedException();
        }

        public PageObject ParsePageObject(string content)
        {
            throw new NotImplementedException();
        }

        public List<SearchResultObject> ParseSearch(string content)
        {
            throw new NotImplementedException();
        }
    }
}

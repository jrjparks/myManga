using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Other;
using HtmlAgilityPack;
using myMangaSiteExtension;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;

namespace MangaEden
{
    [ISiteExtensionDescription(
           "MangaEden",
           "mangaeden.com",
           "http://www.mangaeden.com/",
           RootUrl = "http://www.mangaeden.com",
           Author = "James Parks",
           Version = "0.0.1",
           SupportedObjects = SupportedObjects.All,
           Language = "English")]
    public class MangaEden : ISiteExtension
    {
        protected ISiteExtensionDescriptionAttribute isea;
        protected virtual ISiteExtensionDescriptionAttribute ISEA { get { return isea ?? (isea = GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false)); } }

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

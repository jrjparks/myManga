using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using myMangaSiteExtension.Objects;

namespace MangaEden
{
    [DataContract]
    public class SearchResponse
    {
        [DataMember(Name = "label")]
        public String Label { get; set; }
        [DataMember(Name = "url")]
        public String URL { get; set; }
        [DataMember(Name = "value")]
        public String Value { get; set; }
    }

    [DataContract]
    public class MangaList
    {
        [DataMember(Name = "end")]
        public Int32 End { get; set; }
        [DataMember(Name = "start")]
        public Int32 Start { get; set; }
        [DataMember(Name = "page")]
        public Int32 Page { get; set; }
        [DataMember(Name = "total")]
        public Int32 Total { get; set; }

        [DataMember(Name = "manga")]
        public MangaItem[] Manga { get; set; }
    }

    [DataContract]
    public class MangaItem
    {
        [DataMember(Name = "a")]
        public String Alias { get; set; }
        [DataMember(Name = "c")]
        public String[] Category { get; set; }
        [DataMember(Name = "h")]
        public Int32 Hits { get; set; }
        [DataMember(Name = "i")]
        public String Id { get; set; }
        [DataMember(Name = "im")]
        public String MangaImageUrl { get; set; }
        [DataMember(Name = "ld")]
        public Int64 LastChapterDate { get; set; }
        [DataMember(Name = "s")]
        public String Status { get; set; }
        [DataMember(Name = "t")]
        public String Title { get; set; }
    }

    [DataContract]
    public class MangaDetails
    {
        [DataMember(Name = "aka")]
        public String[] Aka { get; set; }

        [DataMember(Name = "alias")]
        public String Alias { get; set; }

        [DataMember(Name = "artist")]
        public String Artist { get; set; }
        [DataMember(Name = "author")]
        public String Author { get; set; }

        [DataMember(Name = "categories")]
        public String[] Categories { get; set; }

        [DataMember(Name = "chapters")]
        public Object[][] Chapters { get; set; }
        [DataMember(Name = "chapters_len")]
        public Int32 ChapterCount { get; set; }

        [DataMember(Name = "created")]
        public Int64 Created { get; set; }
        [DataMember(Name = "last_chapter_date")]
        public Int64 LastChapterDate { get; set; }
        [DataMember(Name = "released")]
        public Int32 Released { get; set; }

        [DataMember(Name = "title")]
        public String Title { get; set; }
        [DataMember(Name = "description")]
        public String Description { get; set; }

        [DataMember(Name = "image")]
        public String Cover { get; set; }

        [DataMember(Name = "language")]
        public Int32 Language { get; set; }
    }

    [DataContract]
    public class ChapterDetail
    {
        [DataMember(Name = "images")]
        public Object[][] Pages { get; set; }
    }

    [DataContract]
    public class PageItem
    {
        [DataMember(Order = 0)]
        public UInt32 Page { get; set; }
        [DataMember(Order = 1)]
        public String Image { get; set; }
        [DataMember(Order = 2)]
        public Int32 Width { get; set; }
        [DataMember(Order = 3)]
        public Int32 Height { get; set; }
    }

    public static class ModelExtensions
    {
        private readonly static DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
        public static DateTime FromUnix(this Int64 unixTime)
        {
            Int64 unixTimeStampInTicks = (unixTime * TimeSpan.TicksPerSecond);
            return new DateTime(unixStart.Ticks + unixTimeStampInTicks);
        }

        public static SearchResultObject ToSearchResultObject(this MangaItem item)
        {
            return new SearchResultObject()
            {
                Name = HtmlEntity.DeEntitize(item.Title),
                Id = item.Id,
                Url = String.Format("https://www.mangaeden.com/api/manga/{0}", item.Id),
                Cover = new LocationObject()
                {
                    Enabled = true,
                    Url = String.Format("https://cdn.mangaeden.com/mangasimg/{0}", item.MangaImageUrl)
                }
            };
        }

        public static MangaObject ToMangaObject(this MangaDetails details)
        {
            details.Chapters = details.Chapters.Reverse().ToArray();
            return new MangaObject()
            {
                AlternateNames = (from Alt in details.Aka select HtmlEntity.DeEntitize(Alt)).ToList(),
                Artists = { HtmlEntity.DeEntitize(details.Artist) },
                Authors = { HtmlEntity.DeEntitize(details.Author) },
                Chapters = (from Chapter in details.Chapters select Chapter.ToChapterObject()).ToList(),
                Description = HtmlEntity.DeEntitize(details.Description),
                Released = new DateTime(details.Released, 1, 1),
                CoverLocations = { new LocationObject() {
                    Enabled = true,
                    Url = String.Format("https://cdn.mangaeden.com/mangasimg/{0}", details.Cover)
                } },
                Name = details.Title,
                Genres = details.Categories.ToList()
            };
        }

        public static ChapterObject ToChapterObject(this Object[] item)
        {
            return new ChapterObject()
            {
                Chapter = Convert.ToUInt32(item[0]),
                Locations = { new LocationObject() {
                    Enabled = true,
                    Url = String.Format("https://www.mangaeden.com/api/chapter/{0}", (String)item[3])
                } },
                Name = HtmlEntity.DeEntitize((String)item[2]),
                Released = Convert.ToInt64(item[1]).FromUnix()
            };
        }

        public static ChapterObject ToChapterObject(this ChapterDetail details)
        {
            details.Pages = details.Pages.Reverse().ToArray();
            return new ChapterObject()
            {
                Pages = (from Page in details.Pages
                         select new PageObject()
                         {
                             ImgUrl = String.Format("https://cdn.mangaeden.com/mangasimg/{0}", (String)Page[1]),
                             PageNumber = Convert.ToUInt32(Page[0])
                         }).ToList()
            };
        }
    }
}

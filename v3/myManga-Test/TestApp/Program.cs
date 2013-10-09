using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using myMangaSiteExtension;
using myMangaSiteExtension.Attributes.ISiteExtension;
using myMangaSiteExtension.Objects;

namespace TestApp
{
    static class Program
    {
        static Dictionary<String, ISiteExtension> Extentions = new Dictionary<String, ISiteExtension>();

        static void Main(string[] args)
        {
            Extentions.Add("MangaReader", new AFTV_Network.MangaReader());
            Extentions.Add("MangaPanda", new AFTV_Network.MangaPanda());
            LoadManga();
        }

        static void Search()
        {
            Console.Write("Search Term: ");
            String SearchTerm = Console.ReadLine();
            while (SearchTerm != null && SearchTerm != String.Empty)
            {
                ISiteExtension ise = new AFTV_Network.MangaPanda();
                ISiteExtensionDescriptionAttribute isea = ise.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);
                List<SearchResultObject> SearchResults = new List<SearchResultObject>();

                String SearchURL = ise.GetSearchUri(searchTerm: SearchTerm);

                HttpWebRequest request = WebRequest.Create(SearchURL) as HttpWebRequest;
                request.Referer = isea.RefererHeader ?? request.Host;
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        SearchResults = ise.ParseSearch(streamReader.ReadToEnd());
                    }
                }

                Console.WriteLine(String.Format("Search Term:{0}\n\tResults Found: {1}", SearchTerm, SearchResults.Count));
                foreach (SearchResultObject sro in SearchResults)
                {
                    Console.WriteLine(String.Format("Name: {0}", sro.Name));
                    Console.WriteLine(String.Format("\tId: {0}", (sro.Id >= 0) ? sro.Id.ToString() : "Unknown"));
                    Console.WriteLine(String.Format("\tUrl: {0}", sro.Url));
                    Console.WriteLine(String.Format("\tCover Url: {0}", sro.CoverUrl));
                    Console.WriteLine(String.Format("\tRating: {0}", (sro.Rating >= 0) ? sro.Rating.ToString() : "Unknown"));
                }

                Console.WriteLine();
                Console.WriteLine("Empty Search Term Exits Application.");
                Console.Write("Search Term: ");
                SearchTerm = Console.ReadLine();
            }
        }

        static void LoadManga()
        {
            MangaObject mObj = LoadMangaObject("http://www.mangareader.net/103/one-piece.html");
            Console.WriteLine("Returned MangaObject:");
            Console.WriteLine("\tName:{0}", mObj.Name);
            Console.WriteLine("\tReleased:{0}", mObj.Released.ToString("yyyy"));
            Console.WriteLine("\tAlternate Names:{0}", String.Join(", ", mObj.AlternateNames));
            Console.WriteLine("\tAuthors:{0}", String.Join(", ", mObj.Authors));
            Console.WriteLine("\tArtists:{0}", String.Join(", ", mObj.Artists));
            Console.WriteLine("\tGenres:{0}", String.Join(", ", mObj.Genres));
            Console.WriteLine("\tLocations:{0}", String.Join(", ", mObj.Locations));
            Console.WriteLine("\tNumber of Chapters:{0}", mObj.Chapters.Count);

            Console.WriteLine();
            Console.Write("Test Chapter Load...(press enter)");
            Console.ReadLine();

            ChapterObject cObj = mObj.Chapters[0];
            cObj.Pages = LoadChapterObject(cObj, 0).Pages;
            Console.WriteLine("Returned ChapterObject:");
            Console.WriteLine("\tName:{0}", mObj.Chapters[0].Name);
            Console.WriteLine("\tChapter:{0}", mObj.Chapters[0].Chapter);
            Console.WriteLine("\tReleased:{0}", mObj.Chapters[0].Released.ToString("d"));
            Console.WriteLine("\tNumber of Pages:{0}", mObj.Chapters[0].Pages.Count);

            Console.WriteLine();
            Console.Write("Test Page Load...(press enter)");
            Console.ReadLine();
            mObj.Chapters[0].LoadPageObjects(0);
            Console.WriteLine("Returned ChapterObject:");
            foreach (PageObject pageObject in cObj.Pages)
            {
                Console.WriteLine("\t[{0}]:", pageObject.PageNumber);
                Console.WriteLine("\t\tUrl: {0}", pageObject.Url);
                Console.WriteLine("\tImage: {0}", pageObject.ImgUrl);
            }
            Console.ReadLine();
        }

        static MangaObject LoadMangaObject(String Link)
        {
            MangaObject MangaObj = null;
            ISiteExtension ise = Extentions["MangaReader"];
            ISiteExtensionDescriptionAttribute isea = ise.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);

            HttpWebRequest request = WebRequest.Create(Link) as HttpWebRequest;
            request.Referer = isea.RefererHeader ?? request.Host;
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    MangaObj = ise.ParseMangaObject(streamReader.ReadToEnd());
                }
            }
            return MangaObj;
        }

        static ChapterObject LoadChapterObject(String Link)
        {
            ChapterObject ChapterObj = null;
            ISiteExtension ise = Extentions["MangaReader"];
            ISiteExtensionDescriptionAttribute isea = ise.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);

            HttpWebRequest request = WebRequest.Create(Link) as HttpWebRequest;
            request.Referer = isea.RefererHeader ?? request.Host;
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    ChapterObj = ise.ParseChapterObject(streamReader.ReadToEnd());
                }
            }
            return ChapterObj;
        }

        static ChapterObject LoadChapterObject(ChapterObject chapterObject, Int32 LocationId = 0)
        {
            ChapterObject ChapterObj = null;
            ISiteExtension ise = Extentions[chapterObject.Locations[LocationId].SiteExtensionName];
            ISiteExtensionDescriptionAttribute isea = ise.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);

            HttpWebRequest request = WebRequest.Create(chapterObject.Locations[LocationId].Url) as HttpWebRequest;
            request.Referer = isea.RefererHeader ?? request.Host;
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    ChapterObj = ise.ParseChapterObject(streamReader.ReadToEnd());
                }
            }
            return ChapterObj;
        }

        public static void LoadPageObjects(this ChapterObject chapterObject, Int32 LocationId = 0)
        {
            ChapterObject ChapterObj = chapterObject;
            ISiteExtension ise = Extentions[chapterObject.Locations[LocationId].SiteExtensionName];
            ISiteExtensionDescriptionAttribute isea = ise.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);

            List<PageObject> ParsedPages = new List<PageObject>();
            foreach (PageObject pageObject in chapterObject.Pages)
            {
                HttpWebRequest request = WebRequest.Create(chapterObject.Locations[LocationId].Url) as HttpWebRequest;
                request.Referer = isea.RefererHeader ?? request.Host;
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        ParsedPages.Add(ise.ParsePageObject(streamReader.ReadToEnd()));
                    }
                }
            }
        }
    }
}

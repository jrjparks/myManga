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
    class Program
    {
        static void Main(string[] args)
        {
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
            MangaObject mObj = LoadMangaObject("http://www.mangapanda.com/103/one-piece.html");
            Console.WriteLine("Returned MangaObject:");
            Console.WriteLine("\tName:{0}", mObj.Name);
            Console.WriteLine("\tReleased:{0}", mObj.Released.ToString("yyyy"));
            Console.WriteLine("\tAlternate Names:{0}", String.Join(", ", mObj.AlternateNames));
            Console.WriteLine("\tAuthors:{0}", String.Join(", ", mObj.Authors));
            Console.WriteLine("\tArtists:{0}", String.Join(", ", mObj.Artists));
            Console.WriteLine("\tGenres:{0}", String.Join(", ", mObj.Genres));
            Console.WriteLine("\tLocations:{0}", String.Join(", ", mObj.Locations));
            Console.WriteLine("\tNumber of Chapters:{0}", mObj.Chapters.Count);
            Console.ReadLine();
        }

        static MangaObject LoadMangaObject(String Link)
        {
            MangaObject MangaObj = null;
            ISiteExtension ise = new AFTV_Network.MangaPanda();
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
            ISiteExtension ise = new AFTV_Network.MangaReader();
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
    }
}

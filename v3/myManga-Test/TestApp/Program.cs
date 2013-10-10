using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Reflection;
using myMangaSiteExtension;
using myMangaSiteExtension.Attributes.ISiteExtension;
using myMangaSiteExtension.Objects;
using Core.IO;

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
            //Search();
        }

        static void Search()
        {
            Console.Write("Search Term: ");
            String SearchTerm = Console.ReadLine();
            while (SearchTerm != null && SearchTerm != String.Empty)
            {
                Dictionary<String, List<SearchResultObject>> RawSearchResults = new Dictionary<String, List<SearchResultObject>>();
                foreach (ISiteExtension ise in Extentions.Values)
                {
                    ISiteExtensionDescriptionAttribute isea = ise.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);
                    String SearchURL = ise.GetSearchUri(searchTerm: SearchTerm);

                    HttpWebRequest request = WebRequest.Create(SearchURL) as HttpWebRequest;
                    request.Referer = isea.RefererHeader ?? request.Host;
                    request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                    {
                        using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                        {
                            foreach (SearchResultObject searchResultObject in ise.ParseSearch(streamReader.ReadToEnd()))
                            {
                                String keyName = searchResultObject.Name.ToLower().Replace(" ", String.Empty);
                                if (!RawSearchResults.ContainsKey(keyName))
                                    RawSearchResults[keyName] = new List<SearchResultObject>();
                                RawSearchResults[keyName].Add(searchResultObject);
                            }
                        }
                    }
                }

                Console.WriteLine(String.Format("Search Term:{0}\n\tResults Found: {1}", SearchTerm, RawSearchResults.Count));
                foreach (List<SearchResultObject> SearchResultObjects in RawSearchResults.Values)
                {
                    Console.WriteLine(String.Format("Name: {0}", SearchResultObjects[0].Name));
                    Console.WriteLine(String.Format("\tUrl: {0}", String.Join("\n\t     ", (from SearchResultObject sro in SearchResultObjects select sro.Url).ToArray())));
                    Console.WriteLine(String.Format("\tCover Url: {0}", String.Join("\n\t           ", (from SearchResultObject sro in SearchResultObjects select sro.CoverUrl).ToArray())));
                }

                Console.WriteLine();
                Console.WriteLine("Empty Search Term Exits Application.");
                Console.Write("Search Term: ");
                SearchTerm = Console.ReadLine();
            }
        }

        static void LoadManga()
        {
            MangaObject mObj = LoadMangaObject("http://www.mangareader.net/163/sekirei.html");
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
            Console.WriteLine("Loading...");

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
            Console.WriteLine("Loading...");
            cObj.LoadPageObjects(0);
            cObj.SaveToArchive(cObj.Name + ".mca", "ChapterObject", SaveType.XML);
            Console.WriteLine("Returned ChapterObject:");
            foreach (PageObject pageObject in cObj.Pages)
            {
                Console.WriteLine("\t[{0}]:", pageObject.PageNumber);
                Console.WriteLine("\t\tUrl: {0}", pageObject.Url);
                Console.WriteLine("\t\tImage: {0}", pageObject.ImgUrl);
            }

            Console.WriteLine();
            Console.Write("Test Page Download...(press enter)");
            Console.ReadLine();
            Console.WriteLine("Downloading...");
            cObj.DownloadPageObjects(0);
            Console.Write("Done...(press enter)");
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
            ISiteExtension ise = Extentions[chapterObject.Locations[LocationId].SiteExtensionName];
            ISiteExtensionDescriptionAttribute isea = ise.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);

            List<PageObject> ParsedPages = new List<PageObject>();
            foreach (PageObject pageObject in chapterObject.Pages)
            {
                HttpWebRequest request = WebRequest.Create(pageObject.Url) as HttpWebRequest;
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
            chapterObject.Pages = ParsedPages;
        }

        public static void DownloadPageObjects(this ChapterObject chapterObject, Int32 LocationId = 0)
        {
            ISiteExtension ise = Extentions[chapterObject.Locations[LocationId].SiteExtensionName];
            ISiteExtensionDescriptionAttribute isea = ise.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);

            List<PageObject> ParsedPages = new List<PageObject>();
            foreach (PageObject pageObject in chapterObject.Pages)
            {
                HttpWebRequest request = WebRequest.Create(pageObject.ImgUrl) as HttpWebRequest;
                request.Referer = isea.RefererHeader ?? request.Host;
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    DrawProgressBar(String.Format("[{1}]Downloading: {0}", pageObject.Name, pageObject.PageNumber), 0, (Int32)response.ContentLength, 60);
                    using (Stream imgStream = new MemoryStream())
                    {
                        using (Stream webStream = response.GetResponseStream())
                        {
                            try
                            {
                                int read, bufferSize = 4 * 1024;
                                byte[] buffer = new byte[bufferSize];
                                while ((read = webStream.Read(buffer, 0, bufferSize)) > 0)
                                {
                                    imgStream.Write(buffer, 0, read);
                                    DrawProgressBar(String.Format("[{1}]Downloading: {0}", pageObject.Name, pageObject.PageNumber), (Int32)imgStream.Position, (Int32)response.ContentLength, 60);
                                }
                            }
                            catch (Exception ex)
                            {
                                DrawProgressBar(String.Format("[{1}]Error: {0}", pageObject.Name, pageObject.PageNumber), (Int32)imgStream.Position, (Int32)response.ContentLength, 60);
                            }
                        }
                        if (imgStream.CanSeek)
                            imgStream.Seek(0, SeekOrigin.Begin);
                        Console.WriteLine();
                        Console.Write("\tSaving: {0}...", pageObject.Name);
                        imgStream.SaveStreamToArchive(chapterObject.Name + ".mca", pageObject.Name, new Ionic.Zip.ReadOptions());
                        Console.WriteLine("Saved");
                    }
                }
            }
            chapterObject.Pages = ParsedPages;
        }

        private static void DrawProgressBar(String text, int complete, int maxVal, int? barSize = null, string speed = null)
        {
            Int32 pos = Console.CursorLeft, barWidth = barSize ?? Console.BufferWidth;
            if (barWidth >= Console.BufferWidth)
                --barWidth;
            text = text.PadRight(barWidth, ' ');
            if (speed != null)
            {
                string spd = (speed.ToString() ?? String.Empty) + "          ";
                text = text.Remove(text.Length - spd.Length) + spd;
            }
            Console.CursorVisible = false;
            decimal perc = (decimal)complete / (decimal)maxVal;
            String percStr = (perc * 100).ToString("F2") + "%";
            text = text.Remove(text.Length - percStr.Length) + percStr;
            int chars = (int)Math.Ceiling(perc / ((decimal)1 / (decimal)barWidth));

            Console.CursorLeft = 0;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.White;
            Console.Write(text.Substring(0, chars));
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write(text.Substring(chars));
            Console.CursorLeft = pos;

            Console.ResetColor();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Reflection;
using myMangaSiteExtension;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Objects;
using Core.IO;
using Core.Other;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Utilities;
using HtmlAgilityPack;
using System.Text;

namespace TestApp
{
    static class Program
    {
        static Dictionary<String, ISiteExtension> SiteExtentions = new Dictionary<String, ISiteExtension>();
        static Dictionary<String, IDatabaseExtension> DatabaseExtentions = new Dictionary<String, IDatabaseExtension>();

        static void Main(string[] args)
        {
            //SiteExtentions.Add("MangaReader", new AFTV_Network.MangaReader());
            SiteExtentions.Add("MangaPanda", new AFTV_Network.MangaPanda());
            SiteExtentions.Add("MangaHere", new MangaHere.MangaHere());
            //SiteExtentions.Add("Batoto", new Batoto.Batoto());
            //SiteExtentions.Add("Batoto-Spanish", new Batoto.Batoto_Spanish());
            //SiteExtentions.Add("Batoto-German", new Batoto.Batoto_German());
            //SiteExtentions.Add("Batoto-French", new Batoto.Batoto_French());
            DatabaseExtentions.Add("AnimeNewsNetwork", new AnimeNewsNetwork.AnimeNewsNetwork());
            DatabaseExtentions.Add("MangaUpdatesBakaUpdates", new MangaUpdatesBakaUpdates.MangaUpdatesBakaUpdates());
            foreach (ISiteExtension ise in SiteExtentions.Values)
            {
                ISiteExtensionDescriptionAttribute isea = ise.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);
                Console.WriteLine("Loaded Site Extention {0}", isea.Name);
            }
            foreach (IDatabaseExtension ise in DatabaseExtentions.Values)
            {
                IDatabaseExtensionDescriptionAttribute isea = ise.GetType().GetCustomAttribute<IDatabaseExtensionDescriptionAttribute>(false);
                Console.WriteLine("Loaded Database Extention {0}", isea.Name);
            }
            Search();
        }

        static void Search()
        {
            Console.Write("Search Term: ");
            String SearchTerm = Console.ReadLine();
            List<MangaObject> SearchResults = new List<MangaObject>();
            while (SearchTerm != null && SearchTerm != String.Empty)
            {
                if (SearchResults.Count > 0 && SearchTerm.StartsWith("`"))
                {
                    Int32 srIndex = Int32.Parse(SearchTerm.Substring(1));
                    MangaObject mObj = SearchResults[srIndex];
                    mObj.LoadMangaObject();
                    mObj.SortChapters();
                    mObj.SaveToArchive(String.Format("{0}.ma", mObj.Name).SafeFileName(), "MangaObject", SaveType.XML);
                }
                else
                {
                    Dictionary<String, List<SearchResultObject>> RawSearchResults = new Dictionary<String, List<SearchResultObject>>();
                    foreach (ISiteExtension ise in SiteExtentions.Values)
                    {
                        ISiteExtensionDescriptionAttribute isea = ise.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);
                        SearchRequestObject sro = ise.GetSearchRequestObject(searchTerm: SearchTerm);
                        Console.Write("Searching {0}...", isea.Name);

                        HttpWebRequest request = WebRequest.Create(sro.Url) as HttpWebRequest;
                        request.Referer = sro.Referer ?? request.Host;
                        request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                        switch (sro.Method)
                        {
                            default:
                            case myMangaSiteExtension.Enums.SearchMethod.GET:
                                request.Method = "GET";
                                break;

                            case myMangaSiteExtension.Enums.SearchMethod.POST:
                                request.Method = "POST";
                                request.ContentType = "application/x-www-form-urlencoded";
                                using (var requestWriter = new StreamWriter(request.GetRequestStream()))
                                { requestWriter.Write(sro.RequestContent); }
                                break;
                        }

                        try
                        {
                            try
                            {
                                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                                {
                                    using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                                    {
                                        foreach (SearchResultObject searchResultObject in ise.ParseSearch(streamReader.ReadToEnd()))
                                        {
                                            String keyName = new String(searchResultObject.Name.ToLower().Where(Char.IsLetterOrDigit).ToArray());
                                            if (!RawSearchResults.ContainsKey(keyName))
                                                RawSearchResults[keyName] = new List<SearchResultObject>();
                                            RawSearchResults[keyName].Add(searchResultObject);
                                        }
                                    }
                                }
                            }
                            catch (WebException ex)
                            {
                                using (HttpWebResponse response = ex.Response as HttpWebResponse)
                                {
                                    using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                                    {
                                        foreach (SearchResultObject searchResultObject in ise.ParseSearch(streamReader.ReadToEnd()))
                                        {
                                            String keyName = new String(searchResultObject.Name.ToLower().Where(Char.IsLetterOrDigit).ToArray());
                                            if (!RawSearchResults.ContainsKey(keyName))
                                                RawSearchResults[keyName] = new List<SearchResultObject>();
                                            RawSearchResults[keyName].Add(searchResultObject);
                                        }
                                    }
                                }
                            }
                            Console.WriteLine("Done!");
                        }
                        catch
                        {
                            Console.WriteLine("Timeout!");
                        }
                    }

                    Dictionary<String, List<DatabaseObject>> RawDatabaseSearchResults = new Dictionary<String, List<DatabaseObject>>();
                    foreach (IDatabaseExtension ide in DatabaseExtentions.Values)
                    {
                        IDatabaseExtensionDescriptionAttribute idea = ide.GetType().GetCustomAttribute<IDatabaseExtensionDescriptionAttribute>(false);
                        String SearchURL = ide.GetSearchUri(searchTerm: SearchTerm);
                        Console.Write("Searching {0}...", idea.Name);

                        HttpWebRequest request = WebRequest.Create(SearchURL) as HttpWebRequest;
                        request.Referer = idea.RefererHeader ?? request.Host;
                        request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                        try
                        {
                            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                            {
                                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                                {
                                    foreach (DatabaseObject searchResultObject in ide.ParseSearch(streamReader.ReadToEnd()))
                                    {
                                        String keyName = new String(searchResultObject.Name.ToLower().Where(Char.IsLetterOrDigit).ToArray());
                                        if (!RawDatabaseSearchResults.ContainsKey(keyName))
                                            RawDatabaseSearchResults[keyName] = new List<DatabaseObject>();
                                        RawDatabaseSearchResults[keyName].Add(searchResultObject);
                                    }
                                }
                            }
                            Console.WriteLine("Done!");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Timeout!");
                        }
                    }

                    SearchResults.Clear();
                    foreach (String key in RawSearchResults.Keys)
                    {
                        List<SearchResultObject> SearchResultObjects = RawSearchResults[key];
                        MangaObject mangaObject = new MangaObject();
                        mangaObject.Merge(from SearchResultObject searchResultObject in SearchResultObjects select searchResultObject.ConvertToMangaObject());
                        if (RawDatabaseSearchResults.ContainsKey(key))
                        {
                            List<DatabaseObject> DatabaseObjects = RawDatabaseSearchResults[key];
                            mangaObject.AttachDatabase(DatabaseObjectExtensions.Merge(DatabaseObjects));
                        }
                        SearchResults.Add(mangaObject);
                    }

                    Console.WriteLine(String.Format("Search Term:{0}\n\tResults Found: {1}", SearchTerm, RawSearchResults.Count));
                    int i = 0;
                    foreach (MangaObject SearchResult in SearchResults)
                    {
                        Console.WriteLine(String.Format("[{0}]Name: {1}", i++, SearchResult.Name));
                        Console.WriteLine(String.Format("\tUrl: {0}", String.Join("\n\t     ", (from LocationObject location in SearchResult.Locations select location.Url).ToArray())));
                        Console.WriteLine(String.Format("\tDatabase: {0}", String.Join("\n\t          ", (from LocationObject location in SearchResult.DatabaseLocations select location.Url).ToArray())));
                        Console.WriteLine(String.Format("\tCover Url: {0}", String.Join("\n\t           ", SearchResult.Covers)));
                        Console.WriteLine(String.Format("\tDescription: {0}", String.Join("\n\t           ", SearchResult.Description)));
                        Console.WriteLine(String.Format("\tReleased: {0}", String.Join("\n\t           ", SearchResult.Released.ToLongDateString())));
                    }
                }
                Console.WriteLine();
                Console.WriteLine("`(index) to download info.");
                Console.WriteLine("Empty Search Term Exits Application.");
                Console.Write("Search Term: ");
                SearchTerm = Console.ReadLine();
            }
        }

        static void LoadManga()
        {
            MangaObject mObj = LoadMangaObject("http://www.mangahere.com/manga/fairy_tail/", SiteExtentions["MangaHere"]);
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

            ChapterObject cObj = mObj.Chapters.Last();
            cObj.Pages = LoadChapterObject(cObj, 0).Pages;
            Console.WriteLine("Returned ChapterObject:");
            Console.WriteLine("\tName:{0}", cObj.Name);
            Console.WriteLine("\tChapter:{0}", cObj.Chapter);
            Console.WriteLine("\tReleased:{0}", cObj.Released.ToString("d"));
            Console.WriteLine("\tNumber of Pages:{0}", cObj.Pages.Count);

            Console.WriteLine();
            Console.Write("Test Page Load...(press enter)");
            Console.ReadLine();
            Console.WriteLine("Loading...");
            cObj.LoadPageObjects(0);
            cObj.SaveToArchive(cObj.Name + ".xml.mca", "ChapterObject", SaveType.XML);
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

        static MangaObject LoadMangaObject(String Link, ISiteExtension ise)
        {
            MangaObject MangaObj = null;
            ISiteExtensionDescriptionAttribute isea = ise.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);

            HttpWebRequest request = WebRequest.Create(Link) as HttpWebRequest;
            request.Referer = isea.RefererHeader ?? request.Host;
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    MangaObj = ise.ParseMangaObject(streamReader.ReadToEnd());
                    MangaObj.Locations.Add(new LocationObject() { ExtensionName = isea.Name, Url = Link });
                }
            }
            return MangaObj;
        }

        static void LoadMangaObject(this MangaObject MangaObj)
        {
            foreach (LocationObject LocationObj in MangaObj.Locations.FindAll(l => l.Enabled))
            {
                ISiteExtension ise = SiteExtentions[LocationObj.ExtensionName];
                ISiteExtensionDescriptionAttribute isea = ise.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);

                HttpWebRequest request = WebRequest.Create(LocationObj.Url) as HttpWebRequest;
                request.Referer = isea.RefererHeader ?? request.Host;
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    Console.Write("Loading Manga from {0}...", isea.Name);
                    using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        Console.WriteLine("Done!");
                        Console.Write("Parsing Manga from {0}...", isea.Name);
                        MangaObject dmObj = ise.ParseMangaObject(streamReader.ReadToEnd());
                        LocationObj.Enabled = dmObj != null;
                        if (LocationObj.Enabled)
                            MangaObj.Merge(dmObj);
                        Console.WriteLine("Done!");
                    }
                }
            }
        }

        static ChapterObject LoadChapterObject(String Link)
        {
            ChapterObject ChapterObj = null;
            ISiteExtension ise = SiteExtentions["MangaReader"];
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
            ISiteExtension ise = SiteExtentions[chapterObject.Locations[LocationId].ExtensionName];
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
            ISiteExtension ise = SiteExtentions[chapterObject.Locations[LocationId].ExtensionName];
            ISiteExtensionDescriptionAttribute isea = ise.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);

            List<PageObject> ParsedPages = new List<PageObject>();
            DrawProgressBar(String.Format("Parsing: {0}", chapterObject.Name), 0, chapterObject.Pages.Count, 60);
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
                        DrawProgressBar(String.Format("Parsing: {0}", chapterObject.Name), ParsedPages.Count, chapterObject.Pages.Count, 60);
                    }
                }
            }
            Console.WriteLine();
            chapterObject.Pages = ParsedPages;
        }

        public static void DownloadPageObjects(this ChapterObject chapterObject, Int32 LocationId = 0)
        {
            ISiteExtension ise = SiteExtentions[chapterObject.Locations[LocationId].ExtensionName];
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
                        imgStream.SaveStreamToArchive(chapterObject.Name + ".xml.mca", pageObject.Name, new Ionic.Zip.ReadOptions());
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

        /// <summary>
        /// Longitudinal Redundancy Check (LRC) calculator for a byte array. 
        /// This was proved from the LRC Logic of Edwards TurboPump Controller SCU-1600.
        /// ex) DATA (hex 6 bytes): 02 30 30 31 23 03
        ///     LRC  (hex 1 byte ): 47        
        /// </summary>

        public static byte calculateLRC(byte[] bytes)
        {
            byte LRC = 0x00;
            for (int i = 0; i < bytes.Length; i++)
            {
                LRC = (byte)((LRC + bytes[i]) & 0xFF);
            }
            return (byte)(((LRC ^ 0xFF) + 1) & 0xFF);
        }
    }
}

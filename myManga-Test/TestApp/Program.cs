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
using Core.IO.Storage.Manager.BaseInterfaceClasses;
using Core.IO.Storage;
using System.Threading.Tasks;
using System.Threading;

namespace TestApp
{
    static class Program
    {
        static Dictionary<String, ISiteExtension> SiteExtentions = new Dictionary<String, ISiteExtension>();
        static Dictionary<String, IDatabaseExtension> DatabaseExtentions = new Dictionary<String, IDatabaseExtension>();
        static ZipStorage zip_storage;
        static ZipManager zipManager;
        static object _lock = new object();

        static void Main(string[] args)
        {
            MangaObject test = new MangaObject();
            test.Name = "Test";
            test.MangaType = myMangaSiteExtension.Enums.MangaObjectType.Unknown;

            Stream test_stream_1 = test.Serialize(SaveType.Binary);
            MangaObject test2 = test_stream_1.Deserialize<MangaObject>(SaveType.Binary);


            zip_storage = Core.Other.Singleton.Singleton<ZipStorage>.Instance;
            zipManager = Core.Other.Singleton.Singleton<ZipManager>.Instance;

            //SiteExtentions.Add("MangaReader", new AFTV_Network.MangaReader());
            //SiteExtentions.Add("MangaPanda", new AFTV_Network.MangaPanda());
            //SiteExtentions.Add("MangaHere", new MangaHere.MangaHere());
            //SiteExtentions.Add("Batoto", new Batoto.Batoto());
            SiteExtentions.Add("MangaTraders", new MangaTraders.MangaTraders());
            //SiteExtentions.Add("Batoto-Spanish", new Batoto.Batoto_Spanish());
            //SiteExtentions.Add("Batoto-German", new Batoto.Batoto_German());
            //SiteExtentions.Add("Batoto-French", new Batoto.Batoto_French());
            DatabaseExtentions.Add("MangaHelpers", new MangaHelpers.MangaHelpers());
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
            //Authenticate();
            //LoadManga();
            //Search();
            LoadMangaAsync().Wait();
            zip_storage.Dispose();
            zipManager.Dispose();
        }

        static void Authenticate()
        {
            Console.Write("Username: ");
            String Username = Console.ReadLine();
            Console.Write("Password: ");
            String Password = Console.ReadLine();

            CancellationTokenSource cts = new CancellationTokenSource();
            Boolean authenticated = SiteExtentions["Batoto"].Authenticate(new NetworkCredential(Username, Password), cts.Token, null);
            Console.WriteLine("Authenticated: " + (authenticated ? "Success" : "Failed"));

            Console.WriteLine("Testing manga loading...");
            MangaObject mObj = LoadMangaObject("https://bato.to/comic/_/comics/no-guns-life-r13414", SiteExtentions["Batoto"]);
            Console.WriteLine("Returned MangaObject:");
            Console.WriteLine("\tName:{0}", mObj.Name);
            Console.WriteLine("\tReleased:{0}", mObj.Released.ToString("yyyy"));
            Console.WriteLine("\tAlternate Names:{0}", String.Join(", ", mObj.AlternateNames));
            Console.WriteLine("\tAuthors:{0}", String.Join(", ", mObj.Authors));
            Console.WriteLine("\tArtists:{0}", String.Join(", ", mObj.Artists));
            Console.WriteLine("\tGenres:{0}", String.Join(", ", mObj.Genres));
            Console.WriteLine("\tLocations:{0}", String.Join(", ", mObj.Locations));
            Console.WriteLine("\tNumber of Chapters:{0}", mObj.Chapters.Count);
            Console.WriteLine("\tDescription:{0}", mObj.Description);

            Console.WriteLine();
            foreach (ChapterObject cObj in mObj.Chapters)
            {
                Console.WriteLine("\tName:{0}", cObj.Name);
                Console.WriteLine("\tChapter:{0}", cObj.Chapter);
                Console.WriteLine("\tReleased:{0}", cObj.Released.ToString("d"));
                Console.WriteLine();
            }

            SiteExtentions["Batoto"].Deauthenticate();
            Console.Write("Done...(press enter)");
            Console.ReadLine();
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
                    zip_storage.Write(String.Format("{0}/{1}", Environment.CurrentDirectory, String.Format("{0}.ma", mObj.Name).SafeFileName()), "MangaObject", mObj.Serialize(SaveType.XML));
                    //mObj.SaveToArchive(String.Format("{0}.ma", mObj.Name).SafeFileName(), "MangaObject", SaveType.XML);
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
                        SearchRequestObject SearchRequestObject = ide.GetSearchRequestObject(searchTerm: SearchTerm);
                        Console.Write("Searching {0}...", idea.Name);

                        HttpWebRequest request = WebRequest.Create(SearchRequestObject.Url) as HttpWebRequest;
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
                        catch
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
                        Console.WriteLine(String.Format("\tCover Url: {0}", String.Join("\n\t           ", SearchResult.CoverLocations)));
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
            MangaObject mObj = LoadMangaObject("http://mangatraders.org/read-online/TheGamer", SiteExtentions["MangaTraders"]);
            Console.WriteLine("Returned MangaObject:");
            Console.WriteLine("\tName:{0}", mObj.Name);
            Console.WriteLine("\tReleased:{0}", mObj.Released.ToString("yyyy"));
            Console.WriteLine("\tAlternate Names:{0}", String.Join(", ", mObj.AlternateNames));
            Console.WriteLine("\tAuthors:{0}", String.Join(", ", mObj.Authors));
            Console.WriteLine("\tArtists:{0}", String.Join(", ", mObj.Artists));
            Console.WriteLine("\tGenres:{0}", String.Join(", ", mObj.Genres));
            Console.WriteLine("\tLocations:{0}", String.Join(", ", mObj.Locations));
            Console.WriteLine("\tNumber of Chapters:{0}", mObj.Chapters.Count);
            Console.WriteLine("\tDescription:{0}", mObj.Description);

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
            zip_storage.Write(cObj.Name + ".xml.mca", "ChapterObject", cObj.Serialize(SaveType.XML));
            //cObj.SaveToArchive(cObj.Name + ".xml.mca", "ChapterObject", SaveType.XML);
            Console.WriteLine("Returned ChapterObject:");
            //*
            foreach (PageObject pageObject in cObj.Pages)
            {
                Console.WriteLine("\t[{0}]:", pageObject.PageNumber);
                Console.WriteLine("\t\tUrl: {0}", pageObject.Url);
                Console.WriteLine("\t\tImage: {0}", pageObject.ImgUrl);
            }
            //*/
            Console.WriteLine();
            Console.Write("Test Page Download...(press enter)");
            Console.ReadLine();
            Console.WriteLine("Downloading...");
            cObj.DownloadPageObjects(0);
            Console.Write("Done...(press enter)");
            Console.ReadLine();
        }

        static async Task LoadMangaAsync()
        {
            String mangaUrl = "http://mangatraders.org/read-online/TheGamer";
            ISiteExtension extension = SiteExtentions["MangaTraders"];
            CancellationTokenSource cts = new CancellationTokenSource();
            Boolean written = false;
            Progress<int> progress = new Progress<int>(percent => { DrawProgressBarTopWindow(percent, 100, "Progress"); });

            Console.WriteLine();
            Console.Write("Test Loading MangaObject via Async...(press enter)");
            Console.ReadLine();

            Console.WriteLine("Loading Manga vis Async...");
            MangaObject MangaObject = await LoadMangaObjectAsync(mangaUrl, extension, cts.Token, progress);
            lock (_lock)
            {
                Console.WriteLine();
                Console.WriteLine("[MangaObject] Name:{0}", MangaObject.Name);
                Console.WriteLine("[MangaObject] Released:{0}", MangaObject.Released.ToString("yyyy"));
                Console.WriteLine("[MangaObject] Alternate Names:{0}", String.Join(", ", MangaObject.AlternateNames));
                Console.WriteLine("[MangaObject] Authors:{0}", String.Join(", ", MangaObject.Authors));
                Console.WriteLine("[MangaObject] Artists:{0}", String.Join(", ", MangaObject.Artists));
                Console.WriteLine("[MangaObject] Genres:{0}", String.Join(", ", MangaObject.Genres));
                Console.WriteLine("[MangaObject] Locations:{0}", String.Join(", ", MangaObject.Locations));
                Console.WriteLine("[MangaObject] Number of Chapters:{0}", MangaObject.Chapters.Count);
                Console.WriteLine("[MangaObject] Description:{0}", MangaObject.Description);

                Console.WriteLine();
                Console.Write("Test Loading ChapterObject via Async...(press enter)");
            }
            Console.ReadLine();
            ChapterObject ChapterObject = MangaObject.Chapters.Last();
            await ChapterObject.LoadChapterObjectAsync(cts.Token, 0, progress);
            lock (_lock)
            {
                Console.WriteLine();
                Console.WriteLine("[ChapterObject] Name:{0}", ChapterObject.Name);
                Console.WriteLine("[ChapterObject] Released:{0}", ChapterObject.Released.ToString("yyyy"));
                Console.WriteLine("[ChapterObject] V.Ch.SCh:{0}.{1}.{2}", ChapterObject.Volume, ChapterObject.Chapter, ChapterObject.SubChapter);
                Console.WriteLine("[ChapterObject] Page Count:{0}", ChapterObject.Pages.Count);

                Console.WriteLine();
                Console.Write("Test Loading ChapterObject's PageObjects and Images via Async...(press enter)");
            }
            Console.ReadLine();
            String StorageFileName = ChapterObject.Volume + "." + ChapterObject.Chapter + "." + ChapterObject.SubChapter + ".ca.zip";
            written = await zipManager.Retry(async () =>
            {
                return await zipManager.WriteAsync(StorageFileName, "ChapterObject", ChapterObject.Serialize(SaveType.XML));
            }, TimeSpan.FromMinutes(30));
            lock (_lock)
            {
                Console.WriteLine();
                Console.WriteLine("[ChapterObject] Saved ChapterObject: {0}", written ? "Success" : "Failed");
            }

            Int32 concurrent = Environment.ProcessorCount * 2;
            SemaphoreSlim semaphore = new SemaphoreSlim(0, concurrent);
            List<Task> PageLoadTasks = new List<Task>();
            for (Int32 idx = 0; idx < ChapterObject.Pages.Count; ++idx)
            {
                Int32 index = idx;
                PageLoadTasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    Console.WriteLine("[PageObject] Working on Index: {0}", index);
                    ChapterObject.Pages[index] = await LoadPageObjectAsync(ChapterObject, ChapterObject.Pages[index], cts.Token, 0, progress);
                    using (Stream imgStream = await LoadPageObjectImageAsync(ChapterObject, ChapterObject.Pages[index], cts.Token, 0, progress))
                    {
                        written = await zipManager.Retry(async () =>
                        {
                            return await zipManager.WriteAsync(StorageFileName, "ChapterObject", ChapterObject.Serialize(SaveType.XML));
                        }, TimeSpan.FromMinutes(30));
                        lock (_lock)
                        {
                            Console.WriteLine();
                            Console.WriteLine("[ChapterObject] Saved ChapterObject: {0}", written ? "Success" : "Failed");
                        }

                        written = await zipManager.Retry(async () =>
                        {
                            return await zipManager.WriteAsync(StorageFileName, ChapterObject.Pages[index].Name, imgStream);
                        }, TimeSpan.FromMinutes(30));
                        lock (_lock)
                        {
                            Console.WriteLine();
                            Console.WriteLine("[PageObject] Saved Image: {0}", written ? "Success" : "Failed");
                        }
                    }
                    semaphore.Release();
                }));
            }
            lock (_lock)
            {
                Console.WriteLine();
                Console.Write("Ready to test concurrent page loading [{0}]...(press enter)", concurrent);
                Console.ReadLine();
                semaphore.Release(concurrent);
                Console.Write("Waiting for pages...");
            }
            Task.WaitAll(PageLoadTasks.ToArray());

            Console.WriteLine();
            Console.Write("Test Loading via Async Complete...(press enter)");
            Console.ReadLine();
        }

        static MangaObject LoadMangaObject(String Link, ISiteExtension ise)
        {
            MangaObject MangaObj = null;
            ISiteExtensionDescriptionAttribute isea = ise.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);

            HttpWebRequest request = WebRequest.Create(Link) as HttpWebRequest;
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(ise.Cookies);
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
            List<Task> pageTasks = new List<Task>();
            foreach (PageObject pageObject in chapterObject.Pages)
            {
                pageTasks.Add(Task.Run(async () =>
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
                                    while ((read = await webStream.ReadAsync(buffer, 0, bufferSize)) > 0)
                                    {
                                        await imgStream.WriteAsync(buffer, 0, read);
                                        DrawProgressBar(String.Format("[{1}]Downloading: {0}", pageObject.Name, pageObject.PageNumber), (Int32)imgStream.Position, (Int32)response.ContentLength, 60);
                                    }
                                }
                                catch
                                {
                                    DrawProgressBar(String.Format("[{1}]Error: {0}", pageObject.Name, pageObject.PageNumber), (Int32)imgStream.Position, (Int32)response.ContentLength, 60);
                                }
                            }
                            if (imgStream.CanSeek)
                                imgStream.Seek(0, SeekOrigin.Begin);
                            Console.WriteLine();
                            Console.Write("\tSaving: {0}...", pageObject.Name);
                            Boolean written = await zipManager.Retry(async () =>
                            {
                                return await zipManager.WriteAsync(chapterObject.Volume + "." + chapterObject.Chapter + "." + chapterObject.SubChapter + ".ca.zip", pageObject.Name, imgStream);
                            }, TimeSpan.FromMinutes(30));
                            // imgStream.SaveStreamToArchive(chapterObject.Name + ".xml.mca", pageObject.Name, new Ionic.Zip.ReadOptions());
                            Console.WriteLine(written ? "Saved" : "Not Saved");
                        }
                    }
                }));
            }
            Task.WaitAll(pageTasks.ToArray());
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

        private static void DrawProgressBarTopWindow(Int32 progress, Int32 total, String content = "")
        {
            Decimal perc = (Decimal)progress / (Decimal)total;
            String percStr = (perc * 100).ToString("F2") + "%";
            content = content.PadRight(Console.BufferWidth, ' ');
            content = content.Remove(content.Length - percStr.Length) + percStr;
            int chars = (int)Math.Ceiling(perc / ((Decimal)1 / (Decimal)Console.BufferWidth));

            // Start of draw
            lock (_lock)
            {
                Boolean initCursorVisible = Console.CursorVisible;
                Int32 initLeft = Console.CursorLeft, initTop = Console.CursorTop;
                ConsoleColor initForeground = Console.ForegroundColor, initBackground = Console.BackgroundColor;

                Console.CursorVisible = false;
                Console.SetCursorPosition(0, 0);
                Console.Write(new String(' ', Console.BufferWidth));
                Console.SetCursorPosition(0, 0);
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.White;
                Console.Write(content.Substring(0, chars));
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Write(content.Substring(chars));

                Console.SetCursorPosition(initLeft, initTop);
                Console.CursorVisible = initCursorVisible;
                Console.ForegroundColor = initForeground;
                Console.BackgroundColor = initBackground;
            }
        }

        #region Async
        private static async Task<MangaObject> LoadMangaObjectAsync(String Link, ISiteExtension ise)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            return await LoadMangaObjectAsync(Link, ise, cts.Token);
        }

        private static async Task<MangaObject> LoadMangaObjectAsync(String Link, ISiteExtension ise, CancellationToken ct, IProgress<int> progress = null)
        {
            using (WebDownloader wD = new WebDownloader(ise.Cookies))
            {
                wD.Referer = ise.SiteExtensionDescriptionAttribute.RefererHeader;
                ct.ThrowIfCancellationRequested();
                if (progress != null) progress.Report(10);
                String content = await wD.DownloadStringTaskAsync(Link);
                ct.ThrowIfCancellationRequested();
                if (progress != null) progress.Report(50);
                return await Task.Run(() =>
                {
                    if (progress != null) progress.Report(75);
                    MangaObject MangaObject = ise.ParseMangaObject(content);
                    ct.ThrowIfCancellationRequested();
                    if (progress != null) progress.Report(90);
                    MangaObject.Locations.Add(new LocationObject()
                    {
                        ExtensionName = ise.SiteExtensionDescriptionAttribute.Name,
                        Url = Link
                    });
                    ct.ThrowIfCancellationRequested();
                    if (progress != null) progress.Report(100);
                    return MangaObject;
                });
            }
        }

        private static async Task LoadChapterObjectAsync(this ChapterObject ChapterObject, Int32 LocationId = 0)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            await LoadChapterObjectAsync(ChapterObject, cts.Token, LocationId, null);
        }

        private static async Task LoadChapterObjectAsync(this ChapterObject ChapterObject, CancellationToken ct, Int32 LocationId = 0, IProgress<int> progress = null)
        {
            ISiteExtension ise = SiteExtentions[ChapterObject.Locations[LocationId].ExtensionName];
            using (WebDownloader wD = new WebDownloader(ise.Cookies))
            {
                wD.Referer = ise.SiteExtensionDescriptionAttribute.RefererHeader;
                ct.ThrowIfCancellationRequested();
                if (progress != null) progress.Report(10);
                String content = await wD.DownloadStringTaskAsync(ChapterObject.Locations[LocationId].Url);
                ct.ThrowIfCancellationRequested();
                if (progress != null) progress.Report(50);
                await Task.Run(() =>
                {
                    ct.ThrowIfCancellationRequested();
                    if (progress != null) progress.Report(75);
                    ChapterObject.Merge(ise.ParseChapterObject(content));
                    ct.ThrowIfCancellationRequested();
                    if (progress != null) progress.Report(100);
                });
            }
        }

        private static async Task<PageObject> LoadPageObjectAsync(ChapterObject ChapterObject, PageObject PageObject, Int32 LocationId = 0)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            return await LoadPageObjectAsync(ChapterObject, PageObject, cts.Token, LocationId);
        }

        private static async Task<PageObject> LoadPageObjectAsync(ChapterObject ChapterObject, PageObject PageObject, CancellationToken ct, Int32 LocationId = 0, IProgress<int> progress = null)
        {
            ISiteExtension ise = SiteExtentions[ChapterObject.Locations[LocationId].ExtensionName];
            using (WebDownloader wD = new WebDownloader(ise.Cookies))
            {
                wD.Referer = ise.SiteExtensionDescriptionAttribute.RefererHeader;
                ct.ThrowIfCancellationRequested();
                if (progress != null) progress.Report(10);
                String content = await wD.DownloadStringTaskAsync(PageObject.Url);
                ct.ThrowIfCancellationRequested();
                if (progress != null) progress.Report(50);
                return await Task.Run(() =>
                {
                    ct.ThrowIfCancellationRequested();
                    if (progress != null) progress.Report(100);
                    return ise.ParsePageObject(content);
                });
            }
        }

        private static async Task<Stream> LoadPageObjectImageAsync(ChapterObject ChapterObject, PageObject PageObject, CancellationToken ct, Int32 LocationId = 0, IProgress<int> progress = null)
        {
            ISiteExtension ise = SiteExtentions[ChapterObject.Locations[LocationId].ExtensionName];
            using (WebDownloader wD = new WebDownloader(ise.Cookies))
            {
                wD.Referer = ise.SiteExtensionDescriptionAttribute.RefererHeader;
                ct.ThrowIfCancellationRequested();
                if (progress != null) progress.Report(10);
                Stream content = new MemoryStream();
                using (Stream webStream = await wD.OpenReadTaskAsync(PageObject.ImgUrl))
                {
                    if (progress != null) progress.Report(75);
                    await webStream.CopyToAsync(content);
                }
                content.Seek(0, SeekOrigin.Begin);
                if (progress != null) progress.Report(100);
                return content;
            }
        }
        #endregion

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

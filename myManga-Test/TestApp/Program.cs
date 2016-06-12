using myManga_App.IO.DLL;
using myManga_App.IO.Local;
using myManga_App.IO.Network;
using myManga_App.IO.Local.Object;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Collections;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using myManga_App.Objects.UserConfig;
using myManga_App;

namespace TestApp
{
    static class Program
    {
        static object _lock = new object();

        static void Main(string[] args)
        {
            using (Tests t = new Tests())
            {
                t.Run().Wait();
                Console.Write("Test program complete. Press 'Enter' to close.");
                Console.Read();
            }
        }

        public static void DrawProgressBar(String text, int complete, int maxVal, int? barSize = null, string speed = null)
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
            int chars = (int)Math.Floor(perc / ((decimal)1 / (decimal)barWidth));

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

        public static void DrawProgressBarTopWindow(Int32 progress, Int32 total, String content = "")
        {
            Decimal perc = (Decimal)progress / (Decimal)total;
            String percStr = (perc * 100).ToString("F2") + "%";
            content = content.PadRight(Console.BufferWidth, ' ');
            content = content.Remove(content.Length - percStr.Length) + percStr;
            int chars = (int)Math.Floor(perc / ((Decimal)1 / (Decimal)Console.BufferWidth));

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

    class Tests : IDisposable
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(typeof(Program));
        private static readonly CoreManagement CORE = new CoreManagement(logger);

        String[] EnabledPlugins = {
            "MangaTown"
        };

        #region Logging
        private void ConfigureLog4Net(log4net.Core.Level LogLevel = null)
        {
            if (Equals(LogLevel, null))
                LogLevel = log4net.Core.Level.All;
            log4net.Appender.ColoredConsoleAppender appender = new log4net.Appender.ColoredConsoleAppender();
            appender.Layout = new log4net.Layout.SimpleLayout();
            appender.Threshold = LogLevel;
            appender.Name = "TESTING";
            appender.ActivateOptions();
            log4net.Config.BasicConfigurator.Configure(appender);
        }
        #endregion

        #region IO Management
        private readonly ContentDownloadManager ContentDownloadManager;

        private void InitializeDLLs()
        {
            CORE.UpdateUserConfiguration(new UserConfigurationObject()
            {
                EnabledExtensions = CORE.Extensions.Select(Extension => new EnabledExtensionObject(Extension)
                {
                    Enabled = EnabledPlugins.Contains(Extension.ExtensionDescriptionAttribute.Name)
                }).ToList()
            });
            logger.InfoFormat("Loading Plugins...");
            foreach (EnabledExtensionObject ee in CORE.UserConfiguration.EnabledExtensions)
            {
                logger.InfoFormat("* [{2}] Loaded {0} ({1})", ee.Name, ee.Language, ee.Enabled ? "Enabled" : "Disabled");
            }
            logger.InfoFormat("Loading Plugins...DONE!");
        }

        #endregion

        private readonly Dictionary<String, Func<String, Task>> TestMethods;

        private List<MangaObject> SearchResults;

        public Tests()
        {
            TestMethods = new Dictionary<String, Func<String, Task>>(){
                {
                    "search",
                    async (SearchTerm) =>
                    {
                        logger.InfoFormat("Searching for {0}...", SearchTerm);
                        SearchResults = await Test_Search(SearchTerm);
                        logger.InfoFormat("Searching for {0}...DONE!", SearchTerm);

                        Int32 idx = 0;
                        foreach(MangaObject mObj in SearchResults) {
                            logger.InfoFormat("* [{0}] {1}", idx++, mObj.Name);
                            foreach(LocationObject lObj in mObj.Locations)
                                logger.InfoFormat("** {0} ({1})", lObj.ExtensionName, lObj.ExtensionLanguage);
                        }
                    }
                },
                {
                    "download",
                    async (SearchIdxStr) =>
                    {
                        Int32 SearchIdx = 0;
                        Int32.TryParse(SearchIdxStr, out SearchIdx);
                        MangaObject MangaObject = SearchResults[SearchIdx];
                        logger.InfoFormat("Downloading Manga {0}...", MangaObject.Name);
                        await Test_DownloadMangaObject(MangaObject);

                        // Reload MangaObject from the download.
                        Stream MangaObjectStream = await CORE.ZipManager.ReadAsync(
                            ContentDownloadManager.SavePath(MangaObject),
                            typeof(MangaObject).Name
                        ).Retry(TimeSpan.FromMinutes(1));
                        if (!Equals(MangaObjectStream, null))
                        { using (MangaObjectStream) { MangaObject = MangaObjectStream.Deserialize<MangaObject>(CORE.UserConfiguration.SerializeType); } }

                        ChapterObject ChapterObject = MangaObject.Chapters.First();
                        logger.InfoFormat("Downloading Chapter {0}/{1}...", MangaObject.Name, ChapterObject.Name);
                        await Test_DownloadChapterObject(MangaObject, ChapterObject);
                    }
                }
            };

            ConfigureLog4Net();
            InitializeDLLs();
            ContentDownloadManager = new ContentDownloadManager(CORE: CORE);
        }

        public async Task Run()
        {
            Boolean run = true;
            while (run)
            {
                logger.InfoFormat("Tests:");
                foreach (String taskName in TestMethods.Keys)
                    logger.InfoFormat("* {0}", taskName);
                logger.InfoFormat("Run Test: ");
                String command = Console.ReadLine();
                switch (command.ToLower())
                {
                    default:
                        if (TestMethods.ContainsKey(command))
                        {
                            logger.InfoFormat("Test Data: ");
                            String data = Console.ReadLine();
                            logger.InfoFormat("Running test {0}...", command);
                            await TestMethods[command](data);
                            logger.InfoFormat("Running test {0}...DONE!", command);
                        }
                        else logger.InfoFormat("Unknown Test!");
                        break;
                    case "Exit":
                    case "exit":
                    case "Quit":
                    case "quit":
                    case "q":
                        run = false;
                        break;
                }
            }
        }

        #region Tests


        private async Task<List<MangaObject>> Test_Search(String SearchTerm)
        {
            IProgress<Int32> SearchProgress = new Progress<Int32>(progress => Program.DrawProgressBar("Search: " + SearchTerm, progress, 100));
            CancellationTokenSource cts = new CancellationTokenSource();
            return await ContentDownloadManager.SearchAsync(SearchTerm, cts.Token, SearchProgress);
        }

        private Task Test_DownloadMangaObject(MangaObject MangaObject)
        {
            IProgress<Int32> DownloadProgress = new Progress<Int32>(progress => Program.DrawProgressBar("Downloading Manga: " + MangaObject.Name, progress, 100));
            CancellationTokenSource cts = new CancellationTokenSource();
            return ContentDownloadManager.DownloadAsync(MangaObject, Refresh: true, ProgressReporter: DownloadProgress);
        }

        private Task Test_DownloadChapterObject(MangaObject MangaObject, ChapterObject ChapterObject)
        {
            IProgress<Int32> DownloadProgress = new Progress<Int32>(progress => Program.DrawProgressBar("Downloading Chapter: " + MangaObject.Name, progress, 100));
            CancellationTokenSource cts = new CancellationTokenSource();
            return ContentDownloadManager.DownloadAsync(MangaObject, ChapterObject, ProgressReporter: DownloadProgress);
        }

        public void Dispose()
        {
            CORE.Dispose();
            ContentDownloadManager.Dispose();
        }
        #endregion
    }
}

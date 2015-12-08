using myMangaSiteExtension.Objects;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace myManga_App.IO.Network
{
    public enum DownloadType
    {
        Unknown = 0x0,
        Manga,
        Chapter,
        Page,
        Image
    }

    public sealed class DownloadPackage
    {
        public Guid PackageId { get; private set; }
        public DownloadType PackageDownloadType { get; private set; }
        public Object PackageContent { get; set; }

        public DownloadPackage(DownloadType downloadType = DownloadType.Unknown)
        {
            this.PackageId = Guid.NewGuid();
            this.PackageDownloadType = downloadType;
        }

        public override string ToString()
        {
            return String.Format("[{0}] {1}", this.PackageId, this.PackageDownloadType.ToString());
        }
    }

    public sealed class ContentDownloadManager
    {
        private const Int32 DEFAULT_MS_WAIT = 500;     // Wait for 500ms (0.5s)
        private const Int32 RANDOM_RETRY_MS_WAIT_MIN = 100; // Random wait up to 500ms (0.5s)
        private const Int32 RANDOM_RETRY_MS_WAIT_MAX = 600; // Random wait up to 500ms (0.5s)
        private const Int32 RETRY_COUNT = 10; // Random wait up to 500ms (0.5s)

        //private readonly LimitedConcurrencyLevelTaskScheduler lcts;
        private readonly TaskFactory taskFactory;
        private readonly CancellationTokenSource cancellationTokenSource;

        private readonly App App = App.Current as App;
        private readonly List<Task> activeTasks;

        public ContentDownloadManager()
        {
            ServicePointManager.DefaultConnectionLimit = Environment.ProcessorCount * 2;

            activeTasks = new List<Task>();
            cancellationTokenSource = new CancellationTokenSource();
            //taskFactory = new TaskFactory(cancellationTokenSource.Token, null, null, lcts);
        }

        public void Download(MangaObject MangaObject)
        {
            DownloadPackage downloadPackage = new DownloadPackage(DownloadType.Manga);
            downloadPackage.PackageContent = new Object[] { MangaObject };
        }

        public void Download(MangaObject MangaObject, ChapterObject ChapterObject)
        {
            DownloadPackage downloadPackage = new DownloadPackage(DownloadType.Chapter);
            downloadPackage.PackageContent = new Object[] { MangaObject, ChapterObject };
        }

        public void Download(MangaObject MangaObject, ChapterObject ChapterObject, PageObject PageObject)
        {
            DownloadPackage downloadPackage = new DownloadPackage(DownloadType.Page);
            downloadPackage.PackageContent = new Object[] { MangaObject, ChapterObject, PageObject };
        }

        public void Download(MangaObject MangaObject, ChapterObject ChapterObject, String Url, String LocalPath, String Referer = null, String Filename = null)
        {
            DownloadPackage downloadPackage = new DownloadPackage(DownloadType.Image);
            downloadPackage.PackageContent = new Object[] { MangaObject, ChapterObject, Url, LocalPath, Referer, Filename };
        }
    }
}

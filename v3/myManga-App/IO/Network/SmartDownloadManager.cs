using Amib.Threading;
using Core.IO;
using Core.Other.Singleton;
using myManga_App.Properties;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace myManga_App.IO.Network
{
    public sealed class SmartDownloadManager
    {
        private readonly SmartMangaDownloader smd;
        private readonly SmartChapterDownloader scd;
        private readonly SmartPageDownloader spd;
        private readonly SmartImageDownloader sid;
        private readonly App App = App.Current as App;

        public Int32 Concurrency
        { get { return smd.Concurrency + scd.Concurrency + spd.Concurrency + sid.Concurrency; } }
        public Boolean IsIdle
        { get { return smd.IsIdle && scd.IsIdle && spd.IsIdle; } }

        public SmartDownloadManager() : this(null) { }
        public SmartDownloadManager(STPStartInfo stpThredPool)
        {
            smd = new SmartMangaDownloader(stpThredPool);
            smd.MangaObjectComplete += smd_MangaObjectComplete;

            scd = new SmartChapterDownloader(stpThredPool);
            scd.ChapterObjectComplete += scd_ChapterObjectComplete;
            
            spd = new SmartPageDownloader(stpThredPool);
            spd.PageObjectComplete += spd_PageObjectComplete;

            sid = new SmartImageDownloader(stpThredPool);
            sid.SmartImageDownloadObjectComplete += sid_SmartImageDownloadObjectComplete;
        }

        #region SmartMangaDownloader
        public IWorkItemResult Download(MangaObject mangaObject)
        { return smd.DownloadMangaObject(mangaObject); }

        public ICollection<IWorkItemResult> Download(ICollection<MangaObject> mangaObjects)
        { return smd.DownloadMangaObject(mangaObjects); }

        private delegate void smd_MangaObjectCompleteInvoke(object sender, MangaObject e);
        private void smd_MangaObjectComplete(object sender, MangaObject e)
        {
            if (App.Dispatcher.Thread == Thread.CurrentThread)
            {
                String save_path = Path.Combine(App.MANGA_ARCHIVE_DIRECTORY, e.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION));
                Singleton<Core.IO.Storage.Manager.BaseInterfaceClasses.ZipStorage>.Instance.Write(save_path, e.GetType().Name, e.Serialize(SaveType: Settings.Default.SaveType));
                foreach (String cover_url in e.Covers)
                { sid.Download(cover_url, save_path); }
            }
            else
                App.Dispatcher.BeginInvoke(new smd_MangaObjectCompleteInvoke(smd_MangaObjectComplete), new Object[] { sender, e });
        }
        #endregion

        #region SmartChapterDownloader
        public IWorkItemResult Download(ChapterObject chapterObject)
        { return scd.DownloadChapterObject(chapterObject); }

        public ICollection<IWorkItemResult> Download(ICollection<ChapterObject> chapterObjects)
        { return scd.DownloadChapterObject(chapterObjects); }

        private delegate void scd_ChapterObjectCompleteInvoke(object sender, ChapterObject e);
        private void scd_ChapterObjectComplete(object sender, ChapterObject e)
        {
            if (App.Dispatcher.Thread == Thread.CurrentThread)
            {
                String save_path = Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, new String(e.MangaName.Where(Char.IsLetterOrDigit).ToArray()), e.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION));
                Singleton<Core.IO.Storage.Manager.BaseInterfaceClasses.ZipStorage>.Instance.Write(save_path, e.GetType().Name, e.Serialize(SaveType: Settings.Default.SaveType));
                spd.DownloadPageObjectPages(e);
            }
            else
                App.Dispatcher.BeginInvoke(new smd_MangaObjectCompleteInvoke(smd_MangaObjectComplete), new Object[] { sender, e });
        }
        #endregion

        #region SmartPageDownloader
        private void spd_PageObjectComplete(object sender, SmartPageDownloader.PageObjectCompleted e)
        {
            String save_path = Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, new String(e.ChapterObject.MangaName.Where(Char.IsLetterOrDigit).ToArray()), e.ChapterObject.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION));
            Singleton<Core.IO.Storage.Manager.BaseInterfaceClasses.ZipStorage>.Instance.Write(save_path, e.ChapterObject.GetType().Name, e.ChapterObject.Serialize(SaveType: Settings.Default.SaveType));
            sid.Download(e.PageObject.ImgUrl, save_path);
        }
        #endregion

        #region SmartImageDownloader
        private void sid_SmartImageDownloadObjectComplete(object sender, SmartImageDownloader.SmartImageDownloadObject e)
        {
            Singleton<Core.IO.Storage.Manager.BaseInterfaceClasses.ZipStorage>.Instance.Write(e.LocalPath, e.Filename, e.Stream);
            e.Stream.Close();
        }
        #endregion
    }
}

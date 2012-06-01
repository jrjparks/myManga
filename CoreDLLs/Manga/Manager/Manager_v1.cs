using System;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using BakaBox;
using BakaBox.Controls;
using BakaBox.Controls.Threading;
using BakaBox.IO;
using Manga.Archive;
using Manga.Core;
using Manga.Info;
using Manga.Plugin;
using Manga.Zip;

namespace Manga.Manager
{
    public sealed class Manager_v1
    {
        #region Instance
        private static Manager_v1 _Instance;
        private static Object SyncObj = new Object();
        public static Manager_v1 Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (SyncObj)
                    {
                        if (_Instance == null)
                        { _Instance = new Manager_v1(); }
                    }
                }
                return _Instance;
            }
        }

        private Manager_v1()
        {
            Worker.WorkerReportsProgress = Worker.WorkerSupportsCancellation = true;
            Worker.DoWork += Worker_DoWork;

            Worker.TaskAdded += OnTaskAdded;
            Worker.TaskBeginning += OnTaskBeginning;
            Worker.TaskProgress += OnTaskProgress;
            Worker.TaskComplete += OnTaskComplete;
            Worker.TaskFaulted += OnTaskFaulted;
            Worker.TaskRemoved += OnTaskRemoved;
        }
        #endregion

        #region Events
        public event QueuedWorker<ManagerData<String, MangaInfo>>.TaskDelegate TaskAdded;
        private void OnTaskAdded(Object Sender, QueuedTask<ManagerData<String, MangaInfo>> Task)
        {
            if (TaskAdded != null)
                TaskAdded(Sender, Task);
        }
        public event QueuedWorker<ManagerData<String, MangaInfo>>.TaskDelegate TaskBeginning;
        private void OnTaskBeginning(Object Sender, QueuedTask<ManagerData<String, MangaInfo>> Task)
        {
            if (TaskBeginning != null)
                TaskBeginning(Sender, Task);
        }
        public event QueuedWorker<ManagerData<String, MangaInfo>>.TaskDelegate TaskComplete;
        private void OnTaskComplete(Object Sender, QueuedTask<ManagerData<String, MangaInfo>> Task)
        {
            if (TaskComplete != null)
                TaskComplete(Sender, Task);
        }
        public event QueuedWorker<ManagerData<String, MangaInfo>>.TaskDelegate TaskProgress;
        private void OnTaskProgress(Object Sender, QueuedTask<ManagerData<String, MangaInfo>> Task)
        {
            if (TaskProgress != null)
                TaskProgress(Sender, Task);
        }
        public event QueuedWorker<ManagerData<String, MangaInfo>>.TaskDelegate TaskFaulted;
        private void OnTaskFaulted(Object Sender, QueuedTask<ManagerData<String, MangaInfo>> Task)
        {
            if (TaskFaulted != null)
                TaskFaulted(Sender, Task);
        }
        public event QueuedWorker<ManagerData<String, MangaInfo>>.TaskDelegate TaskRemoved;
        private void OnTaskRemoved(Object Sender, QueuedTask<ManagerData<String, MangaInfo>> Task)
        {
            if (TaskRemoved != null)
                TaskRemoved(Sender, Task);
        }

        public event QueuedWorker<ManagerData<String, MangaInfo>>.CompleteDelegate QueueComplete;
        private void OnQueueComplete()
        {
            if (QueueComplete != null)
                QueueComplete(this);
        }

        public delegate void NameUpdatedEvent(Object sender, QueuedTask<ManagerData<String, MangaInfo>> Task);
        public event NameUpdatedEvent NameUpdated;
        private void OnNameUpdated(QueuedTask<ManagerData<String, MangaInfo>> Task)
        {
            if (NameUpdated != null)
                NameUpdated(this, Task);
        }
        #endregion

        #region Fields
        private QueuedBackgroundWorker<ManagerData<String, MangaInfo>> _Worker;
        private QueuedBackgroundWorker<ManagerData<String, MangaInfo>> Worker
        {
            get
            {
                if (_Worker == null)
                    _Worker = new QueuedBackgroundWorker<ManagerData<String, MangaInfo>>();
                return _Worker;
            }
        }
        #endregion

        #region Members
        #region Public
        #region Add Tasks
        public Guid DownloadManga(String InfoPage)
        {
            ManagerData<String, MangaInfo> Data = new ManagerData<String, MangaInfo>(InfoPage, DownloadType.Manga, InfoPage);
            return Worker.AddToQueue(Data);
        }
        public Guid DownloadManga(String InfoPage, Guid TaskID)
        {
            ManagerData<String, MangaInfo> Data = new ManagerData<String, MangaInfo>(InfoPage, DownloadType.Manga, InfoPage);
            return Worker.AddToQueue(Data, TaskID);
        }
        public Guid DownloadManga(MangaInfo MangaInfo)
        {
            ManagerData<String, MangaInfo> Data = new ManagerData<String, MangaInfo>(MangaInfo.Name, DownloadType.Manga, MangaInfo.InfoPage, MangaInfo);
            return Worker.AddToQueue(Data);
        }
        public Guid DownloadManga(MangaInfo MangaInfo, Guid TaskID)
        {
            ManagerData<String, MangaInfo> Data = new ManagerData<String, MangaInfo>(MangaInfo.Name, DownloadType.Manga, MangaInfo.InfoPage, MangaInfo);
            return Worker.AddToQueue(Data, TaskID);
        }

        public Guid DownloadChapter(String ChapterPage)
        {
            ManagerData<String, MangaInfo> Data = new ManagerData<String, MangaInfo>(ChapterPage, DownloadType.Chapter, ChapterPage);
            return Worker.AddToQueue(Data);
        }
        public Guid DownloadChapter(String ChapterPage, Guid TaskID)
        {
            ManagerData<String, MangaInfo> Data = new ManagerData<String, MangaInfo>(ChapterPage, DownloadType.Chapter, ChapterPage);
            return Worker.AddToQueue(Data, TaskID);
        }
        public Guid DownloadChapter(ChapterEntry ChapterEntry)
        {
            ManagerData<String, MangaInfo> Data = new ManagerData<String, MangaInfo>(ChapterEntry.Name, DownloadType.Chapter, ChapterEntry.UrlLink);
            return Worker.AddToQueue(Data);
        }
        public Guid DownloadChapter(ChapterEntry ChapterEntry, Guid TaskID)
        {
            ManagerData<String, MangaInfo> Data = new ManagerData<String, MangaInfo>(ChapterEntry.Name, DownloadType.Chapter, ChapterEntry.UrlLink);
            return Worker.AddToQueue(Data, TaskID);
        }
        #endregion

        #region Remove Tasks
        public Boolean CancelTask(Guid TaskID)
        { return Worker.CancelTask(TaskID); }
        #endregion
        #endregion

        #region Private
        #endregion

        #region Worker Members
        private void PluginReportA(Object s, Int32 p, Object d)
        {
            Worker.ReportProgress(p / 3, d);
            if (d is MangaArchiveInfo || d is MangaInfo)
            {
                Worker.ActiveTask.Data.UpdateTitle((d as MangaData).MangaDataName());
                OnNameUpdated(Worker.ActiveTask);
            }
        }
        private void PluginReportB(Object s, Int32 p, Object d)
        {
            Worker.ReportProgress(33 + (p / 3), d);
        }
        private void PluginReportC(Object s, Int32 p, MangaDataZip.FileType f, Object d)
        {
            Worker.ReportProgress(66 + (p / 3), d);
        }

        void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            QueuedTask<ManagerData<String, MangaInfo>> Task = e.Argument as QueuedTask<ManagerData<String, MangaInfo>>;
            IMangaPlugin Plugin;
            switch (Task.Data.DownloadType)
            {
                default: break;

                case DownloadType.Manga:
                    #region Manga Downloader
                    Boolean IsUpdate = (Task.Data.Parameter != null), 
                        IsUpdateNeeded = !IsUpdate;

                    String InfoPage = Task.Data.Data;
                    Plugin = Global_IMangaPluginCollection.Instance.Plugins.PluginToUse_SiteUrl(InfoPage);

                    if (e.Cancel) throw new Exception("Canceled");
                    Plugin.ProgressChanged += PluginReportA;
                    MangaInfo MI = Plugin.LoadMangaInformation(InfoPage);
                    Plugin.ProgressChanged -= PluginReportA;

                    if (e.Cancel) throw new Exception("Canceled");
                    if (IsUpdate)
                    {
                        MangaInfo oldMI = Task.Data.Parameter;
                        MI.Volume = oldMI.Volume;
                        MI.Chapter = oldMI.Chapter;
                        MI.SubChapter = oldMI.SubChapter;
                        MI.Page = oldMI.Page;
                        IsUpdateNeeded = !MI.Equals(oldMI);
                        oldMI = null;
                    }

                    if (IsUpdateNeeded)
                    {
                        if (e.Cancel) throw new Exception("Canceled");
                        Plugin.ProgressChanged += PluginReportB;
                        CoverData CD = IsUpdate ? null : Plugin.GetCoverImage(MI);
                        Plugin.ProgressChanged -= PluginReportB;

                        if (e.Cancel) throw new Exception("Canceled");
                        MangaDataZip.Instance.ProgressChanged += PluginReportC;
                        if (IsUpdate)
                            Task.Data.UpdateData(MangaDataZip.Instance.UpdateMIZA(MI));
                        else
                            Task.Data.UpdateData(MangaDataZip.Instance.WriteMIZA(MI, CD));
                        MangaDataZip.Instance.ProgressChanged -= PluginReportC;
                    }

                    Worker.ReportProgress(100);
                    #endregion
                    break;

                case DownloadType.Chapter:
                    #region Chapter Downloader
                    Plugin = Global_IMangaPluginCollection.Instance.Plugins.PluginToUse_SiteUrl(Task.Data.Data);
                    try
                    {
                        if (e.Cancel) throw new Exception("Canceled");
                        Plugin.ProgressChanged += PluginReportA;
                        MangaArchiveInfo MAI = Plugin.LoadChapterInformation(Task.Data.Data);
                        Plugin.ProgressChanged -= PluginReportA;

                        if (e.Cancel) throw new Exception("Canceled");
                        #region Download Images
                        using (WebClient WebClient = new WebClient())
                        {
                            String RemotePath, LocalPath;
                            UInt32 FileSize;
                            Boolean Retry;

                            LocationInfoCollection InfoCollection = MAI.PageEntries.DownloadLocations;

                            Double Progress = 0D, Step = 100D / InfoCollection.Count;
                            foreach (LocationInfo Page in InfoCollection)
                            {
                                if (e.Cancel) break;
                                RemotePath = Page.FullOnlinePath;
                                LocalPath = Path.Combine(MAI.TmpFolderLocation.SafeFolder(), Path.GetFileName(RemotePath).SafeFileName());

                                FileInfo LocalFile;
                                Retry = false;
                            retry:
                                try
                                {
                                    LocalFile = new FileInfo(LocalPath);
                                    if (LocalFile.Exists)
                                        LocalFile.Delete();

                                    #region Random
                                    MatchCollection RandomNumbers = Regex.Matches(RemotePath, @"\[R(\d+)-(\d+)\]");
                                    Random r = new Random();
                                    Int32 rNumber;
                                    foreach (Match rNumberMatch in RandomNumbers)
                                    {
                                        rNumber = r.Next(Int32.Parse(rNumberMatch.Groups[1].Value), Int32.Parse(rNumberMatch.Groups[2].Value));
                                        RemotePath = RemotePath.Replace(rNumberMatch.Value, rNumber.ToString());
                                    }
                                    #endregion

                                    WebClient.Headers.Clear();
                                    WebClient.Headers.Add(System.Net.HttpRequestHeader.Referer, Plugin.SiteRefererHeader);
                                    WebClient.DownloadFile(RemotePath, LocalPath);
                                    FileSize = Parse.TryParse<UInt32>(WebClient.ResponseHeaders[System.Net.HttpResponseHeader.ContentLength].ToString(), 0);
                                    LocalFile = new FileInfo(LocalPath);
                                    if (!LocalFile.Exists)
                                        throw new Exception("File not downloaded.");
                                    else if (LocalFile.Length < FileSize)
                                    {
                                        LocalFile.Delete();
                                        throw new Exception("File not completely downloaded.");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (!Retry)
                                    {
                                        RemotePath = Page.FullAltOnlinePath;
                                        Retry = true;
                                        goto retry;
                                    }
                                    else
                                        throw new Exception("Error Downloading Image.", ex);
                                }
                                PluginReportB(this, (Int32)(Progress += Step), Page.FileName);
                            }
                            if (e.Cancel) throw new Exception("Canceled");
                        }
                        #endregion

                        if (e.Cancel) throw new Exception("Canceled");
                        MangaDataZip.Instance.ProgressChanged += PluginReportC;
                        Task.Data.UpdateData(MangaDataZip.Instance.WriteMZA(MAI));
                        MangaDataZip.Instance.ProgressChanged -= PluginReportC;
                    }
                    catch (Exception ex) { throw ex; }
                    finally
                    {
                        Plugin = null;
                    }
                    Worker.ReportProgress(100);

                    e.Result = Task;
                    #endregion
                    break;
            }
            e.Result = Task;
        }
        #endregion
        #endregion
    }
}

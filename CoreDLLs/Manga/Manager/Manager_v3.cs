using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.ComponentModel.Custom.Generic;

using BakaBox.Helper_Code;
using Manga.Archive;
using Manga.Info;
using Manga.Plugin;
using Manga.Zip;
using BakaBox.Controls;
using System.IO;
using System.Text.RegularExpressions;

namespace Manga.Manager
{
    public sealed class Manager_v3
    {
        #region Instance
        private static Manager_v3 _Instance;
        private static Object SyncObj = new Object();
        public static Manager_v3 Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (SyncObj)
                    {
                        if (_Instance == null)
                        { _Instance = new Manager_v3(); }
                    }
                }
                return _Instance;
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when System.ComponentModel.Custom.Generic.BackgroundWorker.ReportProgress(System.Int32) is called.
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs<Guid>> ProgressChanged;
        /// <summary>
        /// Occurs when the background operation has completed, has been canceled, or has raised an exception.
        /// </summary>
        public event EventHandler<RunWorkerCompletedEventArgs<Guid>> RunWorkerCompleted;

        /// <summary>
        /// Raises the System.ComponentModel.Custom.Generic.BackgroundWorker.ProgressChanged event.
        /// </summary>
        /// <param name="e">A System.ComponentModel.Custom.Generic.ProgressChangedEventArgs
        /// that contains the event data.</param>
        protected void OnProgressChanged(Object Sender, ProgressChangedEventArgs<Guid> e)
        {
            EventHandler<ProgressChangedEventArgs<Guid>> eh = ProgressChanged;
            if (eh != null)
                eh(Sender, e);
        }
        /// <summary>
        /// Raises the System.ComponentModel.Custom.Generic.BackgroundWorker.RunWorkerCompleted event.
        /// </summary>
        /// <param name="e">A System.ComponentModel.Custom.Generic.RunWorkerCompletedEventArgs
        /// that contains the event data.</param>
        protected void OnRunWorkerCompleted(Object Sender, RunWorkerCompletedEventArgs<Guid> e)
        {
            EventHandler<RunWorkerCompletedEventArgs<Guid>> eh = RunWorkerCompleted;
            if (eh != null)
                eh(Sender, e);
        }
        #endregion

        #region Fields
        private IMangaPluginCollection _Plugins;
        private IMangaPluginCollection Plugins
        {
            get
            {
                if (_Plugins == null)
                    _Plugins = new IMangaPluginCollection();
                return _Plugins;
            }
        }

        private Dictionary<Guid, BackgroundWorker<ManagerData<String>, Guid, ManagerData<String>>> _WorkerDictionary;
        private Dictionary<Guid, BackgroundWorker<ManagerData<String>, Guid, ManagerData<String>>> WorkerDictionary
        {
            get
            {
                if (_WorkerDictionary == null)
                    _WorkerDictionary = new Dictionary<Guid, BackgroundWorker<ManagerData<String>, Guid, ManagerData<String>>>();
                return _WorkerDictionary;
            }
        }

        private Queue<ManagerData<String>> _Requests;
        public Queue<ManagerData<String>> Requests
        {
            get
            {
                if (_Requests == null)
                    _Requests = new Queue<ManagerData<String>>();
                return _Requests;
            }
        }

        private Byte _MaximumNumberOfWorkers;
        public Byte MaximumNumberOfWorkers
        {
            get { return _MaximumNumberOfWorkers; }
            set { _MaximumNumberOfWorkers = value; }
        }
        
        public Boolean IsPaused 
        { get; private set; }
        public Boolean IsQueueEmpty 
        { get { return Requests.Count.Equals(0); } }

        public Boolean AreWorkersFree
        { get { return WorkerDictionary.Count < MaximumNumberOfWorkers; } }
        #endregion

        #region Constructor
        private Manager_v3()
        {
            MaximumNumberOfWorkers = (Byte)Environment.ProcessorCount;
        }
        #endregion

        #region Members
        #region Public
        public void AddPlugins(params IMangaPlugin[] Plugins)
        { this.Plugins.AddRange(Plugins); }
        
        public void CancelTask(Guid Item)
        { }
        #endregion

        #region Private

        private void CheckForNewWork()
        {
            lock (this)
            {
                if (this.IsQueueEmpty)
                { }
                else if (!IsPaused)
                    if (AreWorkersFree)
                        CreateWorker(Requests.Dequeue());
            }
        }

        #region Queue Adding
        public Guid DownloadManga(String InfoPage)
        {
            ManagerData<String> Data = new ManagerData<String>(DownloadType.Manga, InfoPage);
            Requests.Enqueue(Data);
            CheckForNewWork();
            return Data.Guid;
        }
        public Guid DownloadManga(MangaInfo MangaInfo)
        {
            ManagerData<String> Data = new ManagerData<String>(DownloadType.Manga, MangaInfo.InfoPage, MangaInfo);
            Requests.Enqueue(Data);
            CheckForNewWork();
            return Data.Guid;
        }

        public Guid DownloadChapter(String ChapterPage)
        {
            ManagerData<String> Data = new ManagerData<String>(DownloadType.Chapter, ChapterPage);
            Requests.Enqueue(Data);
            CheckForNewWork();
            return Data.Guid;
        }
        public Guid DownloadChapter(ChapterEntry ChapterEntry)
        {
            ManagerData<String> Data = new ManagerData<String>(DownloadType.Chapter, ChapterEntry.UrlLink);
            Requests.Enqueue(Data);
            CheckForNewWork();
            return Data.Guid;
        }
        #endregion

        #region Queue Destruction
        #endregion
        
        #region Worker
        private void CreateWorker(ManagerData<String> Item)
        {
            BackgroundWorker<ManagerData<String>, Guid, ManagerData<String>> NewWorker = new BackgroundWorker<ManagerData<String>, Guid, ManagerData<String>>();
            NewWorker.WorkerReportsProgress = NewWorker.WorkerSupportsCancellation = true;
            NewWorker.DoWork += Worker_DoWork;
            NewWorker.ProgressChanged += Worker_ProgressChanged;
            NewWorker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            WorkerDictionary.Add(Item.Guid, NewWorker);
            NewWorker.RunWorkerAsync(Item);
        }

        void Worker_DoWork(object sender, DoWorkEventArgs<ManagerData<String>, ManagerData<String>> e)
        {
            switch (e.Argument.DownloadType)
            {
                default: break;

                case DownloadType.Manga:
                    e.Result = DownloadManga(e);
                    break;

                case DownloadType.Chapter:
                    e.Result = DownloadChapter(e);
                    break;
            }
        }

        void Worker_ProgressChanged(object sender, ProgressChangedEventArgs<Guid> e)
        {
            OnProgressChanged(sender, e);
        }

        void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs<ManagerData<String>> e)
        {
            WorkerDictionary[e.Result.Guid].DoWork -= Worker_DoWork;
            WorkerDictionary[e.Result.Guid].ProgressChanged -= Worker_ProgressChanged;
            WorkerDictionary[e.Result.Guid].RunWorkerCompleted -= Worker_RunWorkerCompleted;
            WorkerDictionary[e.Result.Guid] = null;
            WorkerDictionary.Remove(e.Result.Guid);
            CheckForNewWork();
        }

        private void ReportProgress(Guid WorkerID, Int32 Progress)
        {
            if (WorkerDictionary.ContainsKey(WorkerID))
                WorkerDictionary[WorkerID].ReportProgress(Progress, WorkerID);
        }
        #endregion

        private ManagerData<String> DownloadManga(DoWorkEventArgs<ManagerData<String>, ManagerData<String>> e)
        {
            Guid WorkerID = e.Argument.Guid;
            Boolean IsUpdate = (e.Argument.Parameters != null) && (e.Argument.Parameters.Length > 0) && (e.Argument.Parameters[0] is MangaInfo);
            String InfoPage = e.Argument.Data;
            IMangaPlugin Plugin = Plugins.PluginToUse_SiteUrl(InfoPage);

            if (e.Cancel) throw new Exception("Canceled");
            Plugin.ProgressChanged += (s, p, d) => ReportProgress(WorkerID, p / 3);
            MangaInfo MI = Plugin.LoadMangaInformation(InfoPage);
            Plugin.ProgressChanged -= (s, p, d) => ReportProgress(WorkerID, p / 3);

            if (e.Cancel) throw new Exception("Canceled");
            if (IsUpdate)
            {
                MangaInfo oldMI = (e.Argument.Parameters[0] as MangaInfo);
                MI.Volume = oldMI.Volume;
                MI.Chapter = oldMI.Chapter;
                MI.SubChapter = oldMI.SubChapter;
                MI.Page = oldMI.Page;
                oldMI = null;
            }

            if (e.Cancel) throw new Exception("Canceled");
            Plugin.ProgressChanged += (s, p, d) => ReportProgress(WorkerID, 33 + (p / 3));
            CoverData CD = Plugin.GetCoverImage(MI);
            Plugin.ProgressChanged -= (s, p, d) => ReportProgress(WorkerID, 33 + (p / 3));

            if (e.Cancel) throw new Exception("Canceled");
            MangaDataZip.Instance.ProgressChanged += (s, p, f, d) => ReportProgress(WorkerID, 66 + (p / 3));
            MangaDataZip.Instance.WriteMIZA(MI, CD);
            MangaDataZip.Instance.ProgressChanged -= (s, p, f, d) => ReportProgress(WorkerID, 66 + (p / 3));

            ReportProgress(WorkerID, 100);

            return e.Argument;
        }

        private ManagerData<String> DownloadChapter(DoWorkEventArgs<ManagerData<String>, ManagerData<String>> e)
        {
            Guid WorkerID = e.Argument.Guid;
            IMangaPlugin Plugin = Plugins.PluginToUse_SiteUrl(e.Argument.Data);

            if (e.Cancel) throw new Exception("Canceled");
            Plugin.ProgressChanged += (s, p, d) => ReportProgress(WorkerID, p / 3);
            MangaArchiveInfo MAI = Plugin.LoadChapterInformation(e.Argument.Data);
            Plugin.ProgressChanged -= (s, p, d) => ReportProgress(WorkerID, p / 3);

            if (e.Cancel) throw new Exception("Canceled");
            #region Download Images
            using (WebClient WebClient = new WebClient())
            {
                String RemotePath, LocalPath;
                UInt32 FileSize;
                Boolean Retry;

                LocationInfoCollection InfoCollection = MAI.PageEntries.DownloadLocations;

                Double Progress = 0D, Step = 33D / InfoCollection.Count;
                foreach (LocationInfo Page in InfoCollection)
                {
                    if (e.Cancel) break;
                    RemotePath = Page.FullOnlinePath;
                    LocalPath = Path.Combine(IO.SafeFolder(MAI.TmpFolderLocation), IO.SafeFileName(Path.GetFileName(RemotePath)));

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
                    catch
                    {
                        if (!Retry)
                        {
                            RemotePath = Page.FullAltOnlinePath;
                            Retry = true;
                            goto retry;
                        }
                    }
                ReportProgress(WorkerID, (Int32)(Progress += Step));
                }
                if (e.Cancel) throw new Exception("Canceled");
            }
            #endregion

            if (e.Cancel) throw new Exception("Canceled");
            MangaDataZip.Instance.ProgressChanged += (s, p, f, d) => ReportProgress(WorkerID, 66 + (p / 3));
            MangaDataZip.Instance.WriteMZA(MAI);
            MangaDataZip.Instance.ProgressChanged -= (s, p, f, d) => ReportProgress(WorkerID, 66 + (p / 3));

            ReportProgress(WorkerID, 100);

            return e.Argument;
        }
        #endregion
        #endregion
    }
}
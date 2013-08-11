using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using BakaBox.Controls;
using BakaBox.Helper_Code;
using Manga.Archive;
using Manga.Info;
using Manga.Plugin;
using Manga.Zip;

namespace Manga.Manager
{
    #region Events
    public delegate void ManagerItemProgressChangedEventHandler(Object Sender, ProgressChangedEventArgs ProgressChangedEventArgs, Guid TaskIdentification);
    public delegate void ManagerItemStatusChangedEventHandler(Object Sender, Guid TaskIdentification, ManagerItemStatus Status, Exception Exception);
    #endregion

    #region Properties
    public enum DownloadType
    {
        Manga = 0x01,
        Chapter = 0x02
    }
    
    public enum ManagerItemStatus
    {
        Queued = 0x01,
        Started = 0x02,
        Complete = 0x04,
        Faulted = 0x08,
        Canceled = 0x16
    }
    #endregion

    public sealed class Manager_v2
    {
        #region Instance
        private static Manager_v2 _Instance;
        private static Object SyncObj = new Object();
        public static Manager_v2 Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (SyncObj)
                    {
                        if (_Instance == null)
                        { _Instance = new Manager_v2(); }
                    }
                }
                return _Instance;
            }
        }
        #endregion

        #region Events
        public event ManagerItemProgressChangedEventHandler TaskProgressChanged;
        protected void OnProgressChanged(Guid TaskIdentification, ProgressChangedEventArgs ProgressChangedEventArgs)
        {
            if (TaskProgressChanged != null)
                lock (TaskProgressChanged)
                {
                    if (TaskProgressChanged != null)
                        TaskProgressChanged.Invoke(this, ProgressChangedEventArgs, TaskIdentification);
                }
        }

        public event ManagerItemStatusChangedEventHandler TaskStatusChanged;
        protected void OnTaskStatusChanged(Guid TaskIdentification, ManagerItemStatus ManagerItemStatus, Exception Exception)
        {
            switch (ManagerItemStatus)
            {
                default: break;

                case Manager.ManagerItemStatus.Complete:
                case Manager.ManagerItemStatus.Canceled:
                case Manager.ManagerItemStatus.Faulted:
                    RemoveTask(TaskIdentification);
                    break;
            }
            if (TaskStatusChanged != null)
                TaskStatusChanged.Invoke(this, TaskIdentification, ManagerItemStatus, Exception);
        }
        #endregion

        #region Properties
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

        private Dictionary<Guid, ManagerItem<String>> _Tasks;
        public Dictionary<Guid, ManagerItem<String>> Tasks
        {
            get
            {
                if (_Tasks == null)
                    _Tasks = new Dictionary<Guid, ManagerItem<String>>();
                return _Tasks;
            }
        }

        private AsyncOperation asyncOperation = null;
        private readonly SendOrPostCallback operationCompleted;
        private readonly SendOrPostCallback progressReporter;
        #endregion

        #region Constructor
        private Manager_v2()
        {
            operationCompleted = new SendOrPostCallback(AsyncOperationCompleted);
            progressReporter = new SendOrPostCallback(ProgressReporter);
        }
        #endregion

        #region Members
        #region Public
        public void AddPlugins(params IMangaPlugin[] Plugins)
        { this.Plugins.AddRange(Plugins); }

        public Guid DownloadManga(String InfoPage)
        {
            ManagerItem<String> Item = new ManagerItem<String>(DownloadType.Manga, InfoPage);

            ThreadPool.QueueUserWorkItem(new WaitCallback(DownloadMangaCallBack), Item);

            return AddTask(Item);
        }
        public Guid DownloadManga(MangaInfo MangaInfo)
        {
            ManagerItem<String> Item = new ManagerItem<String>(DownloadType.Manga, MangaInfo.InfoPage, MangaInfo);

            ThreadPool.QueueUserWorkItem(new WaitCallback(DownloadMangaCallBack), Item);

            return AddTask(Item);
        }

        public Guid DownloadChapter(String ChapterPage)
        {
            ManagerItem<String> Item = new ManagerItem<String>(DownloadType.Chapter, ChapterPage);

            ThreadPool.QueueUserWorkItem(new WaitCallback(DownloadChapterCallBack), Item);

            return AddTask(Item);
        }
        public Guid DownloadChapter(ChapterEntry ChapterEntry)
        {
            ManagerItem<String> Item = new ManagerItem<String>(DownloadType.Chapter, ChapterEntry.UrlLink);

            ThreadPool.QueueUserWorkItem(new WaitCallback(DownloadChapterCallBack), Item);

            return AddTask(Item);
        }

        public void CancelTask(Guid Item)
        { Tasks[Item].RequestCancel(); }
        #endregion

        #region Private
        private void AsyncOperationCompleted(object state)
        {
            Object[] Data = state as Object[];
        }
        private void ProgressReporter(object state)
        {
            Object[] Data = state as Object[];
            OnProgressChanged((Guid)(Data[0] ?? Guid.Empty), Data[1] as ProgressChangedEventArgs);
        }

        private Guid AddTask(ManagerItem<String> Item)
        {
            Tasks.Add(Item.TaskIdentification, Item);

            Item.ProgressChanged += ItemProgressChanged;
            Item.TaskStatusChanged += ItemTaskStatusChanged;

            return Item.TaskIdentification;
        }
        private Boolean RemoveTask(Guid Item)
        {
            Tasks[Item].ProgressChanged -= ItemProgressChanged;
            Tasks[Item].TaskStatusChanged -= ItemTaskStatusChanged;

            return _Tasks.Remove(Item);
        }
        private void ItemProgressChanged(Object Sender, ProgressChangedEventArgs ProgressChangedEventArgs, Guid TaskIdentification)
        { OnProgressChanged(TaskIdentification, ProgressChangedEventArgs); }
        private void ItemTaskStatusChanged(Object Sender, Guid TaskIdentification, ManagerItemStatus Status, Exception Exception)
        { OnTaskStatusChanged(TaskIdentification, Status, Exception); }

        private void DownloadMangaCallBack(Object State)
        {
            ManagerItem<String> ManagerItem = State as ManagerItem<String>;
            try
            {
                Boolean IsUpdate = (ManagerItem.Parameters != null) && (ManagerItem.Parameters.Length > 0) && (ManagerItem.Parameters[0] is MangaInfo);
                String InfoPage = ManagerItem.Argument;
                IMangaPlugin Plugin = Plugins.PluginToUse_SiteUrl(InfoPage);

                if (ManagerItem.CancelRequested) throw new Exception("Canceled");
                Plugin.ProgressChanged += (s, p, d) => ManagerItem.ReportProgress(p / 3, d);
                MangaInfo MI = Plugin.LoadMangaInformation(InfoPage);

                if (ManagerItem.CancelRequested) throw new Exception("Canceled");
                if (IsUpdate)
                {
                    MangaInfo oldMI = (ManagerItem.Parameters[0] as MangaInfo);
                    MI.Volume = oldMI.Volume;
                    MI.Chapter = oldMI.Chapter;
                    MI.SubChapter = oldMI.SubChapter;
                    MI.Page = oldMI.Page;
                    oldMI = null;
                }

                if (ManagerItem.CancelRequested) throw new Exception("Canceled");
                Plugin.ProgressChanged += (s, p, d) => ManagerItem.ReportProgress(33 + (p / 3), d);
                CoverData CD = Plugin.GetCoverImage(MI);

                if (ManagerItem.CancelRequested) throw new Exception("Canceled");
                MangaDataZip.Instance.ProgressChanged += (s, p, f, d) => ManagerItem.ReportProgress(66 + (p / 3), d);
                MangaDataZip.Instance.WriteMIZA(MI, CD);

                ManagerItem.ReportComplete();
            }
            catch (Exception e)
            {
                ManagerItem.ReportFault(e);
            }
        }

        private void DownloadChapterCallBack(Object State)
        {
            ManagerItem<String> ManagerItem = State as ManagerItem<String>;
            try
            {
                IMangaPlugin Plugin = Plugins.PluginToUse_SiteUrl(ManagerItem.Argument);

                if (ManagerItem.CancelRequested) throw new Exception("Canceled");
                Plugin.ProgressChanged += (s, p, d) => ManagerItem.ReportProgress(p / 3, d);
                MangaArchiveInfo MAI = Plugin.LoadChapterInformation(ManagerItem.Argument);

                if (ManagerItem.CancelRequested) throw new Exception("Canceled");
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
                        if (ManagerItem.CancelRequested) break;
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
                        finally
                        {
                            ManagerItem.ReportProgress((Int32)(Progress += Step));
                        }
                    }
                    if (ManagerItem.CancelRequested) throw new Exception("Canceled");
                }
                #endregion

                if (ManagerItem.CancelRequested) throw new Exception("Canceled");
                MangaDataZip.Instance.ProgressChanged += (s, p, f, d) => ManagerItem.ReportProgress(66 + (p / 3), d);
                MangaDataZip.Instance.WriteMZA(MAI);

                ManagerItem.ReportComplete();
            }
            catch (Exception e)
            {
                ManagerItem.ReportFault(e);
            }
        }
        #endregion
        #endregion
    }
    
    public class ManagerItem<T>
    {
        #region Events
        public event ManagerItemProgressChangedEventHandler ProgressChanged;
        protected virtual void OnProgressChanged(Int32 Progress)
        { OnProgressChanged(Progress, null); }
        protected virtual void OnProgressChanged(Int32 Progress, Object UserState)
        {
            if (ProgressChanged != null)
                ProgressChanged(this, new ProgressChangedEventArgs(Progress, UserState), TaskIdentification);
        }

        public event ManagerItemStatusChangedEventHandler TaskStatusChanged;
        protected virtual void OnTaskComplete()
        {
            OnProgressChanged(100);
            if (TaskStatusChanged != null)
                TaskStatusChanged(this, TaskIdentification, ManagerItemStatus.Complete, null);
        }
        protected virtual void OnTaskFaulted(Exception Exception)
        {
            OnProgressChanged(100);
            if (TaskStatusChanged != null)
                TaskStatusChanged(this, TaskIdentification, ManagerItemStatus.Faulted, Exception);
        }
        #endregion

        #region Properties
        /*private EventWaitHandle _WaitHandle;
        public EventWaitHandle WaitHandle
        {
            get
            {
                if (_WaitHandle == null)
                    _WaitHandle = new ManualResetEvent(false);
                return _WaitHandle;
            }
        }//*/

        private Guid _TaskIdentification;
        public Guid TaskIdentification
        {
            get
            {
                if (_TaskIdentification == null ||
                    _TaskIdentification == Guid.Empty)
                    _TaskIdentification = Guid.NewGuid();
                return _TaskIdentification;
            }
        }

        private Boolean _CancelRequested;
        public Boolean CancelRequested
        {
            get { return _CancelRequested; }
            private set { _CancelRequested = value; }
        }

        public DownloadType DownloadType { get; private set; }
        public T Argument { get; private set; }
        public Object[] Parameters { get; private set; }
        #endregion

        #region Members
        public void ReportProgress(Int32 Progress)
        { OnProgressChanged(Progress); }
        public void ReportProgress(Int32 Progress, Object Data)
        { OnProgressChanged(Progress, Data); }
        
        public void ReportComplete()
        { OnTaskComplete(); }
        public void ReportFault(Exception Exception)
        { OnTaskFaulted(Exception); }

        public void RequestCancel()
        { CancelRequested = true; }
        #endregion

        public ManagerItem(DownloadType DownloadType, T Argument)
        {
            this.DownloadType = DownloadType;
            this.Argument = Argument;
            this.Parameters = null;
        }
        public ManagerItem(DownloadType DownloadType, T Argument, params Object[] Parameters)
        {
            this.DownloadType = DownloadType;
            this.Argument = Argument;
            this.Parameters = Parameters;
        }
    }
}

using System;
using System.ComponentModel;
using BakaBox.Controls.Threading;
using Manga.Archive;
using Manga.Core;
using Manga.Info;
using Manga.Plugin;

namespace Manga.Manager
{
    public class MPUevents
    {
        public delegate void MPUTaskDelegate(Object sender, QueuedTask<RequestTransfer> Task, MangaData MD);
    }
    internal class ManagerPluginUser : IDisposable
    {
        #region Delegates & Events
        public event QueuedWorker<RequestTransfer>.TaskDelegate TaskAdded;
        protected virtual void OnTaskAdded(QueuedTask<RequestTransfer> Task)
        {
            if (TaskAdded != null)
                TaskAdded(this, Task);
        }
        public event QueuedWorker<RequestTransfer>.TaskDelegate TaskBeginning;
        protected virtual void OnTaskBeginning(QueuedTask<RequestTransfer> Task)
        {
            if (TaskBeginning != null)
                TaskBeginning(this, Task);
        }
        public event QueuedWorker<RequestTransfer>.TaskDelegate TaskComplete;
        protected virtual void OnTaskComplete(QueuedTask<RequestTransfer> Task)
        {
            if (TaskComplete != null)
                TaskComplete(this, Task);
        }
        public event QueuedWorker<RequestTransfer>.TaskDelegate TaskProgress;
        protected virtual void OnTaskProgress(QueuedTask<RequestTransfer> Task)
        {
            if (TaskProgress != null)
                TaskProgress(this, Task);
        }
        public event QueuedWorker<RequestTransfer>.TaskDelegate TaskFaulted;
        protected virtual void OnTaskFaulted(QueuedTask<RequestTransfer> Task)
        {
            if (TaskFaulted != null)
                TaskFaulted(this, Task);
        }
        public event QueuedWorker<RequestTransfer>.TaskDelegate TaskRemoved;
        protected virtual void OnTaskRemoved(QueuedTask<RequestTransfer> Task)
        {
            if (TaskRemoved != null)
                TaskRemoved(this, Task);
        }

        public event QueuedWorker<RequestTransfer>.CompleteDelegate QueueComplete;
        protected virtual void OnQueueComplete()
        {
            if (QueueComplete != null)
                QueueComplete(this);
        }

        public event MPUevents.MPUTaskDelegate NewMAI_Name;
        protected virtual void OnNewMAI_Name(QueuedTask<RequestTransfer> Task, MangaData MD)
        {
            if (NewMAI_Name != null)
                NewMAI_Name(this, Task, MD);
        }
        #endregion

        private IMangaPluginCollection WebMangaPlugins { get; set; }
        public IMangaPlugin GetPlugin(String SiteName)
        {
            return WebMangaPlugins.PluginToUse_SiteName(SiteName);
        }
        private readonly Random rNumber;

        private readonly QueuedBackgroundWorker<RequestTransfer> QueuedPluginWorker;

        public QueuedTask<RequestTransfer> ActiveRequest { get { return QueuedPluginWorker.ActiveTask; } }

        public ManagerPluginUser()
        {
            WebMangaPlugins = new IMangaPluginCollection();

            rNumber = new Random();

            QueuedPluginWorker = new QueuedBackgroundWorker<RequestTransfer>();
            QueuedPluginWorker.WorkerReportsProgress = true;
            QueuedPluginWorker.DoWork += QueuedFileWorker_DoWork;

            QueuedPluginWorker.TaskAdded += (s, t) => OnTaskAdded(t);
            QueuedPluginWorker.TaskBeginning += (s, t) => OnTaskBeginning(t);
            QueuedPluginWorker.TaskComplete += (s, t) => OnTaskComplete(t);
            QueuedPluginWorker.TaskProgress += (s, t) => OnTaskProgress(t);
            QueuedPluginWorker.TaskFaulted += (s, t) => OnTaskFaulted(t);
            QueuedPluginWorker.TaskRemoved += (s, t) => OnTaskRemoved(t);
            QueuedPluginWorker.QueueComplete += (s) => OnQueueComplete();
        }

        public void AddPluginClass(IMangaPlugin Plugin)
        {
            if (!WebMangaPlugins.Contains(Plugin))
                WebMangaPlugins.Add(Plugin);
        }
        public Boolean RemovePluginClass(IMangaPlugin Plugin)
        {
            if (WebMangaPlugins.Contains(Plugin))
                return WebMangaPlugins.Remove(Plugin);
            return false;
        }
        public Boolean RemovePluginClass(Int32 Index)
        {
            return WebMangaPlugins.RemoveAt(Index);
        }

        public Guid RequestChapterInfo(String ChapterPath)
        {
            return RequestChapterInfo(ChapterPath, Guid.NewGuid());
        }
        public Guid RequestChapterInfo(String ChapterPath, Guid TaskID)
        {
            lock (this)
            {
                QueuedPluginWorker.AddToQueue(new RequestTransfer() { Path = ChapterPath, RequestType = DataType.ChapterInformation }, TaskID);
            }
            return TaskID;
        }

        public Guid RequestMangaInfo(String ChapterPath)
        {
            return RequestMangaInfo(ChapterPath, Guid.NewGuid());
        }
        public Guid RequestMangaInfo(String MangaPath, Guid TaskID)
        {
            lock (this)
            {
                QueuedPluginWorker.AddToQueue(new RequestTransfer() { Path = MangaPath, RequestType = DataType.MangaInformation }, TaskID);
            }
            return TaskID;
        }

        public Guid RequestMangaInfoUpdate(MangaInfo MangaInfo)
        {
            return RequestMangaInfo(MangaInfo, Guid.NewGuid());
        }
        public Guid RequestMangaInfo(MangaInfo MangaInfo, Guid TaskID)
        {
            lock (this)
            {
                QueuedPluginWorker.AddToQueue(new RequestTransfer() { mData = MangaInfo, RequestType = DataType.MangaInformation }, TaskID);
            }
            return TaskID;
        }

        public void ClearQueue()
        {
            QueuedPluginWorker.ClearQueue();
        }

        public void CancelQueue()
        {
            QueuedPluginWorker.CancelQueue();
        }

        public Boolean CancelTask(Guid TaskID)
        {
            return QueuedPluginWorker.CancelTask(TaskID);
        }

        #region Worker Tasks
        private void QueuedFileWorker_DoWork(Object sender, DoWorkEventArgs e)
        {
            QueuedTask<RequestTransfer> Task = e.Argument as QueuedTask<RequestTransfer>;
            RequestTransfer ReqTrans = Task.Data;
            Boolean UsePath = (ReqTrans.mData == null | !(ReqTrans.mData is MangaInfo));

            IMangaPlugin UsePlugin;
            String _MangaPath;
            if (UsePath)
                _MangaPath = ReqTrans.Path;
            else
                _MangaPath = (ReqTrans.mData as MangaInfo).InfoPage;
            UsePlugin = WebMangaPlugins.PluginToUse_SiteUrl(_MangaPath);

            if (UsePlugin != null)
            {
                UsePlugin.ProgressChanged += UsePlugin_ProgressChanged;
                try
                {
                    switch (ReqTrans.RequestType)
                    {
                        case DataType.ChapterInformation:
                            if ((UsePlugin.SupportedMethods & SupportedMethods.ChapterInfo) == SupportedMethods.ChapterInfo)
                                ReqTrans.mData = UsePlugin.LoadChapterInformation(ReqTrans.Path);
                            break;

                        case DataType.MangaInformation:
                            if ((UsePlugin.SupportedMethods & SupportedMethods.MangaInfo) == SupportedMethods.MangaInfo)
                            {
                                MangaInfo _tmpMI = UsePlugin.LoadMangaInformation(_MangaPath);
                                if (!UsePath)
                                {
                                    MangaInfo _oldMI = (ReqTrans.mData as MangaInfo);
                                    _tmpMI.Volume = _oldMI.Volume;
                                    _tmpMI.Chapter = _oldMI.Chapter;
                                    _tmpMI.SubChapter = _oldMI.SubChapter;
                                    _tmpMI.Page = _oldMI.Page;
                                    _oldMI = null;
                                }
                                ReqTrans.mData = 
                                    new MangaInfoCoverData(
                                        new MangaInfo(_tmpMI), 
                                        (UsePath && (UsePlugin.SupportedMethods & SupportedMethods.CoverImage).Equals(SupportedMethods.CoverImage)) ? 
                                        UsePlugin.GetCoverImage(_tmpMI) : null);
                                _tmpMI = null;
                            }
                            break;

                        default: break;
                    }
                    QueuedPluginWorker.WorkerCancellationToken.Token.ThrowIfCancellationRequested();
                }
                catch
                {
                    ReqTrans.mData = null;
                }
                Task.Data = ReqTrans;
                UsePlugin.ProgressChanged -= UsePlugin_ProgressChanged;
                QueuedPluginWorker.ReportProgress(100);
            }
            e.Result = Task;
        }

        private void UsePlugin_ProgressChanged(Object Sender, Int32 Progress, Object Data)
        {
            QueuedPluginWorker.ReportProgress(Progress, Data);
            if (Data is MangaArchiveInfo || Data is MangaInfo)
                OnNewMAI_Name(ActiveRequest, (Data as MangaData));
        }

        public String ManipulateTo(MangaData MangaData, DataType ManipulationType)
        {
            IMangaPlugin UsePlugin = WebMangaPlugins.PluginToUse_SiteName(MangaData.Site);
            return UsePlugin.ManipulateMangaData(MangaData, ManipulationType);
        }
        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            QueuedPluginWorker.Dispose();
        }
        #endregion
    }

    public class RequestTransfer
    {
        public String Path { get; set; }
        public Object mData { get; set; }
        public DataType RequestType { get; set; }
    }
}

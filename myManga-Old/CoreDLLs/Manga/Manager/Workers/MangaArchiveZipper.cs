using System;
using System.ComponentModel;
using System.IO;
using BakaBox.Controls.Threading;
using Manga.Archive;
using Manga.Core;
using Manga.Zip;

namespace Manga.Manager
{
    public class MAZevents
    {
        public delegate void MZA_Saved(Object sender, String Path, MangaArchiveInfo MangaInfo);
    }

    internal class MangaArchiveZipper : IDisposable
    {
        #region Events
        public event QueuedWorker<MangaArchiveInfo>.TaskDelegate TaskAdded;
        protected virtual void OnTaskAdded(QueuedTask<MangaArchiveInfo> Task)
        {
            if (TaskAdded != null)
                TaskAdded(this, Task);
        }
        public event QueuedWorker<MangaArchiveInfo>.TaskDelegate TaskBeginning;
        protected virtual void OnTaskBeginning(QueuedTask<MangaArchiveInfo> Task)
        {
            if (TaskBeginning != null)
                TaskBeginning(this, Task);
        }
        public event QueuedWorker<MangaArchiveInfo>.TaskDelegate TaskComplete;
        protected virtual void OnTaskComplete(QueuedTask<MangaArchiveInfo> Task)
        {
            if (TaskComplete != null)
                TaskComplete(this, Task);
        }
        public event QueuedWorker<MangaArchiveInfo>.TaskDelegate TaskProgress;
        protected virtual void OnTaskProgress(QueuedTask<MangaArchiveInfo> Task)
        {
            if (TaskProgress != null)
                TaskProgress(this, Task);
        }
        public event QueuedWorker<MangaArchiveInfo>.TaskDelegate TaskFaulted;
        protected virtual void OnTaskFaulted(QueuedTask<MangaArchiveInfo> Task)
        {
            if (TaskFaulted != null)
                TaskFaulted(this, Task);
        }
        public event QueuedWorker<MangaArchiveInfo>.TaskDelegate TaskRemoved;
        protected virtual void OnTaskRemoved(QueuedTask<MangaArchiveInfo> Task)
        {
            if (TaskRemoved != null)
                TaskRemoved(this, Task);
        }

        public event QueuedWorker<MangaArchiveInfo>.CompleteDelegate QueueComplete;
        protected virtual void OnQueueComplete()
        {
            if (QueueComplete != null)
                QueueComplete(this);
        }

        public event MAZevents.MZA_Saved MZASaved;
        protected virtual void OnMZASave(String Path, MangaArchiveInfo MangaArchiveInfo)
        {
            if (MZASaved != null)
                MZASaved(this, Path, MangaArchiveInfo);
        }
        #endregion

        #region Vars
        private readonly QueuedBackgroundWorker<MangaArchiveInfo> MangaArchiveZipWorker;
        public QueuedTask<MangaArchiveInfo> ActiveTask { get { return MangaArchiveZipWorker.ActiveTask; } }
        private String mdZip_SaveLocation { get; set; }
        #endregion

        public MangaArchiveZipper()
        {
            mdZip_SaveLocation = MangaDataZip.DefaultSaveLocation; ;
            MangaArchiveZipWorker = new QueuedBackgroundWorker<MangaArchiveInfo>();
            MAZippperCore();
        }
        public MangaArchiveZipper(String MangaDataZip_SaveLocation)
        {
            mdZip_SaveLocation = MangaDataZip_SaveLocation;
            MangaArchiveZipWorker = new QueuedBackgroundWorker<MangaArchiveInfo>();
            MAZippperCore();
        }
        private void MAZippperCore()
        {
            MangaArchiveZipWorker.WorkerReportsProgress = true;
            MangaArchiveZipWorker.DoWork += new DoWorkEventHandler(QueuedZipSaver_DoWork);
            MangaArchiveZipWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(QueuedZipSaver_RunWorkerCompleted);
            MangaArchiveZipWorker.TaskAdded += (s, t) => OnTaskAdded(t);
            MangaArchiveZipWorker.TaskBeginning += (s, t) => OnTaskBeginning(t);
            MangaArchiveZipWorker.TaskComplete += (s, t) => OnTaskComplete(t);
            MangaArchiveZipWorker.TaskProgress += (s, t) => OnTaskProgress(t);
            MangaArchiveZipWorker.TaskFaulted += (s, t) => OnTaskFaulted(t);
            MangaArchiveZipWorker.TaskRemoved += (s, t) => OnTaskRemoved(t);
            MangaArchiveZipWorker.QueueComplete += (s) => OnQueueComplete();
        }
        public void UpdateSavePath(String Path)
        { mdZip_SaveLocation = Path; }

        #region Zip Events

        #region Public

        public Guid AddMAIToQueue(MangaArchiveInfo Data) 
        { return AddMAIToQueue(Data, Guid.NewGuid()); }
        public Guid AddMAIToQueue(MangaArchiveInfo Data, Guid TaskID) 
        { return AddMAIToQueue(new QueuedTask<MangaArchiveInfo>(Data, TaskID)); }
        public Guid AddMAIToQueue(QueuedTask<MangaArchiveInfo> Task)
        {
            return MangaArchiveZipWorker.AddToQueue(Task);
        }

        public void ClearQueue()
        {
            MangaArchiveZipWorker.ClearQueue();
        }

        public void CancelQueue()
        {
            MangaArchiveZipWorker.CancelQueue();
        }

        public Boolean CancelTask(Guid TaskID)
        {
            return MangaArchiveZipWorker.CancelTask(TaskID);
        }

        #endregion

        #region Private
        private void QueuedZipSaver_RunWorkerCompleted(Object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result != null)
            {
                TransferClass Result = e.Result as TransferClass;
                OnMZASave(Result.Data[1] as String, Result.Data[0] as MangaArchiveInfo);
            }
            GC.Collect();
        }

        private void QueuedZipSaver_DoWork(Object sender, DoWorkEventArgs e)
        {
            QueuedTask<MangaArchiveInfo> Task = e.Argument as QueuedTask<MangaArchiveInfo>;
            String MZA_Path = String.Empty;
            try
            {
                MangaArchiveZipWorker.WorkerCancellationToken.Token.ThrowIfCancellationRequested();
                MangaArchiveZipWorker.ReportProgress(5);
                MangaArchiveInfo MangaArchiveInfo = Task.Data;
                MangaArchiveZipWorker.ReportProgress(10);
                
                using (MangaDataZip mdZip = new MangaDataZip(mdZip_SaveLocation))
                {
                    MZA_Path = mdZip.CreateMZAPath(MangaArchiveInfo);
                    mdZip.ProgressChanged += (s, p, t, d) =>
                    {
                        if (t == MangaDataMethods.FileType.MZA)
                            MangaArchiveZipWorker.ReportProgress((Int32)Math.Round((Double)p * 0.7D) + 10);
                    };
                    MZA_Path = mdZip.WriteMZA(MangaArchiveInfo, MZA_Path);
                }
               
                MangaArchiveZipWorker.ReportProgress(80);
                e.Result = new TransferClass() { Data = new Object[] { MangaArchiveInfo, MZA_Path } };
            }
            catch
            {
                if (File.Exists(MZA_Path))
                    File.Delete(MZA_Path);
                OnTaskFaulted(Task);
                e.Result = null;
            }
            MangaArchiveZipWorker.ReportProgress(100);
        }
        #endregion

        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            MangaArchiveZipWorker.Dispose();
        }
        #endregion
    }
}

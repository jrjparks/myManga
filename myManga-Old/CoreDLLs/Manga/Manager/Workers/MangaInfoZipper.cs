using System;
using System.ComponentModel;
using System.IO;
using BakaBox.Controls.Threading;
using Manga.Core;
using Manga.Info;
using Manga.Zip;

namespace Manga.Manager
{
    public class MIZevents
    {
        public delegate void MIZA_Saved(Object sender, String Path, MangaInfo MangaInfo);
    }

    internal class MangaInfoZipper : IDisposable
    {
        #region Events
        public event QueuedWorker<MangaInfoCoverData>.TaskDelegate TaskAdded;
        protected virtual void OnTaskAdded(QueuedTask<MangaInfoCoverData> Task)
        {
            if (TaskAdded != null)
                TaskAdded(this, Task);
        }
        public event QueuedWorker<MangaInfoCoverData>.TaskDelegate TaskBeginning;
        protected virtual void OnTaskBeginning(QueuedTask<MangaInfoCoverData> Task)
        {
            if (TaskBeginning != null)
                TaskBeginning(this, Task);
        }
        public event QueuedWorker<MangaInfoCoverData>.TaskDelegate TaskComplete;
        protected virtual void OnTaskComplete(QueuedTask<MangaInfoCoverData> Task)
        {
            if (TaskComplete != null)
                TaskComplete(this, Task);
        }
        public event QueuedWorker<MangaInfoCoverData>.TaskDelegate TaskProgress;
        protected virtual void OnTaskProgress(QueuedTask<MangaInfoCoverData> Task)
        {
            if (TaskProgress != null)
                TaskProgress(this, Task);
        }
        public event QueuedWorker<MangaInfoCoverData>.TaskDelegate TaskFaulted;
        protected virtual void OnTaskFaulted(QueuedTask<MangaInfoCoverData> Task)
        {
            if (TaskFaulted != null)
                TaskFaulted(this, Task);
        }
        public event QueuedWorker<MangaInfoCoverData>.TaskDelegate TaskRemoved;
        protected virtual void OnTaskRemoved(QueuedTask<MangaInfoCoverData> Task)
        {
            if (TaskRemoved != null)
                TaskRemoved(this, Task);
        }

        public event QueuedWorker<MangaInfoCoverData>.CompleteDelegate QueueComplete;
        protected virtual void OnQueueComplete()
        {
            if (QueueComplete != null)
                QueueComplete(this);
        }


        public event MIZevents.MIZA_Saved MIZASaved;
        protected virtual void OnMIZASave(String Path, MangaInfo MangaInfo) // Haha I'm thinking Jar Jar Binks...
        {
            if (MIZASaved != null)
                MIZASaved(this, Path, MangaInfo);
        }
        #endregion

        #region Vars
        private readonly QueuedBackgroundWorker<MangaInfoCoverData> MangaInfoZipWorker;
        public QueuedTask<MangaInfoCoverData> ActiveTask { get { return MangaInfoZipWorker.ActiveTask; } }
        private String mdZip_SaveLocation { get; set; }
        #endregion

        public MangaInfoZipper()
        {
            mdZip_SaveLocation = MangaDataZip.DefaultSaveLocation;
            MangaInfoZipWorker = new QueuedBackgroundWorker<MangaInfoCoverData>();
            MIZippperCore();
        }
        public MangaInfoZipper(String MangaDataZip_SaveLocation)
        {
            mdZip_SaveLocation = MangaDataZip_SaveLocation;
            MangaInfoZipWorker = new QueuedBackgroundWorker<MangaInfoCoverData>();
            MIZippperCore();
        }
        private void MIZippperCore()
        {
            MangaInfoZipWorker.WorkerReportsProgress = true;
            MangaInfoZipWorker.DoWork += QueuedZipSaver_DoWork;
            MangaInfoZipWorker.RunWorkerCompleted += QueuedZipSaver_RunWorkerCompleted;
            MangaInfoZipWorker.TaskAdded += (s, t) => OnTaskAdded(t);
            MangaInfoZipWorker.TaskBeginning += (s, t) => OnTaskBeginning(t);
            MangaInfoZipWorker.TaskComplete += (s, t) => OnTaskBeginning(t);
            MangaInfoZipWorker.TaskProgress += (s, t) => OnTaskProgress(t);
            MangaInfoZipWorker.TaskFaulted += (s, t) => OnTaskFaulted(t);
            MangaInfoZipWorker.TaskRemoved += (s, t) => OnTaskRemoved(t);
            MangaInfoZipWorker.QueueComplete += (s) => OnQueueComplete();
        }
        public void UpdateSavePath(String Path)
        { mdZip_SaveLocation = Path; }
        
        #region Zip Events

        #region Public
        public Guid AddMIToQueue(MangaInfo Data)
        { return AddMIToQueue(new MangaInfoCoverData(Data, null), Guid.NewGuid()); }
        public Guid AddMIToQueue(MangaInfoCoverData Data)
        { return AddMIToQueue(Data, Guid.NewGuid()); }
        public Guid AddMIToQueue(MangaInfo Data, Guid TaskID)
        { return AddMIToQueue(new QueuedTask<MangaInfoCoverData>(new MangaInfoCoverData(Data, null), TaskID)); }
        public Guid AddMIToQueue(MangaInfoCoverData Data, Guid TaskID)
        { return AddMIToQueue(new QueuedTask<MangaInfoCoverData>(Data, TaskID)); }
        public Guid AddMIToQueue(QueuedTask<MangaInfoCoverData> Task)
        {
            return MangaInfoZipWorker.AddToQueue(Task);
        }

        public void ClearQueue()
        {
            MangaInfoZipWorker.ClearQueue();
        }

        public void CancelQueue()
        {
            MangaInfoZipWorker.CancelQueue();
        }

        public Boolean CancelTask(Guid TaskID)
        {
            return MangaInfoZipWorker.CancelTask(TaskID);
        }
        #endregion

        #region Private
        private void QueuedZipSaver_RunWorkerCompleted(Object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result != null)
            {
                TransferClass Result = e.Result as TransferClass;
                OnMIZASave(Result.Data[1] as String, (Result.Data[0] as MangaInfoCoverData).MangaInfo);
            }
            GC.Collect();
        }

        private void QueuedZipSaver_DoWork(Object sender, DoWorkEventArgs e)
        {
            QueuedTask<MangaInfoCoverData> Task = e.Argument as QueuedTask<MangaInfoCoverData>;
            String MIZA_Path = Path.Combine(IO.SafeFolder(Path.Combine(Environment.CurrentDirectory, "MangaInfo")), IO.SafeFileName(Task.Data.MangaInfo.MISaveName));
            try
            {
                MangaInfoZipWorker.WorkerCancellationToken.Token.ThrowIfCancellationRequested();
                MangaInfoZipWorker.ReportProgress(5);
                MangaInfoCoverData MICD = Task.Data;
                MangaInfoZipWorker.ReportProgress(10);

                using (MangaDataZip mdZip = new MangaDataZip(mdZip_SaveLocation))
                {
                    mdZip.ProgressChanged += (s, p, t, d) =>
                    {
                        if (t == MangaDataMethods.FileType.MIZA)
                            MangaInfoZipWorker.ReportProgress((Int32)Math.Round((Double)p * 0.7D) + 10);
                    };
                    if (!File.Exists(MIZA_Path))
                        MIZA_Path = mdZip.WriteMIZA(MICD.MangaInfo, MICD.CoverData, MIZA_Path);
                    else
                        MIZA_Path = mdZip.UpdateMIZA(MICD.MangaInfo, MIZA_Path);
                }

                MangaInfoZipWorker.ReportProgress(80);
                e.Result = new TransferClass() { Data = new Object[] { MICD, MIZA_Path } };
            }
            catch
            {
                if (File.Exists(MIZA_Path))
                    File.Delete(MIZA_Path);
                OnTaskFaulted(Task);
                e.Result = null;
            }
            MangaInfoZipWorker.ReportProgress(100);
        }
        #endregion

        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            MangaInfoZipWorker.Dispose();
        }
        #endregion
    }
}

using System;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BakaBox.Controls.Threading;
using Manga.Archive;
using Manga.Core;
using Manga.Zip;
using BakaBox.Controls;

namespace Manga.Manager
{
    public class RemoteImageLocation
    {
        public String RemotePath { get; set; }
        public String AltRemotePath { get; set; }

        public RemoteImageLocation()
            : this(String.Empty, String.Empty)
        { }
        public RemoteImageLocation(String RemotePath)
            : this(RemotePath, String.Empty)
        { }
        public RemoteImageLocation(String RemotePath, String AltRemotePath)
        {
            this.RemotePath = RemotePath;
            this.AltRemotePath = AltRemotePath;
        }
    }
    public class ImageRequest
    {
        public Guid TaskID { get; set; }
        public Boolean AutoRetry { get; set; }
        public String LocalFolderPath { get; set; }
        public TaskStatus Status { get; set; }
        public MangaArchiveInfo MangaArchiveInfo { get; set; }
    }
    internal class MangaImageDownloader : IDisposable
    {
        #region Delegates & Events
        public event QueuedWorker<ImageRequest>.TaskDelegate TaskAdded;
        protected virtual void OnTaskAdded(QueuedTask<ImageRequest> Task)
        {
            if (TaskAdded != null)
                TaskAdded(this, Task);
        }
        public event QueuedWorker<ImageRequest>.TaskDelegate TaskBeginning;
        protected virtual void OnTaskBeginning(QueuedTask<ImageRequest> Task)
        {
            if (TaskBeginning != null)
                TaskBeginning(this, Task);
        }
        public event QueuedWorker<ImageRequest>.TaskDelegate TaskComplete;
        protected virtual void OnTaskComplete(QueuedTask<ImageRequest> Task)
        {
            if (TaskComplete != null)
                TaskComplete(this, Task);
        }
        public event QueuedWorker<ImageRequest>.TaskDelegate TaskProgress;
        protected virtual void OnTaskProgress(QueuedTask<ImageRequest> Task)
        {
            if (TaskProgress != null)
                TaskProgress(this, Task);
        }
        public event QueuedWorker<ImageRequest>.TaskDelegate TaskFaulted;
        protected virtual void OnTaskFaulted(QueuedTask<ImageRequest> Task)
        {
            if (TaskFaulted != null)
                TaskFaulted(this, Task);
        }
        public event QueuedWorker<ImageRequest>.TaskDelegate TaskRemoved;
        protected virtual void OnTaskRemoved(QueuedTask<ImageRequest> Task)
        {
            if (TaskRemoved != null)
                TaskRemoved(this, Task);
        }

        public event QueuedWorker<ImageRequest>.CompleteDelegate QueueComplete;
        protected virtual void OnQueueComplete()
        {
            if (QueueComplete != null)
                QueueComplete(this);
        }
        #endregion

        #region Variables

        private readonly Random rNumber;
        private readonly QueuedBackgroundWorker<ImageRequest> QueuedImageWorker;

        public QueuedTask<ImageRequest> ActiveTransfer { get { return QueuedImageWorker.ActiveTask; } }
        #endregion

        #region Methods

        #region Constructor
        public MangaImageDownloader()
        {
            rNumber = new Random();

            QueuedImageWorker = new QueuedBackgroundWorker<ImageRequest>();
            QueuedImageWorker.WorkerReportsProgress = true;
            QueuedImageWorker.DoWork += QueuedImageWorker_DoWork;
            QueuedImageWorker.TaskAdded += (s, t) => OnTaskAdded(t);
            QueuedImageWorker.TaskBeginning += (s, t) => OnTaskBeginning(t);
            QueuedImageWorker.TaskComplete += (s, t) => OnTaskComplete(t);
            QueuedImageWorker.TaskProgress += (s, t) => OnTaskProgress(t);
            QueuedImageWorker.TaskFaulted += (s, t) => OnTaskFaulted(t);
            QueuedImageWorker.TaskRemoved += (s, t) => OnTaskRemoved(t);
            QueuedImageWorker.QueueComplete += (s) => OnQueueComplete();
        }
        #endregion

        #region ImageWorker
        void QueuedImageWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            QueuedImageWorker.ReportProgress(1);
            ImageRequest IR = default(ImageRequest);
            lock (ActiveTransfer)
            {
                ActiveTransfer.Data.Status = TaskStatus.Running;
                IR = ActiveTransfer.Data;
            }
            LocationInfoCollection LIC = IR.MangaArchiveInfo.PageEntries.DownloadLocations;
            Double Step = 98D / (Double)LIC.Count, Progress = 2D;
            QueuedImageWorker.ReportProgress((Int32)Math.Round(Progress));

            ParallelOptions DownloaderParallelOptions = new ParallelOptions();
            DownloaderParallelOptions.CancellationToken = QueuedImageWorker.WorkerCancellationToken.Token;

            if (!Directory.Exists(IO.SafeFolder(IR.LocalFolderPath)))
                Directory.CreateDirectory(IO.SafeFolder(IR.LocalFolderPath));

            IR.Status = TaskStatus.RanToCompletion;
            try
            {
                if (!QueuedImageWorker.WorkerCancellationToken.IsCancellationRequested)
                {
                    Parallel.ForEach(LIC, DownloaderParallelOptions, (ImageLocation, LoopState) =>
                    {
                        if (!LoopState.ShouldExitCurrentIteration)
                        {
                            String RemotePath, LocalPath;
                            UInt32 TriesLeft, FileSize;
                            if (!QueuedImageWorker.WorkerCancellationToken.IsCancellationRequested ||
                                !DownloaderParallelOptions.CancellationToken.IsCancellationRequested)
                            {
                                RemotePath = ImageLocation.FullOnlinePath;
                                LocalPath = Path.Combine(IO.SafeFolder(IR.LocalFolderPath), IO.SafeFileName(Path.GetFileName(RemotePath)));
                                TriesLeft = 3;

                                #region WebClient
                                using (WebClient WebClient = new WebClient())
                                {
                                    MatchCollection RandomNumbers = Regex.Matches(RemotePath, @"\[R(\d+)-(\d+)\]");
                                    Random r = new Random();
                                    Int32 rNumber;
                                    foreach (Match rNumberMatch in RandomNumbers)
                                    {
                                        rNumber = r.Next(Int32.Parse(rNumberMatch.Groups[1].Value), Int32.Parse(rNumberMatch.Groups[2].Value));
                                        RemotePath = RemotePath.Replace(rNumberMatch.Value, rNumber.ToString());
                                    }
                                retryDownload:
                                    if (!LoopState.ShouldExitCurrentIteration)
                                    {
                                        try
                                        {
                                            FileInfo localFile = new FileInfo(LocalPath);
                                            if (localFile.Exists)
                                                localFile.Delete();

                                            WebClient.DownloadFile(RemotePath, LocalPath);
                                            FileSize = UInt32.Parse(WebClient.ResponseHeaders[System.Net.HttpResponseHeader.ContentLength] as String);
                                            localFile = new FileInfo(LocalPath);
                                            if (!localFile.Exists)
                                                throw new Exception("File not downloaded.");
                                            else if (localFile.Length < FileSize)
                                            {
                                                localFile.Delete();
                                                throw new Exception("File not completely downloaded.");
                                            }
                                            IR.MangaArchiveInfo.PageEntries.GetPageByFileName(Path.GetFileName(LocalPath)).Downloaded = true;
                                            IR.MangaArchiveInfo.PageEntries.GetPageByFileName(Path.GetFileName(LocalPath)).FileSize = FileSize;
                                            QueuedImageWorker.ReportProgress(
                                                        (Int32)Math.Round(Progress += Step),
                                                        new QueuedTaskInfo<String>()
                                                        {
                                                            Status = TaskStatus.RanToCompletion,
                                                            Data = LocalPath,
                                                            ID = IR.TaskID
                                                        });
                                        }
                                        catch
                                        {
                                            if (IR.AutoRetry)
                                            {
                                                if (TriesLeft.Equals(0))
                                                    QueuedImageWorker.ReportProgress(
                                                        (Int32)Math.Round(Progress += Step),
                                                        new QueuedTaskInfo<String>()
                                                        {
                                                            Status = TaskStatus.Faulted,
                                                            Data = RemotePath,
                                                            ID = IR.TaskID
                                                        });
                                                else if (TriesLeft-- > 0)
                                                {
                                                    if (ImageLocation.AltOnlinePath != null)
                                                        RemotePath = ImageLocation.FullAltOnlinePath;
                                                    goto retryDownload;
                                                }
                                            }
                                        }
                                    }
                                }
                                #endregion
                            }
                            else
                            {
                                IR.Status = TaskStatus.Canceled;
                                LoopState.Stop();
                            }
                        }
                    });
                }
                e.Result = IR;
            }
            catch (Exception Ex)
            {
                IR.Status = TaskStatus.Faulted;
                e.Result = Ex;
                lock (ActiveTransfer)
                {
                    ActiveTransfer.Data.Status = TaskStatus.Faulted;
                }
                OnTaskFaulted(ActiveTransfer);
            }
            if (IR.Status != TaskStatus.RanToCompletion)
            {
                if (QueuedImageWorker.IsQueueEmpty)
                    MangaDataZip.CleanUnusedFolders(IR.MangaArchiveInfo, IR.MangaArchiveInfo.TmpFolderLocation);
                else
                    Directory.Delete(IR.LocalFolderPath, true);
            }
        }
        #endregion

        #region Queue Files

        public Guid AddFilesToQueue(MangaArchiveInfo MangaArchiveInfo)
        { return AddFilesToQueue(MangaArchiveInfo.TmpFolderLocation, Guid.NewGuid(), false, MangaArchiveInfo); }
        public Guid AddFilesToQueue(String LocalPath, MangaArchiveInfo MangaArchiveInfo)
        { return AddFilesToQueue(LocalPath, Guid.NewGuid(), false, MangaArchiveInfo); }
        public Guid AddFilesToQueue(String LocalPath, Guid QueueKey, MangaArchiveInfo MangaArchiveInfo)
        { return AddFilesToQueue(LocalPath, QueueKey, false, MangaArchiveInfo); }
        public Guid AddFilesToQueue(String LocalPath, Boolean AutoRetry, MangaArchiveInfo MangaArchiveInfo)
        { return AddFilesToQueue(LocalPath, Guid.NewGuid(), AutoRetry, MangaArchiveInfo); }
        public Guid AddFilesToQueue(String LocalPath, Guid QueueKey, Boolean AutoRetry, MangaArchiveInfo MangaArchiveInfo)
        {
            QueuedImageWorker.AddToQueue(new QueuedTask<ImageRequest>(
                new ImageRequest()
                {
                    LocalFolderPath = IO.SafeFolder(LocalPath),
                    TaskID = QueueKey,
                    AutoRetry = AutoRetry,
                    MangaArchiveInfo = MangaArchiveInfo,
                    Status = TaskStatus.WaitingToRun
                }, 
                QueueKey));
            return QueueKey;
        }
        #endregion

        #region QueuedWorker Methods
        public void ClearQueue()
        {
            QueuedImageWorker.ClearQueue();
        }

        public void CancelQueue()
        {
            QueuedImageWorker.CancelQueue();
        }

        public Boolean CancelTask(Guid TaskID)
        {
            return QueuedImageWorker.CancelTask(TaskID);
        }
        #endregion

        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            QueuedImageWorker.Dispose();
        }
        #endregion
    }
}

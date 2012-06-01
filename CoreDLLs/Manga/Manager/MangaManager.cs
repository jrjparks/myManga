using System;
using System.Collections.Generic;
using System.IO;
using Manga.Archive;
using Manga.Core;
using Manga.Info;
using Manga.Plugin;
using Manga.Zip;
using BakaBox.Controls.Threading;

namespace Manga.Manager
{
    public class MangaManager : IDisposable
    {
        #region Instance
        private MangaManager _Instance;
        public MangaManager Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new MangaManager();
                return _Instance;
            }
        }
        #endregion

        #region Events
        public event MPUevents.MPUTaskDelegate NewMAI_Name;
        protected virtual void OnNewMAI_Name(Object Sender, QueuedTask<RequestTransfer> Task, MangaData MD)
        {
            if (NewMAI_Name != null)
                NewMAI_Name(Sender, Task, MD);
        }
        public event MAZevents.MZA_Saved MZASaved;
        protected virtual void OnMZASave(String Path, MangaArchiveInfo MangaArchiveInfo)
        {
            if (MZASaved != null)
                MZASaved(this, Path, MangaArchiveInfo);
        }
        public event MIZevents.MIZA_Saved MIZASaved;
        protected virtual void OnMIZASave(String Path, MangaInfo MangaInfo)
        {
            if (MIZASaved != null)
                MIZASaved(this, Path, MangaInfo);
        }


        public event QueuedWorker<Object>.TaskDelegate TaskAdded;
        protected virtual void OnTaskAdded(Object Sender, QueuedTask<Object> Task)
        {
            if (TaskAdded != null)
                TaskAdded(Sender, Task);
        }
        public event QueuedWorker<Object>.TaskDelegate TaskBeginning;
        protected virtual void OnTaskBeginning(Object Sender, QueuedTask<Object> Task)
        {
            if (TaskBeginning != null)
                if (!_RecentlyRemovedTasks.Contains(Task.Guid))
                    TaskBeginning(Sender, Task);
        }
        public event QueuedWorker<Object>.TaskDelegate TaskComplete;
        protected virtual void OnTaskComplete(Object Sender, QueuedTask<Object> Task)
        {
            if (TaskComplete != null)
                if (!_RecentlyRemovedTasks.Contains(Task.Guid))
                    TaskComplete(Sender, Task);
        }
        public event QueuedWorker<Object>.TaskDelegate TaskProgress;
        protected virtual void OnTaskProgress(Object Sender, QueuedTask<Object> Task)
        {
            if (TaskProgress != null)
                if (!_RecentlyRemovedTasks.Contains(Task.Guid))
                    TaskProgress(Sender, Task);
        }
        public event QueuedWorker<Object>.TaskDelegate TaskFaulted;
        protected virtual void OnTaskFaulted(Object Sender, QueuedTask<Object> Task)
        {
            if (TaskFaulted != null)
                if (!_RecentlyRemovedTasks.Contains(Task.Guid))
                    TaskFaulted(Sender, Task);
        }
        public event QueuedWorker<Object>.TaskDelegate TaskRemoved;
        protected virtual void OnTaskRemoved(Object Sender, QueuedTask<Object> Task)
        {
            if (TaskRemoved != null)
                TaskRemoved(Sender, Task);
        }

        public event QueuedWorker<Object>.CompleteDelegate QueueComplete;
        protected virtual void OnQueueComplete()
        {
            if (QueueComplete != null)
                QueueComplete(this);
        }
        #endregion

        #region Vars

        #region Enum
        public enum mDataType { Info, Chapter }
        #endregion

        #region Private
        private readonly ManagerPluginUser PluginUser;
        private readonly MangaImageDownloader MangaImageWorker;
        private readonly MangaArchiveZipper MAZipWorker;
        private readonly MangaInfoZipper MIZipWorker;

        private readonly Queue<Guid> _RecentlyRemovedTasks;

        private MangaDataZip MangaDataZip;
        #endregion

        #region Public
        public QueuedTask<RequestTransfer> ActivePluginTask { get { return PluginUser.ActiveRequest; } }
        public QueuedTask<MangaArchiveInfo> ActiveMAZipTask { get { return MAZipWorker.ActiveTask; } }
        public QueuedTask<MangaInfoCoverData> ActiveMIZipTask { get { return MIZipWorker.ActiveTask; } }
        #endregion

        #endregion

        #region Methods
        #region Public

        #region Constructors
        public MangaManager()
        {
            MangaDataZip = new MangaDataZip();
            _RecentlyRemovedTasks = new Queue<Guid>();
            #region PluginUser
            PluginUser = new ManagerPluginUser();
            #endregion

            #region MangaImageWorker
            MangaImageWorker = new MangaImageDownloader();
            #endregion

            #region MAZipWorker
            MAZipWorker = new MangaArchiveZipper(MangaDataZip.SaveLocation);
            #endregion

            #region MIZipWorker
            MIZipWorker = new MangaInfoZipper(MangaDataZip.SaveLocation);
            #endregion

            CreateEvents();
        }
        public MangaManager(ref MangaDataZip MangaDataZip)
        {
            this.MangaDataZip = MangaDataZip;
            _RecentlyRemovedTasks = new Queue<Guid>();
            #region PluginUser
            PluginUser = new ManagerPluginUser();
            #endregion

            #region MangaImageWorker
            MangaImageWorker = new MangaImageDownloader();
            #endregion

            #region MAZipWorker
            MAZipWorker = new MangaArchiveZipper(MangaDataZip.SaveLocation);
            #endregion

            #region MIZipWorker
            MIZipWorker = new MangaInfoZipper(MangaDataZip.SaveLocation);
            #endregion

            CreateEvents();
        }
        private void CreateEvents()
        {
            #region PluginUser
            PluginUser.TaskAdded += (s, t) => OnTaskAdded(s, ToGenericTask(s, t));
            PluginUser.TaskBeginning += (s, t) => OnTaskBeginning(s, ToGenericTask(s, t));
            PluginUser.TaskComplete += (s, t) =>
            {
                #region Plugin Task Complete
                OnTaskComplete(s, ToGenericTask(s, t));
                if (t.Data.mData != null)
                    switch (t.Data.RequestType)
                    {
                        case DataType.ChapterInformation:
                            DownloadMangaImage(t.Data.mData as MangaArchiveInfo, t.Guid);
                            break;

                        case DataType.MangaInformation:
                            MIZipWorker.AddMIToQueue(t.Data.mData as MangaInfoCoverData, t.Guid);
                            break;

                        default: break;
                    }
                else OnTaskFaulted(s, ToGenericTask(s, t));
                #endregion
            };
            PluginUser.TaskProgress += (s, t) => OnTaskProgress(s, ToGenericTask(s, t));
            PluginUser.TaskFaulted += (s, t) => OnTaskFaulted(s, ToGenericTask(s, t));
            PluginUser.TaskRemoved += (s, t) => OnTaskRemoved(s, ToGenericTask(s, t));
            PluginUser.QueueComplete += (s) => OnQueueComplete();
            PluginUser.NewMAI_Name += (s, t, m) => OnNewMAI_Name(s, t, m);
            #endregion

            #region MangaImageWorker
            MangaImageWorker.TaskAdded += (s, t) => OnTaskAdded(s, ToGenericTask(s, t));
            MangaImageWorker.TaskBeginning += (s, t) => OnTaskBeginning(s, ToGenericTask(s, t));
            MangaImageWorker.TaskComplete += (s, t) =>
            {
                #region ImageDownloader Task Complete
                OnTaskComplete(s, ToGenericTask(s, t));
                if (t.Data.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
                    MAZipWorker.AddMAIToQueue(t.Data.MangaArchiveInfo, t.Guid);
                GC.Collect();
                #endregion
            };
            MangaImageWorker.TaskProgress += (s, t) => OnTaskProgress(s, ToGenericTask(s, t));
            MangaImageWorker.TaskFaulted += (s, t) => OnTaskFaulted(s, ToGenericTask(s, t));
            MangaImageWorker.TaskRemoved += (s, t) => OnTaskRemoved(s, ToGenericTask(s, t));
            MangaImageWorker.QueueComplete += (s) => OnQueueComplete();
            #endregion

            #region MAZipWorker
            MAZipWorker.TaskAdded += (s, t) => OnTaskAdded(s, ToGenericTask(s, t));
            MAZipWorker.MZASaved += (s, p, m) =>
            {
                OnMZASave(p, m);
            };
            MAZipWorker.TaskBeginning += (s, t) => OnTaskBeginning(s, ToGenericTask(s, t));
            MAZipWorker.TaskComplete += (s, t) =>
            {
                OnTaskComplete(s, ToGenericTask(s, t));
            };
            MAZipWorker.TaskProgress += (s, t) => OnTaskProgress(s, ToGenericTask(s, t));
            MAZipWorker.TaskFaulted += (s, t) => OnTaskFaulted(s, ToGenericTask(s, t));
            MAZipWorker.TaskRemoved += (s, t) => OnTaskRemoved(s, ToGenericTask(s, t));
            MAZipWorker.QueueComplete += (s) => OnQueueComplete();
            #endregion

            #region MIZipWorker
            MIZipWorker.TaskAdded += (s, t) => OnTaskAdded(s, ToGenericTask(s, t));
            MIZipWorker.MIZASaved += (s, p, m) => OnMIZASave(p, m);
            MIZipWorker.TaskBeginning += (s, t) => OnTaskBeginning(s, ToGenericTask(s, t));
            MIZipWorker.TaskComplete += (s, t) => OnTaskComplete(s, ToGenericTask(s, t));
            MIZipWorker.TaskProgress += (s, t) => OnTaskProgress(s, ToGenericTask(s, t));
            MIZipWorker.TaskFaulted += (s, t) => OnTaskFaulted(s, ToGenericTask(s, t));
            MIZipWorker.TaskRemoved += (s, t) => OnTaskRemoved(s, ToGenericTask(s, t));
            MIZipWorker.QueueComplete += (s) => OnQueueComplete();
            #endregion

            #region MangaDataZip
            MangaDataZip.PropertyChanged += (s, p) =>
            {
                if (p.PropertyName.Equals("SaveLocation"))
                {
                    MAZipWorker.UpdateSavePath(MangaDataZip.SaveLocation);
                    MIZipWorker.UpdateSavePath(MangaDataZip.SaveLocation);
                }
            };
            #endregion
        }
        #endregion

        #region Plugin Methods
        public void AddPluginClass(IMangaPlugin Plugin)
        {
            PluginUser.AddPluginClass(Plugin);
        }
        public Boolean RemovePluginClass(IMangaPlugin Plugin)
        {
            return PluginUser.RemovePluginClass(Plugin);
        }
        public Boolean RemovePluginClass(Int32 Index)
        {
            return PluginUser.RemovePluginClass(Index);
        }
        #endregion

        #region Task Methods
        public Guid DownloadChapterInfo(String Path)
        {
            return DownloadChapterInfo(Path, Guid.NewGuid());
        }
        public Guid DownloadChapterInfo(String Path, Guid TaskID)
        {
            PluginUser.RequestChapterInfo(Path, TaskID);
            return TaskID;
        }

        public Guid DownloadInfo(String Path)
        {
            return DownloadInfo(Path, Guid.NewGuid());
        }
        public Guid DownloadInfo(String Path, Guid TaskID)
        {
            PluginUser.RequestMangaInfo(Path, TaskID);
            return TaskID;
        }

        public Guid UpdateInfo(MangaInfo MangaInfo)
        {
            return UpdateInfo(MangaInfo, Guid.NewGuid());
        }
        public Guid UpdateInfo(MangaInfo MangaInfo, Guid TaskID)
        {
            PluginUser.RequestMangaInfo(MangaInfo, TaskID);
            return TaskID;
        }

        public Guid DownloadMangaImage(MangaArchiveInfo MAI)
        {
            return DownloadMangaImage(MAI, Guid.NewGuid());
        }
        public Guid DownloadMangaImage(MangaArchiveInfo MAI, Guid TaskID)
        {
            return MangaImageWorker.AddFilesToQueue(MAI.TmpFolderLocation, TaskID, true, MAI);
        }

        public void CancelTask(Guid TaskID)
        {
            _RecentlyRemovedTasks.Enqueue(TaskID);
            if (_RecentlyRemovedTasks.Count > 10)
                _RecentlyRemovedTasks.Dequeue();

            if (!PluginUser.CancelTask(TaskID))
                if (!MangaImageWorker.CancelTask(TaskID))
                    MAZipWorker.CancelTask(TaskID);
        }
        #endregion

        #endregion

        #region Private
        #region QueuedTask Conversions
        private QueuedTask<Object> ToGenericTask(Object Sender, QueuedTask<RequestTransfer> Task)
        {
            QueuedTask<Object> GenericTask = new QueuedTask<Object>(Task.Data, Task.Guid);
            GenericTask.SetProgress(Task.Progress);
            GenericTask.SetStatus(Task.TaskStatus);
            return GenericTask;
        }
        private QueuedTask<Object> ToGenericTask(Object Sender, QueuedTask<ImageRequest> Task)
        {
            QueuedTask<Object> GenericTask = new QueuedTask<Object>(Task.Data, Task.Guid);
            GenericTask.SetProgress(Task.Progress);
            GenericTask.SetStatus(Task.TaskStatus);
            return GenericTask;
        }
        private QueuedTask<Object> ToGenericTask(Object Sender, QueuedTask<MangaArchiveInfo> Task)
        {
            QueuedTask<Object> GenericTask = new QueuedTask<Object>(Task.Data, Task.Guid);
            GenericTask.SetProgress(Task.Progress);
            GenericTask.SetStatus(Task.TaskStatus);
            return GenericTask;
        }
        private QueuedTask<Object> ToGenericTask(Object Sender, QueuedTask<MangaInfoCoverData> Task)
        {
            QueuedTask<Object> GenericTask = new QueuedTask<Object>(Task.Data, Task.Guid);
            GenericTask.SetProgress(Task.Progress);
            GenericTask.SetStatus(Task.TaskStatus);
            return GenericTask;
        }
        #endregion
        #endregion
        #endregion
        
        #region IDisposable Members
        public void Dispose()
        {
            PluginUser.Dispose();
            MangaImageWorker.Dispose();
            MAZipWorker.Dispose();
            MIZipWorker.Dispose();
        }
        #endregion
    }

    internal class TransferClass
    {
        public Object[] Data { get; set; }
    }
}

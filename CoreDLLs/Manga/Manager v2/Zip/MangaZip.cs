using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using BakaBox.Controls.Threading;
using System.IO;

namespace Manga.Manager_v2.Zip
{
#if RELEASE
    [DebuggerStepThrough]
#endif
    class MangaZip
    {
        #region Constructor

        private readonly QueuedBackgroundWorker<ZipDataHandler> _ArchiveWorker;

        public MangaZip()
        {
            _ArchiveWorker = new QueuedBackgroundWorker<ZipDataHandler>();
            _ArchiveWorker.WorkerReportsProgress = _ArchiveWorker.WorkerSupportsCancellation = true;
            _ArchiveWorker.DoWork += _ArchiveWorker_DoWork;
            _ArchiveWorker.TaskFaulted += _ArchiveWorker_TaskFaulted;
            _ArchiveWorker.TaskComplete += _ArchiveWorker_TaskComplete;
        }
        #endregion

        #region Events

        public event EventHandler TaskAdded;
        private void OnTaskAdded(EventArgs e)
        {
            if (TaskAdded != null)
                TaskAdded(this, e);
        }

        public event EventHandler TaskUpdated;
        private void OnTaskUpdated(EventArgs e)
        {
            if (TaskUpdated != null)
                TaskUpdated(this, e);
        }

        public event EventHandler TaskCompleted;
        private void OnTaskCompleted(EventArgs e)
        {
            if (TaskCompleted != null)
                TaskCompleted(this, e);
        }

        public event EventHandler TaskFaulted;
        private void OnTaskFaulted(EventArgs e)
        {
            if (TaskFaulted != null)
                TaskFaulted(this, e);
        }

        #endregion

        #region Methods

        public void CreateArchive(ZipDataHandler value)
        {
            _ArchiveWorker.AddToQueue(value);
        }

        public void AddToArchive(ZipDataHandler value)
        {
            _ArchiveWorker.AddToQueue(value);
        }

        public void AddToArchive(Stream Data)
        {
            AddToArchive(new ZipDataHandler(Data));
        }

        public void RemoveFromArchive(ZipDataHandler value)
        {
            _ArchiveWorker.AddToQueue(value);
        }

        #endregion

        #region Worker

        private void _ArchiveWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            throw new NotImplementedException();
        }

        void _ArchiveWorker_TaskFaulted(object Sender, QueuedTask<ZipDataHandler> Task)
        {
            throw new NotImplementedException();
        }

        void _ArchiveWorker_TaskComplete(object Sender, QueuedTask<ZipDataHandler> Task)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

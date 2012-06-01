using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace BakaBox.Controls.Threading
{
    [DebuggerStepThrough]
    public class QueuedWorker<T>
    {
        public delegate void TaskDelegate(Object Sender, QueuedTask<T> Task);
        public delegate void CompleteDelegate(Object Sender);
    }
    [DebuggerStepThrough]
    public class TransferClass
    {
        public Guid TaskID { get; private set; }
        public Object[] Data { get; private set; }

        public TransferClass(Guid TaskID, params Object[] Data)
        {
            SetTaskID(TaskID);
            SetData(Data);
        }

        public void SetTaskID(Guid TaskID)
        { this.TaskID = TaskID; }
        public void SetData(params Object[] Data)
        { this.Data = Data; ; }
    }

    [DebuggerStepThrough]
    public class QueuedBackgroundWorker<T> : BackgroundWorker
    {
        #region Events
        public event QueuedWorker<T>.TaskDelegate TaskAdded;
        protected virtual void OnTaskAdded(QueuedTask<T> Task)
        {
            if (TaskAdded != null)
                TaskAdded(this, Task);
        }
        public event QueuedWorker<T>.TaskDelegate TaskBeginning;
        protected virtual void OnTaskBeginning(QueuedTask<T> Task)
        {
            if (TaskBeginning != null)
                TaskBeginning(this, Task);
        }
        public event QueuedWorker<T>.TaskDelegate TaskComplete;
        protected virtual void OnTaskComplete(QueuedTask<T> Task)
        {
            if (TaskComplete != null)
                TaskComplete(this, Task);
        }
        public event QueuedWorker<T>.TaskDelegate TaskProgress;
        protected virtual void OnTaskProgress(QueuedTask<T> Task)
        {
            if (TaskProgress != null)
                TaskProgress(this, Task);
        }
        public event QueuedWorker<T>.TaskDelegate TaskFaulted;
        protected virtual void OnTaskFaulted(QueuedTask<T> Task)
        {
            if (TaskFaulted != null)
                TaskFaulted(this, Task);
        }
        public event QueuedWorker<T>.TaskDelegate TaskRemoved;
        protected virtual void OnTaskRemoved(QueuedTask<T> Task)
        {
            if (TaskRemoved != null)
                TaskRemoved(this, Task);
        }

        public event QueuedWorker<T>.CompleteDelegate QueueComplete;
        protected virtual void OnQueueComplete()
        {
            if (QueueComplete != null)
                QueueComplete(this);
        }
        #endregion

        public Boolean IsPaused { get; private set; }
        public Boolean IsQueueEmpty { get { return DataQueue.Count.Equals(0); } }
        public CancellationTokenSource WorkerCancellationToken { get; private set; }
        public QueuedTask<T> ActiveTask { get; private set; }

        private Queue<QueuedTask<T>> _DataQueue;
        public Queue<QueuedTask<T>> DataQueue
        {
            get
            {
                if (_DataQueue == null)
                    _DataQueue = new Queue<QueuedTask<T>>();
                return _DataQueue;
            }
        }
        public QueuedBackgroundWorker()
            : base()
        {
            WorkerSupportsCancellation = true;
            IsPaused = false;
        }

        private void CheckForNewWork()
        {
            lock (this)
            {
                if (this.IsQueueEmpty)
                { ActiveTask = null; OnQueueComplete(); }
                else if (!IsPaused)
                    if (!this.IsBusy)
                    {
                        if (WorkerCancellationToken == null) { }
                        else if (WorkerCancellationToken.IsCancellationRequested)
                            WorkerCancellationToken.Dispose();
                        WorkerCancellationToken = new CancellationTokenSource();

                        ActiveTask = DataQueue.Dequeue();
                        ActiveTask.SetStatus(TaskStatus.WaitingToRun);
                        OnTaskBeginning(ActiveTask);
                        base.RunWorkerAsync(ActiveTask);
                    }
            }
        }

        #region Queue Adding
        private new void RunWorkerAsync()
        { }
        private new void RunWorkerAsync(Object Data)
        { }
        public void RunWorkerAsync(T Data)
        { AddToQueue(Data); }
        public Guid AddToQueue(T Data)
        { return AddToQueue(Data, Guid.NewGuid()); }
        public Guid AddToQueue(T Data, Guid TaskID)
        { return AddToQueue(new QueuedTask<T>(Data, TaskID)); }
        public Guid AddToQueue(QueuedTask<T> QueuedTask)
        {
            QueuedTask.SetStatus(TaskStatus.WaitingForActivation);
            lock (this)
            {
                DataQueue.Enqueue(QueuedTask);
            }
            OnTaskAdded(QueuedTask);
            CheckForNewWork();
            return QueuedTask.Guid;
        }
        #endregion

        #region Queue Destruction
        public void ClearQueue()
        {
            lock (this)
            {
                DataQueue.Clear();
            }
        }
        public void CancelQueue()
        {
            ClearQueue();
            base.CancelAsync();
        }

        public void CancelCurrentTask()
        {
            WorkerCancellationToken.Cancel();
            if (WorkerSupportsCancellation)
                CancelAsync();
        }
        public Boolean CancelTask(Guid Guid)
        {
            Boolean Canceled = false;
            QueuedTask<T> RemovedItem = default(QueuedTask<T>);
            if (ContainsTask(Guid))
            {
                lock (DataQueue)
                {
                    QueuedTask<T>[] QueueItems = new QueuedTask<T>[DataQueue.Count];
                    DataQueue.CopyTo(QueueItems, 0);
                    DataQueue.Clear();
                    foreach (QueuedTask<T> Item in QueueItems)
                    {
                        if (!Item.Guid.Equals(Guid))
                            DataQueue.Enqueue(Item);
                        else
                        {
                            RemovedItem = Item;
                            Canceled = true;
                        }
                    }
                }
            }
            else if (ActiveTask != null &&
                ActiveTask.Guid.Equals(Guid))
            {
                RemovedItem = ActiveTask;
                Canceled = true;
                CancelCurrentTask();
            }
            if (Canceled)
                OnTaskRemoved(RemovedItem);
            GC.Collect();
            return Canceled;
        }
        public Boolean ContainsTask(Guid Guid)
        {
            Boolean Contains = false;
            lock (DataQueue)
            {
                foreach (QueuedTask<T> Task in DataQueue)
                    if (Contains = Task.Guid.Equals(Guid))
                        break;
            }
            return Contains;
        }
        #endregion

        #region Work Control
        public void PauseWork()
        { IsPaused = true; }
        public void ResumeWork()
        {
            IsPaused = false;
            CheckForNewWork();
        }
        #endregion

        protected override void Dispose(Boolean Disposing)
        {
            CancelQueue();
            while (base.IsBusy)
                Thread.Sleep(1);
            base.Dispose(Disposing);
        }

        #region BackgroundWorker Members
        protected override void OnRunWorkerCompleted(RunWorkerCompletedEventArgs e)
        {
            lock (ActiveTask)
            {
                if (e.Cancelled)
                {
                    ActiveTask.SetStatus(TaskStatus.Canceled);
                    OnTaskRemoved(ActiveTask);
                }
                else
                {
                    ActiveTask.SetStatus(TaskStatus.RanToCompletion);
                    OnTaskComplete(ActiveTask);
                }
            }
            base.OnRunWorkerCompleted(e);
            CheckForNewWork();
        }

        protected override void OnProgressChanged(ProgressChangedEventArgs e)
        {
            lock (ActiveTask)
            {
                ActiveTask.SetProgress(e.ProgressPercentage);
                ActiveTask.SetStatus(TaskStatus.Running);
            }
            OnTaskProgress(ActiveTask);
            base.OnProgressChanged(e);
        }

        protected override void OnDoWork(DoWorkEventArgs e)
        {
            try
            {
                lock (ActiveTask)
                {
                    ActiveTask.SetStatus(TaskStatus.Running);
                }
                base.OnDoWork(e);
            }
            catch
            {
                lock (ActiveTask)
                {
                    ActiveTask.SetStatus(TaskStatus.Faulted);
                }
                OnTaskFaulted(ActiveTask);
            }
        }
        #endregion
    }

    [DebuggerStepThrough]
    public class QueuedTask<T> : BakaBox.MVVM.ModelBase
    {
        #region Variables
        private T _Data;
        public T Data { get; set; }

        private Guid _Guid;
        public Guid Guid
        {
            get { return _Guid; }
            private set
            {
                OnPropertyChanging("Guid");
                _Guid = value;
                OnPropertyChanged("Guid");
            }
        }

        private Int32 _Progress;
        public Int32 Progress
        {
            get { return _Progress; }
            private set
            {
                OnPropertyChanging("Progress");
                _Progress = value;
                OnPropertyChanged("Progress");
            }
        }

        private TaskStatus _TaskStatus;
        public TaskStatus TaskStatus
        {
            get { return _TaskStatus; }
            private set
            {
                OnPropertyChanging("TaskStatus");
                _TaskStatus = value;
                OnPropertyChanged("TaskStatus");
            }
        }
        #endregion

        public QueuedTask()
            : this(default(T), Guid.Empty, TaskStatus.Created) { }
        public QueuedTask(T Data, Guid ID)
            : this(Data, ID, TaskStatus.Created) { }
        public QueuedTask(T Data, Guid Guid, TaskStatus TaskStatus)
        {
            this.Data = Data;
            SetGuid(Guid);
            SetStatus(TaskStatus);
            SetProgress(0);
        }

        #region Members
        public Int32 SetProgress(Int32 Progress)
        { return (this.Progress = Progress); }

        public TaskStatus SetStatus(TaskStatus TaskStatus)
        { return (this.TaskStatus = TaskStatus); }

        internal Guid SetGuid(Guid Guid)
        { return (this.Guid = Guid); }
        #endregion
    }

    [DebuggerStepThrough]
    public class QueuedTaskInfo<T>
    {
        public T Data { get; set; }
        public Guid ID { get; set; }
        public TaskStatus Status { get; set; }

        public QueuedTaskInfo()
        {
            Data = default(T);
            ID = Guid.Empty;
            Status = TaskStatus.Created;
        }
    }
}

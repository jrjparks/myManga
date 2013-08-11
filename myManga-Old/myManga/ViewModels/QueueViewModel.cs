using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Input;
using BakaBox.MVVM;
using Manga.Manager;
using BakaBox.Controls.Threading;
using Manga;
using Manga.Core;
using Manga.Info;
using System.Windows;
using System.Threading;
using System.Collections.Generic;

namespace myManga.ViewModels
{
    public sealed class QueueViewModel : ViewModelBase
    {
        public QueueViewModel()
        {
            DownloadManager.Instance.TaskAdded += UpdateTaskData;
            DownloadManager.Instance.TaskBeginning += UpdateTaskData;
            DownloadManager.Instance.TaskProgress += UpdateTaskData;
            DownloadManager.Instance.TaskComplete += UpdateTaskData;

            DownloadManager.Instance.TaskFaulted += RemoveMangaTask;
            DownloadManager.Instance.TaskRemoved += RemoveMangaTask;

            DownloadManager.Instance.NameUpdated += (s, t) =>
            {
                Int32 Index = IndexOfTaskID(t.Guid);
                if (Index >= 0)
                {
                    MangaTasks[Index].Title = t.Data.Title;
                    MangaTasks[Index].Progress = t.Progress;
                }
                else
                    MangaTasks.Add(new ListQueueItem() { ID = t.Guid, Progress = t.Progress, Title = t.Data.Title });
            };
        }

        #region Events
        public delegate void OpenMZA(String _FileName);
        public event OpenMZA _OpenMZA;
        private void OnOpenMZA(String _FileName)
        {
            if (_OpenMZA != null)
                _OpenMZA(_FileName);
        }
        #endregion

        #region Methods
        #region Private
        private void UpdateTaskData(Object Sender, QueuedTask<ManagerData<String, MangaInfo>> Task)
        {
            if (!RecentlyRemoved.Contains(Task.Guid))
            {
                Int32 _TaskIDIndex = IndexOfTaskID(Task.Guid);
                if (!_TaskIDIndex.Equals(-1))
                    MangaTasks[_TaskIDIndex].Progress = Task.Progress;
                else
                    AddMangaTask(new ListQueueItem() { ID = Task.Guid, Progress = Task.Progress, Title = Task.Data.Title });
            }
        }
        private Int32 IndexOfTaskID(Guid TaskID)
        {
            if (MangaTasks.Count > 0)
                foreach (ListQueueItem Item in MangaTasks)
                {
                    if (Item.ID.Equals(TaskID))
                        return MangaTasks.IndexOf(Item);
                }
            return -1;
        }

        private void OpenMangaTask(Guid Guid)
        {
            String Title = MangaTasks[IndexOfTaskID(Guid)].Title;
            SendViewModelToastNotification(this, String.Format("Open: {0}", Title), TimeSpan.FromMilliseconds(500));
            OnOpenMZA(Title);
        }

        private void DeleteMangaTask(Guid Guid)
        {
            if (!DownloadManager.Instance.CancelTask(Guid))
                RemoveMangaTask(Guid);
        }

        private void RemoveMangaTask(Object Sender, QueuedTask<ManagerData<String, MangaInfo>> Task)
        {
            RemoveMangaTask(Task.Guid);
            SendViewModelToastNotification(this, String.Format("Removed Task: {0}\n{1}", Task.Data.Title, Task.Guid));
        }
        #endregion

        #region Public
        #endregion
        #endregion

        #region Lists
        private ObservableCollection<ListQueueItem> _MangaTasks;
        public ObservableCollection<ListQueueItem> MangaTasks
        {
            get
            {
                if (_MangaTasks == null)
                    _MangaTasks = new ObservableCollection<ListQueueItem>();
                return _MangaTasks;
            }
        }

        private List<Guid> _RecentlyRemoved;
        private List<Guid> RecentlyRemoved
        {
            get
            {
                if (_RecentlyRemoved == null)
                    _RecentlyRemoved = new List<Guid>(20);
                return _RecentlyRemoved;
            }
        }

        private delegate void RemoveMangaTaskDelegate(Guid value);
        private void RemoveMangaTask(Guid value)
        {
            if (Application.Current.Dispatcher.Thread == Thread.CurrentThread)
            {
                Int32 Index;
                if ((Index = IndexOfTaskID(value)) >= 0)
                {
                    MangaTasks.RemoveAt(Index);
                    if (RecentlyRemoved.Count == RecentlyRemoved.Capacity)
                        RecentlyRemoved.RemoveAt(RecentlyRemoved.Count - 1);
                    RecentlyRemoved.Add(value);
                }
            }
            else
                Application.Current.Dispatcher.Invoke(new RemoveMangaTaskDelegate(RemoveMangaTask), value);
        }

        private delegate void CleanMangaTasksDelegate();
        private void CleanMangaTasks()
        {
            if (Application.Current.Dispatcher.Thread == Thread.CurrentThread)
            {
                List<Guid> ItemGuids = new List<Guid>(MangaTasks.Count);
                foreach (ListQueueItem MangaItem in MangaTasks)
                    if (MangaItem.Progress == 100)
                        ItemGuids.Add(MangaItem.ID);
                foreach (Guid ID in ItemGuids)
                    RemoveMangaTask(ID);

            }
            else
                Application.Current.Dispatcher.Invoke(new CleanMangaTasksDelegate(CleanMangaTasks));
        }

        private delegate void AddMangaTaskDelegate(ListQueueItem value);
        private void AddMangaTask(ListQueueItem value)
        {
            if (Application.Current.Dispatcher.Thread == Thread.CurrentThread)
                MangaTasks.Add(value);
            else
                Application.Current.Dispatcher.Invoke(new AddMangaTaskDelegate(AddMangaTask), value);
        }
        #endregion

        #region Commands
        private DelegateCommand<Guid> _OpenTask { get; set; }
        public ICommand OpenTask
        {
            get
            {
                if (_OpenTask == null)
                    _OpenTask = new DelegateCommand<Guid>(OpenMangaTask, CanOpenTask);
                return _OpenTask;
            }
        }
        private Boolean CanOpenTask(Guid ID)
        {
            Int32 _Index = IndexOfTaskID(ID);
            if (!_Index.Equals(-1))
                if (MangaTasks[_Index].Progress == 100)
                    return true;
            return false;
        }

        private DelegateCommand<Guid> _DeleteTask { get; set; }
        public ICommand DeleteTask
        {
            get
            {
                if (_DeleteTask == null)
                    _DeleteTask = new DelegateCommand<Guid>(DeleteMangaTask);
                return _DeleteTask;
            }
        }

        private DelegateCommand _CleanTasks { get; set; }
        public ICommand CleanTasks
        {
            get
            {
                if (_CleanTasks == null)
                    _CleanTasks = new DelegateCommand(CleanMangaTasks);
                return _CleanTasks;
            }
        }
        #endregion
    }

    public class ListQueueItem : Manga.NotifyPropChangeBase
    {
        #region Private
        private Guid _ID { get; set; }
        private String _Title { get; set; }
        private Int32 _Progress { get; set; }
        #endregion

        #region Public
        public Guid ID
        {
            get { return _ID; }
            set { _ID = value; OnPropertyChanged("ID"); }
        }
        public String Title
        {
            get { return _Title; }
            set { _Title = value; OnPropertyChanged("Title"); }
        }
        public Int32 Progress
        {
            get { return _Progress; }
            set { _Progress = value; OnPropertyChanged("Progress"); }
        }

        public String ID_Text { get { return ID.ToString(); } }
        #endregion
    }
}

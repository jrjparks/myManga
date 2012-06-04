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

namespace myManga.ViewModels
{
    public sealed class QueueViewModel : ViewModelBase
    {
        public QueueViewModel()
        {
            Manager_v1.Instance.TaskAdded += UpdateTaskData;
            Manager_v1.Instance.TaskBeginning += UpdateTaskData;
            Manager_v1.Instance.TaskProgress += UpdateTaskData;
            Manager_v1.Instance.TaskComplete += UpdateTaskData;

            Manager_v1.Instance.TaskFaulted += RemoveMangaTask;
            Manager_v1.Instance.TaskRemoved += RemoveMangaTask;

            Manager_v1.Instance.NameUpdated += (s, t) =>
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
            Int32 _TaskIDIndex = IndexOfTaskID(Task.Guid);
            if (!_TaskIDIndex.Equals(-1))
                MangaTasks[_TaskIDIndex].Progress = Task.Progress;
            else
                AddMangaTask(new ListQueueItem() { ID = Task.Guid, Progress = Task.Progress, Title = Task.Data.Title });
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
            Int32 _Index = IndexOfTaskID(Guid);
            if (!Manager_v1.Instance.CancelTask(Guid))
                if (_Index >= 0)
                    RemoveMangaTask(_Index);
        }

        private void RemoveMangaTask(Object Sender, QueuedTask<ManagerData<String, MangaInfo>> Task)
        {
            Int32 Index = IndexOfTaskID(Task.Guid);
            if (Index >= 0)
            {
                RemoveMangaTask(Index);
                SendViewModelToastNotification(this, String.Format("Removed Task: {0}\n{1}", Task.Data.Title, Task.Guid));
            }
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

        private delegate void RemoveMangaTaskDelegate(Int32 value);
        private void RemoveMangaTask(Int32 value)
        {
            if (Application.Current.Dispatcher.Thread == Thread.CurrentThread)
                MangaTasks.RemoveAt(value);
            else
                Application.Current.Dispatcher.Invoke(new RemoveMangaTaskDelegate(RemoveMangaTask), value);
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

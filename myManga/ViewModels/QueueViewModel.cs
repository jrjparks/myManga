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

namespace myManga.ViewModels
{
    public sealed class QueueViewModel : ViewModelBase
    {
        public QueueViewModel()
        {
            _MangaTasks = new ObservableCollection<ListQueueItem>();

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
                    _MangaTasks[Index].Title = t.Data.Title;
                    _MangaTasks[Index].Progress = t.Progress;
                }
                else
                    _MangaTasks.Add(new ListQueueItem() { ID = t.Guid, Progress = t.Progress, Title = t.Data.Title });
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
                _MangaTasks[_TaskIDIndex].Progress = Task.Progress;
            else
                _MangaTasks.Add(new ListQueueItem() { ID = Task.Guid, Progress = Task.Progress, Title = Task.Data.Title });
        }
        private Int32 IndexOfTaskID(Guid TaskID)
        {
            if (_MangaTasks.Count > 0)
                foreach (ListQueueItem Item in _MangaTasks)
                {
                    if (Item.ID.Equals(TaskID))
                        return _MangaTasks.IndexOf(Item);
                }
            return -1;
        }

        private void OpenMangaTask(Guid Guid)
        {
            String Title = _MangaTasks[IndexOfTaskID(Guid)].Title;
            SendViewModelToastNotification(this, String.Format("Open: {0}", Title), TimeSpan.FromMilliseconds(500));
            OnOpenMZA(Title);
        }

        private void DeleteMangaTask(Guid Guid)
        {
            Int32 _Index = IndexOfTaskID(Guid);
            if (!Manager_v1.Instance.CancelTask(Guid))
                if (_Index >= 0)
                    _MangaTasks.RemoveAt(_Index);
        }

        private void RemoveMangaTask(Object Sender, QueuedTask<ManagerData<String, MangaInfo>> Task)
        {
            Int32 Index = IndexOfTaskID(Task.Guid);
            if (Index >= 0)
            {
                _MangaTasks.RemoveAt(Index);
                SendViewModelToastNotification(this, String.Format("Removed Task: {0}\n{1}", Task.Data.Title, Task.Guid));
            }
        }
        #endregion

        #region Public
        #endregion
        #endregion

        #region Lists
        public ObservableCollection<ListQueueItem> _MangaTasks { get; set; }
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
                if (_MangaTasks[_Index].Progress == 100)
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

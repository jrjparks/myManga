using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Interfaces;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using myManga_App.Objects;
using Core.MVVM;
using System.Windows.Input;
using Core.IO;
using myManga_App.Properties;

namespace myManga_App.ViewModels
{
    public class SettingsViewModel : IDisposable, INotifyPropertyChanging, INotifyPropertyChanged
    {
        #region NotifyPropertyChange
        public event PropertyChangingEventHandler PropertyChanging;
        protected void OnPropertyChanging([CallerMemberName] String caller = "")
        {
            if (PropertyChanging != null)
                PropertyChanging(this, new PropertyChangingEventArgs(caller));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] String caller = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }
        #endregion

        #region Settings TreeView
        protected ObservableCollection<Object> settingsTreeView;
        public ObservableCollection<Object> SettingsTreeView
        {
            get { return settingsTreeView ?? (settingsTreeView = new ObservableCollection<Object>()); }
            set
            {
                OnPropertyChanging();
                settingsTreeView = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region PluginList
        private ObservableCollection<SiteExtentionInformationObject> _SiteExtentionInformationObjects;
        public ObservableCollection<SiteExtentionInformationObject> SiteExtentionInformationObjects
        {
            get { return _SiteExtentionInformationObjects; }
            set { _SiteExtentionInformationObjects = value; OnPropertyChanged("SiteExtentionInformationObjects"); }
        }

        private ObservableCollection<DatabaseExtensionInformationObject> _DatabaseExtensionInformationObjects;
        public ObservableCollection<DatabaseExtensionInformationObject> DatabaseExtensionInformationObjects
        {
            get { return _DatabaseExtensionInformationObjects; }
            set { _DatabaseExtensionInformationObjects = value; OnPropertyChanged("DatabaseExtensionInformationObjects"); }
        }
        #endregion

        #region Buttons
        protected DelegateCommand saveCommand;
        public ICommand SaveCommand
        { get { return saveCommand ?? (saveCommand = new DelegateCommand(SaveUserConfig)); } }

        protected DelegateCommand closeCommand;
        public ICommand CloseCommand
        { get { return closeCommand ?? (closeCommand = new DelegateCommand(OnCloseEvent)); } }
        public event EventHandler CloseEvent;
        protected void OnCloseEvent()
        {
            if (CloseEvent != null)
                CloseEvent(this, null);
        }
        #endregion

        #region Extention List Movements
        protected DelegateCommand<String> moveUpCommand;
        public ICommand MoveUpCommand
        { get { return moveUpCommand ?? (moveUpCommand = new DelegateCommand<String>(MoveElementUpCommand, CanMoveUpCommand)); } }

        protected Boolean CanMoveUpCommand(String content)
        {
            String[] ButtonArgs = content.Split(':');
            Int32 index = 0;
            Object obj;
            switch (ButtonArgs[0])
            {
                case "Site":
                    obj = SiteExtentionInformationObjects.First(_seio => _seio.Name == ButtonArgs[1]);
                    index = SiteExtentionInformationObjects.IndexOf(obj as SiteExtentionInformationObject);
                    break;
                case "Database":
                    obj = DatabaseExtensionInformationObjects.First(_deio => _deio.Name == ButtonArgs[1]);
                    index = DatabaseExtensionInformationObjects.IndexOf(obj as DatabaseExtensionInformationObject);
                    break;
            }
            return index > 0;
        }

        protected void MoveElementUpCommand(String content)
        {
            String[] ButtonArgs = content.Split(':');
            Int32 index = 0;
            Object obj;
            switch (ButtonArgs[0])
            {
                case "Site":
                    obj = SiteExtentionInformationObjects.First(_seio => _seio.Name == ButtonArgs[1]);
                    index = SiteExtentionInformationObjects.IndexOf(obj as SiteExtentionInformationObject);
                    SiteExtentionInformationObject seio = SiteExtentionInformationObjects[index];
                    SiteExtentionInformationObjects.RemoveAt(index);
                    SiteExtentionInformationObjects.Insert(index - 1, seio);
                    OnPropertyChanged("SiteExtentionInformationObjects");
                    break;
                case "Database":
                    obj = DatabaseExtensionInformationObjects.First(_deio => _deio.Name == ButtonArgs[1]);
                    index = DatabaseExtensionInformationObjects.IndexOf(obj as DatabaseExtensionInformationObject);
                    DatabaseExtensionInformationObject deio = DatabaseExtensionInformationObjects[index];
                    DatabaseExtensionInformationObjects.RemoveAt(index);
                    DatabaseExtensionInformationObjects.Insert(index - 1, deio);
                    OnPropertyChanged("DatabaseExtensionInformationObjects");
                    break;
            }
        }

        protected DelegateCommand<String> moveDownCommand;
        public ICommand MoveDownCommand
        { get { return moveDownCommand ?? (moveDownCommand = new DelegateCommand<String>(MoveElementDownCommand, CanMoveDownCommand)); } }

        protected Boolean CanMoveDownCommand(String content)
        {
            String[] ButtonArgs = content.Split(':');
            Int32 index = 0;
            Object obj;
            switch (ButtonArgs[0])
            {
                case "Site":
                    obj = SiteExtentionInformationObjects.First(_seio => _seio.Name == ButtonArgs[1]);
                    index = SiteExtentionInformationObjects.IndexOf(obj as SiteExtentionInformationObject);
                    return index < SiteExtentionInformationObjects.Count - 1;
                case "Database":
                    obj = DatabaseExtensionInformationObjects.First(_deio => _deio.Name == ButtonArgs[1]);
                    index = DatabaseExtensionInformationObjects.IndexOf(obj as DatabaseExtensionInformationObject);
                    return index < DatabaseExtensionInformationObjects.Count - 1;
            }
            return false;
        }

        protected void MoveElementDownCommand(String content)
        {
            String[] ButtonArgs = content.Split(':');
            Int32 index = 0;
            Object obj;
            switch (ButtonArgs[0])
            {
                case "Site":
                    obj = SiteExtentionInformationObjects.First(_seio => _seio.Name == ButtonArgs[1]);
                    index = SiteExtentionInformationObjects.IndexOf(obj as SiteExtentionInformationObject);
                    SiteExtentionInformationObject seio = SiteExtentionInformationObjects[index];
                    SiteExtentionInformationObjects.RemoveAt(index);
                    SiteExtentionInformationObjects.Insert(index + 1, seio);
                    OnPropertyChanged("SiteExtentionInformationObjects");
                    break;
                case "Database":
                    obj = DatabaseExtensionInformationObjects.First(_deio => _deio.Name == ButtonArgs[1]);
                    index = DatabaseExtensionInformationObjects.IndexOf(obj as DatabaseExtensionInformationObject);
                    DatabaseExtensionInformationObject deio = DatabaseExtensionInformationObjects[index];
                    DatabaseExtensionInformationObjects.RemoveAt(index);
                    DatabaseExtensionInformationObjects.Insert(index + 1, deio);
                    OnPropertyChanged("DatabaseExtensionInformationObjects");
                    break;
            }
        }
        #endregion

        #region SaveType
        public IEnumerable<SaveType> SaveTypes
        { get { return Enum.GetValues(typeof(SaveType)).Cast<SaveType>(); } }

        private SaveType selectedSaveType;
        public SaveType SelectedSaveType
        {
            get { return selectedSaveType; }
            set
            {
                OnPropertyChanging();
                selectedSaveType = value;
                OnPropertyChanged();
            }
        }
        #endregion

        protected readonly App App = App.Current as App;

        public SettingsViewModel()
        {
            SiteExtentionInformationObjects = new ObservableCollection<SiteExtentionInformationObject>();
            foreach (ISiteExtension ise in App.SiteExtensions.DLLCollection)
            {
                ISiteExtensionDescriptionAttribute iseda = ise.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);
                SiteExtentionInformationObjects.Add(new SiteExtentionInformationObject(iseda) { Enabled = App.UserConfig.EnabledSiteExtentions.Contains(iseda.Name) });
            }
            DatabaseExtensionInformationObjects = new ObservableCollection<DatabaseExtensionInformationObject>();
            foreach (IDatabaseExtension ide in App.DatabaseExtensions.DLLCollection)
            {
                IDatabaseExtensionDescriptionAttribute ideda = ide.GetType().GetCustomAttribute<IDatabaseExtensionDescriptionAttribute>(false);
                DatabaseExtensionInformationObjects.Add(new DatabaseExtensionInformationObject(ideda) { Enabled = App.UserConfig.EnabledDatabaseExtentions.Contains(ideda.Name) });
            }
            SelectedSaveType = Settings.Default.SaveType;
        }

        public void SaveUserConfig()
        {
            App.UserConfig.EnabledSiteExtentions.Clear();
            App.UserConfig.EnabledSiteExtentions.AddRange((from SiteExtentionInformationObject seio in SiteExtentionInformationObjects where seio.Enabled select seio.Name));
            App.UserConfig.EnabledDatabaseExtentions.Clear();
            App.UserConfig.EnabledDatabaseExtentions.AddRange((from DatabaseExtensionInformationObject deio in DatabaseExtensionInformationObjects where deio.Enabled select deio.Name));
            Settings.Default.SaveType = SelectedSaveType;
            App.SaveUserConfig();
        }

        public void Dispose() { }
    }
}

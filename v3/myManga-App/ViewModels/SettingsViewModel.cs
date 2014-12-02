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
    public class SettingsViewModel : BaseViewModel
    {
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
        private ObservableCollection<SiteExtensionInformationObject> _SiteExtensionInformationObjects;
        public ObservableCollection<SiteExtensionInformationObject> SiteExtensionInformationObjects
        {
            get { return _SiteExtensionInformationObjects; }
            set { _SiteExtensionInformationObjects = value; OnPropertyChanged("SiteExtensionInformationObjects"); }
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
                    obj = SiteExtensionInformationObjects.First(_seio => _seio.Name == ButtonArgs[1]);
                    index = SiteExtensionInformationObjects.IndexOf(obj as SiteExtensionInformationObject);
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
                    obj = SiteExtensionInformationObjects.First(_seio => _seio.Name == ButtonArgs[1]);
                    index = SiteExtensionInformationObjects.IndexOf(obj as SiteExtensionInformationObject);
                    SiteExtensionInformationObject seio = SiteExtensionInformationObjects[index];
                    SiteExtensionInformationObjects.RemoveAt(index);
                    SiteExtensionInformationObjects.Insert(index - 1, seio);
                    OnPropertyChanged("SiteExtensionInformationObjects");
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
                    obj = SiteExtensionInformationObjects.First(_seio => _seio.Name == ButtonArgs[1]);
                    index = SiteExtensionInformationObjects.IndexOf(obj as SiteExtensionInformationObject);
                    return index < SiteExtensionInformationObjects.Count - 1;
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
                    obj = SiteExtensionInformationObjects.First(_seio => _seio.Name == ButtonArgs[1]);
                    index = SiteExtensionInformationObjects.IndexOf(obj as SiteExtensionInformationObject);
                    SiteExtensionInformationObject seio = SiteExtensionInformationObjects[index];
                    SiteExtensionInformationObjects.RemoveAt(index);
                    SiteExtensionInformationObjects.Insert(index + 1, seio);
                    OnPropertyChanged("SiteExtensionInformationObjects");
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

        public SettingsViewModel()
        {
            SiteExtensionInformationObjects = new ObservableCollection<SiteExtensionInformationObject>();
            foreach (String SiteExtensionName in App.UserConfig.EnabledSiteExtensions) 
            {
                if (App.SiteExtensions.DLLCollection.Contains(SiteExtensionName))
                {
                    ISiteExtension SiteExtension = App.SiteExtensions.DLLCollection[SiteExtensionName];
                    ISiteExtensionDescriptionAttribute SiteExtensionDescriptionAttribute = SiteExtension.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);
                    SiteExtensionInformationObjects.Add(new SiteExtensionInformationObject(SiteExtensionDescriptionAttribute) { Enabled = true });
                }
            }
            foreach (ISiteExtension ise in App.SiteExtensions.DLLCollection.Where(se => SiteExtensionInformationObjects.FirstOrDefault(sei => sei.Name == se.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false).Name) == null))
            {
                ISiteExtensionDescriptionAttribute iseda = ise.GetType().GetCustomAttribute<ISiteExtensionDescriptionAttribute>(false);
                SiteExtensionInformationObjects.Add(new SiteExtensionInformationObject(iseda) { Enabled = App.UserConfig.EnabledSiteExtensions.Contains(iseda.Name) });
            }

            DatabaseExtensionInformationObjects = new ObservableCollection<DatabaseExtensionInformationObject>();
            foreach (String DatabaseExtensionName in App.UserConfig.EnabledDatabaseExtentions)
            {
                if (App.DatabaseExtensions.DLLCollection.Contains(DatabaseExtensionName))
                {
                    IDatabaseExtension DatabaseExtension = App.DatabaseExtensions.DLLCollection[DatabaseExtensionName];
                    IDatabaseExtensionDescriptionAttribute DatabaseExtensionDescriptionAttribute = DatabaseExtension.GetType().GetCustomAttribute<IDatabaseExtensionDescriptionAttribute>(false);
                    DatabaseExtensionInformationObjects.Add(new DatabaseExtensionInformationObject(DatabaseExtensionDescriptionAttribute) { Enabled = true });
                }
            }

            foreach (IDatabaseExtension DatabaseExtension in App.DatabaseExtensions.DLLCollection.Where(de => DatabaseExtensionInformationObjects.FirstOrDefault(dei => dei.Name == de.GetType().GetCustomAttribute<IDatabaseExtensionDescriptionAttribute>(false).Name) == null))
            {
                IDatabaseExtensionDescriptionAttribute DatabaseExtensionDescriptionAttribute = DatabaseExtension.GetType().GetCustomAttribute<IDatabaseExtensionDescriptionAttribute>(false);
                DatabaseExtensionInformationObjects.Add(new DatabaseExtensionInformationObject(DatabaseExtensionDescriptionAttribute) { Enabled = App.UserConfig.EnabledDatabaseExtentions.Contains(DatabaseExtensionDescriptionAttribute.Name) });
            }
            SelectedSaveType = Settings.Default.SaveType;
        }

        public void SaveUserConfig()
        {
            App.UserConfig.EnabledSiteExtensions.Clear();
            App.UserConfig.EnabledSiteExtensions.AddRange((from SiteExtensionInformationObject seio in SiteExtensionInformationObjects where seio.Enabled select seio.Name));
            App.UserConfig.EnabledDatabaseExtentions.Clear();
            App.UserConfig.EnabledDatabaseExtentions.AddRange((from DatabaseExtensionInformationObject deio in DatabaseExtensionInformationObjects where deio.Enabled select deio.Name));
            Settings.Default.SaveType = SelectedSaveType;
            App.SaveUserConfig();
        }

        public void Dispose() { }
    }
}

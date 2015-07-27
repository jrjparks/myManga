using Core.IO;
using Core.IO.Storage.Manager.BaseInterfaceClasses;
using Core.MVVM;
using Core.Other.Singleton;
using myManga_App.Objects;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Input;

namespace myManga_App.ViewModels
{
    // TODO: Convert properties to DependencyProperty

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

        #region DefaultPageZoom
        private Double defaultPageZoom;
        public Double DefaultPageZoom
        {
            get { return defaultPageZoom; }
            set
            {
                OnPropertyChanging();
                defaultPageZoom = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region BackChapters
        private Boolean removeBackChapters;
        public Boolean RemoveBackChapters
        {
            get { return removeBackChapters; }
            set
            {
                OnPropertyChanging();
                removeBackChapters = value;
                OnPropertyChanged();
            }
        }

        private Int32 backChaptersToKeep;
        public Int32 BackChaptersToKeep
        {
            get { return backChaptersToKeep; }
            set
            {
                OnPropertyChanging();
                backChaptersToKeep = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region ThemeType
        private ThemeType theme = ThemeType.Light;
        public ThemeType Theme
        {
            get { return theme; }
            set
            {
                OnPropertyChanging();
                theme = value;
                OnPropertyChanged();
                App.ApplyTheme(value);
            }
        }
        #endregion

        public SettingsViewModel()
        {
            if (!IsInDesignMode)
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
                this.SelectedSaveType = App.UserConfig.SaveType;
                this.DefaultPageZoom = App.UserConfig.DefaultPageZoom;
                this.RemoveBackChapters = App.UserConfig.RemoveBackChapters;
                this.BackChaptersToKeep = App.UserConfig.BackChaptersToKeep;
                this.Theme = App.UserConfig.Theme;
            }
        }

        public void SaveUserConfig()
        {
            // SiteExtensionInformationObjects
            App.UserConfig.EnabledSiteExtensions.Clear();
            foreach (String SiteExtensionInformationName in this.SiteExtensionInformationObjects.Where(x => x.Enabled).Select(x => x.Name))
                App.UserConfig.EnabledSiteExtensions.Add(SiteExtensionInformationName);

            // DatabaseExtensionInformationObjects
            App.UserConfig.EnabledDatabaseExtentions.Clear();
            foreach (String DatabaseExtensionInformationName in this.DatabaseExtensionInformationObjects.Where(x => x.Enabled).Select(x => x.Name))
                App.UserConfig.EnabledDatabaseExtentions.Add(DatabaseExtensionInformationName);

            App.UserConfig.DefaultPageZoom = this.DefaultPageZoom;
            if (App.UserConfig.SaveType != SelectedSaveType) ConvertStoredFiles();
            App.UserConfig.SaveType = SelectedSaveType;
            App.UserConfig.RemoveBackChapters = this.RemoveBackChapters;
            App.UserConfig.BackChaptersToKeep = this.BackChaptersToKeep;
            App.UserConfig.Theme = this.Theme;
        }

        private void ConvertStoredFiles()
        {
            Stream archive_stream;
            foreach (String filepath in Directory.EnumerateFiles(App.MANGA_ARCHIVE_DIRECTORY, App.MANGA_ARCHIVE_FILTER))
            {
                if (App.ZipStorage.TryRead(filepath, typeof(MangaObject).Name, out archive_stream))
                { try { App.ZipStorage.Write(filepath, typeof(MangaObject).Name, archive_stream.Deserialize<MangaObject>(App.UserConfig.SaveType).Serialize(this.SelectedSaveType)); } catch { } archive_stream.Close(); }
                if (App.ZipStorage.TryRead(filepath, typeof(BookmarkObject).Name, out archive_stream))
                { try { App.ZipStorage.Write(filepath, typeof(BookmarkObject).Name, archive_stream.Deserialize<BookmarkObject>(App.UserConfig.SaveType).Serialize(this.SelectedSaveType)); } catch { } archive_stream.Close(); }
            }
            foreach (String filepath in Directory.EnumerateFiles(App.CHAPTER_ARCHIVE_DIRECTORY, App.CHAPTER_ARCHIVE_FILTER, SearchOption.AllDirectories))
            {
                if (App.ZipStorage.TryRead(filepath, typeof(ChapterObject).Name, out archive_stream))
                { try { App.ZipStorage.Write(filepath, typeof(ChapterObject).Name, archive_stream.Deserialize<ChapterObject>(App.UserConfig.SaveType).Serialize(this.SelectedSaveType)); } catch { } archive_stream.Close(); }
            }
        }
    }
}
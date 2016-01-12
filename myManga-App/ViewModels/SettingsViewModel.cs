using myManga_App.IO.Local.Object;
using myManga_App.Objects;
using myManga_App.Objects.UserConfig;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
        private static readonly DependencyProperty SelectedSiteExtensionInformationObjectProperty = DependencyProperty.RegisterAttached(
            "SelectedSiteExtensionInformationObject",
            typeof(SiteExtensionInformationObject),
            typeof(SettingsViewModel),
            new PropertyMetadata(null));

        public SiteExtensionInformationObject SelectedSiteExtensionInformationObject
        {
            get { return (SiteExtensionInformationObject)GetValue(SelectedSiteExtensionInformationObjectProperty); }
            private set { SetValue(SelectedSiteExtensionInformationObjectProperty, value); }
        }

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

        #region SerializeType
        public IEnumerable<SerializeType> SerializeTypes
        { get { return Enum.GetValues(typeof(SerializeType)).Cast<SerializeType>(); } }

        private SerializeType selectedSerializeType;
        public SerializeType SelectedSerializeType
        {
            get { return selectedSerializeType; }
            set
            {
                OnPropertyChanging();
                selectedSerializeType = value;
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

        #region Plugin Authenticate
        private CancellationTokenSource AuthenticationCTS { get; set; }
        private static readonly DependencyProperty AuthenticationUsernameProperty = DependencyProperty.RegisterAttached(
            "AuthenticationUsername",
            typeof(String),
            typeof(SettingsViewModel),
            new PropertyMetadata(String.Empty));

        public String AuthenticationUsername
        {
            get { return (String)GetValue(AuthenticationUsernameProperty); }
            private set { SetValue(AuthenticationUsernameProperty, value); }
        }

        private static readonly DependencyProperty AuthenticationPasswordProperty = DependencyProperty.RegisterAttached(
            "AuthenticationPassword",
            typeof(String),
            typeof(SettingsViewModel),
            new PropertyMetadata(String.Empty));

        public String AuthenticationPassword
        {
            get { return (String)GetValue(AuthenticationPasswordProperty); }
            private set { SetValue(AuthenticationPasswordProperty, value); }
        }

        private static readonly DependencyProperty AuthenticationRememberMeProperty = DependencyProperty.RegisterAttached(
            "AuthenticationRememberMe",
            typeof(Boolean),
            typeof(SettingsViewModel),
            new PropertyMetadata(false));

        public Boolean AuthenticationRememberMe
        {
            get { return (Boolean)GetValue(AuthenticationRememberMeProperty); }
            private set { SetValue(AuthenticationRememberMeProperty, value); }
        }

        private static readonly DependencyProperty ShowAuthenticationProperty = DependencyProperty.RegisterAttached(
            "ShowAuthentication",
            typeof(Boolean),
            typeof(SettingsViewModel),
            new PropertyMetadata(false));

        public Boolean ShowAuthentication
        {
            get { return (Boolean)GetValue(ShowAuthenticationProperty); }
            private set { SetValue(ShowAuthenticationProperty, value); }
        }

        protected DelegateCommand<System.Windows.Controls.PasswordBox> authenticateCommand;
        public ICommand AuthenticateCommand
        { get { return authenticateCommand ?? (authenticateCommand = new DelegateCommand<System.Windows.Controls.PasswordBox>(Authenticate)); } }
        private async void Authenticate(System.Windows.Controls.PasswordBox PasswordBox)
        {
            try { if (!Equals(AuthenticationCTS, null)) { AuthenticationCTS.Cancel(); } }
            catch { }
            using (AuthenticationCTS = new CancellationTokenSource())
            {
                try
                {
                    ISiteExtension siteExtention = App.SiteExtensions.DLLCollection[this.SelectedSiteExtensionInformationObject.Name];
                    String Username = this.AuthenticationUsername;
                    Boolean RememberMe = this.AuthenticationRememberMe;
                    Task<Boolean> authenticationTask = Task.Run<Boolean>(() => siteExtention.Authenticate(new System.Net.NetworkCredential(Username, PasswordBox.SecurePassword), AuthenticationCTS.Token, null));
                    Boolean authenticationSuccess = await authenticationTask;
                    if (authenticationSuccess)
                    {
                        this.AuthenticationUsername = String.Empty;
                        this.AuthenticationRememberMe = false;
                        if (RememberMe)
                        {
                            UserPluginAuthenticationObject upa = App.UserAuthentication.UserPluginAuthentications.FirstOrDefault(_upa => _upa.PluginName.Equals(this.SelectedSiteExtensionInformationObject.Name));
                            App.UserAuthentication.UserPluginAuthentications.Remove(upa);
                            upa = upa ?? new UserPluginAuthenticationObject();
                            upa.PluginName = this.SelectedSiteExtensionInformationObject.Name;
                            upa.Username = Username;
                            upa.Password = PasswordBox.SecurePassword;
                            App.UserAuthentication.UserPluginAuthentications.Add(upa);
                            App.SaveUserAuthentication();
                        }
                    }
                    PasswordBox.SecurePassword.Clear();
                    PasswordBox.Clear();
                    this.ShowAuthentication = !authenticationSuccess;
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { throw ex; }
                finally { }
            }
        }

        protected DelegateCommand cancelAuthenticationCommand;
        public ICommand CancelAuthenticationCommand
        { get { return cancelAuthenticationCommand ?? (cancelAuthenticationCommand = new DelegateCommand(CancelAuthentication)); } }
        private void CancelAuthentication() { this.ShowAuthentication = false; }
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
                this.SelectedSerializeType = App.UserConfig.SerializeType;
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
            if (App.UserConfig.SerializeType != SelectedSerializeType) ConvertStoredFiles();
            App.UserConfig.SerializeType = SelectedSerializeType;
            App.UserConfig.RemoveBackChapters = this.RemoveBackChapters;
            App.UserConfig.BackChaptersToKeep = this.BackChaptersToKeep;
            App.UserConfig.Theme = this.Theme;
        }

        private void ConvertStoredFiles()
        {
            foreach (String filepath in Directory.EnumerateFiles(App.MANGA_ARCHIVE_DIRECTORY, App.MANGA_ARCHIVE_FILTER))
            {
                using (Stream MangaObjectStream = App.ZipManager.Read(filepath, typeof(MangaObject).Name))
                {
                    MangaObject MangaObject = MangaObjectStream.Deserialize<MangaObject>(App.UserConfig.SerializeType);
                    App.ZipManager.Write(filepath, typeof(MangaObject).Name, MangaObject.Serialize(SelectedSerializeType));
                }

                using (Stream BookmarkObjectStream = App.ZipManager.Read(filepath, typeof(BookmarkObject).Name))
                {
                    BookmarkObject BookmarkObject = BookmarkObjectStream.Deserialize<BookmarkObject>(App.UserConfig.SerializeType);
                    App.ZipManager.Write(filepath, typeof(BookmarkObject).Name, BookmarkObject.Serialize(SelectedSerializeType));
                }
            }
            foreach (String filepath in Directory.EnumerateFiles(App.CHAPTER_ARCHIVE_DIRECTORY, App.CHAPTER_ARCHIVE_FILTER, SearchOption.AllDirectories))
            {
                using (Stream ChapterObjectStream = App.ZipManager.Read(filepath, typeof(ChapterObject).Name))
                {
                    ChapterObject ChapterObject = ChapterObjectStream.Deserialize<ChapterObject>(App.UserConfig.SerializeType);
                    App.ZipManager.Write(filepath, typeof(ChapterObject).Name, ChapterObject.Serialize(SelectedSerializeType));
                }
            }
        }

        protected override void SubDispose()
        {
            this.DatabaseExtensionInformationObjects = null;
            this.SiteExtensionInformationObjects = null;
        }
    }
}
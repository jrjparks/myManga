using myManga_App.IO.Local.Object;
using myManga_App.Objects.Extensions;
using myManga_App.Objects.UserConfig;
using myManga_App.ViewModels.Dialog;
using myMangaSiteExtension.Enums;
using myMangaSiteExtension.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Communication;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace myManga_App.ViewModels.Pages
{
    public sealed class SettingsViewModel : BaseViewModel
    {
        public SettingsViewModel()
            : base(false)
        {
            SiteExtensionObjects = new ObservableCollection<ExtensionObject>();
            DatabaseExtensionObjects = new ObservableCollection<ExtensionObject>();
            AuthenticationDialog = new AuthenticationDialogViewModel();
            if (!IsInDesignMode)
            { ResetData(); }
        }

        protected override void SubPullFocus()
        {
            base.SubPullFocus();
            ResetData();
        }

        protected override void SubLostFocus()
        {
            base.SubLostFocus();
            ResetData();
            App.ApplyTheme(App.UserConfiguration.Theme);
        }

        protected override void SubReturnFocus()
        {
            base.SubReturnFocus();
            ResetData();
            App.ApplyTheme(App.UserConfiguration.Theme);
        }

        private void UserConfiguration_PropertyChanged(Object sender, PropertyChangedEventArgs e)
        {
            UserConfigurationObject UserConfiguration = sender as UserConfigurationObject;
            switch (e.PropertyName)
            {
                case "Theme":
                    App.ApplyTheme(UserConfiguration.Theme);
                    break;
            }
        }

        private void ResetData()
        {
            if (!Equals(UserConfiguration, null))   // Cleanup any events from old UserConfigurationObject
            { UserConfiguration.PropertyChanged -= UserConfiguration_PropertyChanged; }

            UserConfiguration = new UserConfigurationObject();
            PropertyInfo[] UserConfigurationProperties = typeof(UserConfigurationObject).GetProperties();
            foreach (PropertyInfo Property in UserConfigurationProperties)
            { Property.SetValue(UserConfiguration, Property.GetValue(App.UserConfiguration)); }
            UserConfiguration.PropertyChanged += UserConfiguration_PropertyChanged;

            SiteExtensionObjects.Clear();
            DatabaseExtensionObjects.Clear();

            // Load SiteExtensions

            IEnumerable<EnabledExtensionObject> EnabledSiteExtensionObjects = UserConfiguration.EnabledExtensions.Where(ext => Equals(ext.ExtensionType, typeof(ISiteExtension).Name));
            IEnumerable<EnabledExtensionObject> EnabledDatabaseExtensionObjects = UserConfiguration.EnabledExtensions.Where(ext => Equals(ext.ExtensionType, typeof(IDatabaseExtension).Name));

            foreach (EnabledExtensionObject EnabledSiteExtensionObject in EnabledSiteExtensionObjects)
            {
                ISiteExtension SiteExtension = App.SiteExtensions[EnabledSiteExtensionObject.Name, EnabledSiteExtensionObject.Language];
                SiteExtensionObjects.Add(new ExtensionObject(SiteExtension, EnabledSiteExtensionObject.Enabled));
            }
            foreach (ISiteExtension SiteExtension in App.SiteExtensions)
            {
                String Name = String.Format("{0} ({1})", SiteExtension.ExtensionDescriptionAttribute.Name, SiteExtension.ExtensionDescriptionAttribute.Language);
                ExtensionObject SiteExtensionObject = SiteExtensionObjects.FirstOrDefault(seo => Equals(seo.Name, Name));
                if (Equals(SiteExtensionObject, null)) SiteExtensionObjects.Add(new ExtensionObject(SiteExtension, false));
            }

            foreach (EnabledExtensionObject EnabledDatabaseExtensionObject in EnabledDatabaseExtensionObjects)
            {
                IDatabaseExtension DatabaseExtension = App.DatabaseExtensions[EnabledDatabaseExtensionObject.Name, EnabledDatabaseExtensionObject.Language];
                DatabaseExtensionObjects.Add(new ExtensionObject(DatabaseExtension, EnabledDatabaseExtensionObject.Enabled));
            }
            foreach (IDatabaseExtension DatabaseExtension in App.DatabaseExtensions)
            {
                String Name = String.Format("{0} ({1})", DatabaseExtension.ExtensionDescriptionAttribute.Name, DatabaseExtension.ExtensionDescriptionAttribute.Language);
                ExtensionObject DatabaseExtensionObject = DatabaseExtensionObjects.FirstOrDefault(seo => Equals(seo.Name, Name));
                if (Equals(DatabaseExtensionObject, null)) DatabaseExtensionObjects.Add(new ExtensionObject(DatabaseExtension, false));
            }
            MangaCount = App.MangaCacheObjects.Count(mco =>
                Equals(mco.MangaObject.MangaType, MangaObjectType.Manga));
            ManhwaCount = App.MangaCacheObjects.Count(mco =>
                Equals(mco.MangaObject.MangaType, MangaObjectType.Manhua));
            ManhwaCount += App.MangaCacheObjects.Count(mco =>
                Equals(mco.MangaObject.MangaType, MangaObjectType.Manhwa));
            UnknownCount = App.MangaCacheObjects.Count(mco =>
                Equals(mco.MangaObject.MangaType, MangaObjectType.Unknown));
            TotalCount = MangaCount + ManhwaCount + UnknownCount;
        }

        #region Extension Collections

        #region SiteExtension Collections
        public ObservableCollection<ExtensionObject> SiteExtensionObjects
        { get; private set; }
        #endregion

        #region DatabaseExtension Collections
        public ObservableCollection<ExtensionObject> DatabaseExtensionObjects
        { get; private set; }
        #endregion

        #endregion

        #region Status Page

        #region MangaCount
        private static readonly DependencyPropertyKey MangaCountPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "MangaCount",
            typeof(Int32),
            typeof(SettingsViewModel),
            null);
        private static readonly DependencyProperty MangaCountProperty = MangaCountPropertyKey.DependencyProperty;

        public Int32 MangaCount
        {
            get { return (Int32)GetValue(MangaCountProperty); }
            private set { SetValue(MangaCountPropertyKey, value); }
        }
        #endregion

        #region ManhwaCount
        private static readonly DependencyPropertyKey ManhwaCountPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "ManhwaCount",
            typeof(Int32),
            typeof(SettingsViewModel),
            null);
        private static readonly DependencyProperty ManhwaCountProperty = ManhwaCountPropertyKey.DependencyProperty;

        public Int32 ManhwaCount
        {
            get { return (Int32)GetValue(ManhwaCountProperty); }
            private set { SetValue(ManhwaCountPropertyKey, value); }
        }
        #endregion

        #region UnknownCount
        private static readonly DependencyPropertyKey UnknownCountPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "UnknownCount",
            typeof(Int32),
            typeof(SettingsViewModel),
            null);
        private static readonly DependencyProperty UnknownCountProperty = UnknownCountPropertyKey.DependencyProperty;

        public Int32 UnknownCount
        {
            get { return (Int32)GetValue(UnknownCountProperty); }
            private set { SetValue(UnknownCountPropertyKey, value); }
        }
        #endregion

        #region TotalCount
        private static readonly DependencyPropertyKey TotalCountPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "TotalCount",
            typeof(Int32),
            typeof(SettingsViewModel),
            null);
        private static readonly DependencyProperty TotalCountProperty = TotalCountPropertyKey.DependencyProperty;

        public Int32 TotalCount
        {
            get { return (Int32)GetValue(TotalCountProperty); }
            private set { SetValue(TotalCountPropertyKey, value); }
        }
        #endregion

        #endregion

        #region User Configuration
        private static readonly DependencyPropertyKey UserConfigurationPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "UserConfiguration",
            typeof(UserConfigurationObject),
            typeof(SettingsViewModel),
            null);
        private static readonly DependencyProperty UserConfigurationProperty = UserConfigurationPropertyKey.DependencyProperty;

        public UserConfigurationObject UserConfiguration
        {
            get { return (UserConfigurationObject)GetValue(UserConfigurationProperty); }
            private set { SetValue(UserConfigurationPropertyKey, value); }
        }

        public IEnumerable<SerializeType> SerializeTypes
        { get { return Enum.GetValues(typeof(SerializeType)).Cast<SerializeType>(); } }
        #endregion

        #region Save User Configuration Command
        private DelegateCommand saveUserConfigurationCommand;
        public ICommand SaveUserConfigurationCommand
        { get { return saveUserConfigurationCommand ?? (saveUserConfigurationCommand = new DelegateCommand(SaveUserConfiguration)); } }

        private void SaveUserConfiguration()
        {
            String[] IgnoreProperties = new String[] {
                "WindowSizeHeight",
                "WindowSizeWidth",
                "WindowState",
                "ViewTypes"
            };
            UserConfiguration.EnabledExtensions.Clear();
            foreach (ExtensionObject ExtensionObject in SiteExtensionObjects)
            {
                UserConfiguration.EnabledExtensions.Add(new EnabledExtensionObject(ExtensionObject.Extension)
                {
                    Enabled = ExtensionObject.Enabled
                });
            }
            foreach (ExtensionObject ExtensionObject in DatabaseExtensionObjects)
            {
                UserConfiguration.EnabledExtensions.Add(new EnabledExtensionObject(ExtensionObject.Extension)
                {
                    Enabled = ExtensionObject.Enabled
                });
            }
            /*
            UserConfiguration.EnabledSiteExtensions.Clear();
            foreach (ExtensionObject ExtensionObject in SiteExtensionObjects)
            { if (ExtensionObject.Enabled) UserConfiguration.EnabledSiteExtensions.Add(ExtensionObject.Name); }

            UserConfiguration.EnabledDatabaseExtensions.Clear();
            foreach (ExtensionObject ExtensionObject in DatabaseExtensionObjects)
            { if (ExtensionObject.Enabled) UserConfiguration.EnabledDatabaseExtensions.Add(ExtensionObject.Name); }
            //*/
            PropertyInfo[] UserConfigurationProperties = typeof(UserConfigurationObject).GetProperties();
            foreach (PropertyInfo Property in UserConfigurationProperties)
            { if (!IgnoreProperties.Contains(Property.Name)) Property.SetValue(App.UserConfiguration, Property.GetValue(UserConfiguration)); }

            Messenger.Instance.Send(true, "PreviousFocusRequest");
        }
        #endregion

        #region Cancel Configuration Command
        private DelegateCommand cancelConfigurationCommand;
        public ICommand CancelConfigurationCommand
        { get { return cancelConfigurationCommand ?? (cancelConfigurationCommand = new DelegateCommand(CancelConfiguration)); } }

        private void CancelConfiguration()
        {
            ReturnFocus();
        }
        #endregion

        #region Authentication Dialog
        public static readonly DependencyPropertyKey AuthenticationDialogPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "AuthenticationDialog",
            typeof(AuthenticationDialogViewModel),
            typeof(SettingsViewModel),
            null);
        public static readonly DependencyProperty AuthenticationDialogProperty = AuthenticationDialogPropertyKey.DependencyProperty;

        public AuthenticationDialogViewModel AuthenticationDialog
        {
            get { return (AuthenticationDialogViewModel)GetValue(AuthenticationDialogProperty); }
            private set { SetValue(AuthenticationDialogPropertyKey, value); }
        }
        #endregion

        #region AuthenticateExtensionCommand
        private DelegateCommand<ExtensionObject> authenticateExtensionCommand;
        public ICommand AuthenticateExtensionCommand
        { get { return authenticateExtensionCommand ?? (authenticateExtensionCommand = new DelegateCommand<ExtensionObject>(AuthenticateExtension, CanAuthenticateExtension)); } }

        private Boolean CanAuthenticateExtension(ExtensionObject Extension)
        {
            if (Equals(Extension, null)) return false;
            if (Equals(Extension.Extension, null)) return false;
            return true;
        }

        private async void AuthenticateExtension(ExtensionObject Extension)
        {
            try
            {
                Boolean Authenticated = await AuthenticationDialog.ShowDialogAsync(Extension.Extension);
                Extension.Update(Extension.Extension, LoadIcon: false);
            }
            catch { }
            finally { }
        }
        #endregion
    }
}

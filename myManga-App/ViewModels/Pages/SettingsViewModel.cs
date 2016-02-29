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
1
            // Load SiteExtensions
            foreach (String EnabledSiteExtension in UserConfiguration.EnabledSiteExtensions)
            {
                ISiteExtension SiteExtension = App.SiteExtensions[EnabledSiteExtension];
                SiteExtensionObjects.Add(new ExtensionObject(SiteExtension, true));
            }
            foreach (ISiteExtension SiteExtension in App.SiteExtensions)
            {
                if (!UserConfiguration.EnabledSiteExtensions.Contains(SiteExtension.ExtensionDescriptionAttribute.Name))
                    SiteExtensionObjects.Add(new ExtensionObject(SiteExtension, false));
            }

            // Load DatabaseExtensions
            foreach (String EnabledDatabaseExtension in UserConfiguration.EnabledDatabaseExtensions)
            {
                IDatabaseExtension DatabaseExtension = App.DatabaseExtensions[EnabledDatabaseExtension];
                DatabaseExtensionObjects.Add(new ExtensionObject(DatabaseExtension, true));
            }
            foreach (IDatabaseExtension DatabaseExtension in App.DatabaseExtensions)
            {
                if (!UserConfiguration.EnabledDatabaseExtensions.Contains(DatabaseExtension.ExtensionDescriptionAttribute.Name))
                    DatabaseExtensionObjects.Add(new ExtensionObject(DatabaseExtension, false));
            }
            MangaCount = App.MangaCacheObjects.Count(mco =>
                Equals(mco.MangaObject.MangaType, MangaObjectType.Manga));
            ManhwaCount = App.MangaCacheObjects.Count(mco =>
                Equals(mco.MangaObject.MangaType, MangaObjectType.Manhua));
            ManhwaCount += App.MangaCacheObjects.Count(mco =>
                Equals(mco.MangaObject.MangaType, MangaObjectType.Manhwa));
            UnknownCount = App.MangaCacheObjects.Count(mco =>
                Equals(mco.MangaObject.MangaType, MangaObjectType.Unknown));
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

            UserConfiguration.EnabledSiteExtensions.Clear();
            foreach (ExtensionObject ExtensionObject in SiteExtensionObjects)
            { if (ExtensionObject.Enabled) UserConfiguration.EnabledSiteExtensions.Add(ExtensionObject.Name); }

            UserConfiguration.EnabledDatabaseExtensions.Clear();
            foreach (ExtensionObject ExtensionObject in DatabaseExtensionObjects)
            { if (ExtensionObject.Enabled) UserConfiguration.EnabledDatabaseExtensions.Add(ExtensionObject.Name); }

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
        private DelegateCommand<IExtension> authenticateExtensionCommand;
        public ICommand AuthenticateExtensionCommand
        { get { return authenticateExtensionCommand ?? (authenticateExtensionCommand = new DelegateCommand<IExtension>(AuthenticateExtension, CanAuthenticateExtension)); } }

        private Boolean CanAuthenticateExtension(IExtension Extension)
        {
            if (Equals(Extension, null)) return false;
            return true;
        }

        private async void AuthenticateExtension(IExtension Extension)
        {
            try
            {
                Boolean Authenticated = await AuthenticationDialog.ShowDialogAsync(Extension);
                if (Authenticated) { }
            }
            catch { }
            finally { }
        }
        #endregion
    }
}

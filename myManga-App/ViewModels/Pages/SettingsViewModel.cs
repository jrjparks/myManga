using myManga_App.IO.Local.Object;
using myManga_App.Objects.Extensions;
using myManga_App.Objects.UserConfig;
using myManga_App.ViewModels.Dialog;
using myMangaSiteExtension.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Communication;
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
            SiteExtensionObjects = new ObservableCollection<SiteExtensionObject>();
            DatabaseExtensionObjects = new ObservableCollection<DatabaseExtensionObject>();
            AuthenticationDialog = new AuthenticationDialogViewModel();
            if (!IsInDesignMode)
            {
                ResetData();
            }
        }

        private void ResetData()
        {
            UserConfiguration = new UserConfigurationObject();
            PropertyInfo[] UserConfigurationProperties = typeof(UserConfigurationObject).GetProperties();
            foreach (PropertyInfo Property in UserConfigurationProperties)
            { Property.SetValue(UserConfiguration, Property.GetValue(App.UserConfiguration)); }
            UserConfiguration.PropertyChanged += (s, e) =>
            {
                UserConfigurationObject UserConfiguration = s as UserConfigurationObject;
                switch (e.PropertyName)
                {
                    case "Theme":
                        App.ApplyTheme(UserConfiguration.Theme);
                        break;
                }
            };

            SiteExtensionObjects.Clear();
            DatabaseExtensionObjects.Clear();

            // Load SiteExtensions
            foreach (String EnabledSiteExtension in UserConfiguration.EnabledSiteExtensions)
            {
                ISiteExtension SiteExtension = App.SiteExtensions.DLLCollection[EnabledSiteExtension];
                SiteExtensionObjects.Add(new SiteExtensionObject(SiteExtension, true));
            }
            foreach (ISiteExtension SiteExtension in App.SiteExtensions.DLLCollection)
            {
                if (!UserConfiguration.EnabledSiteExtensions.Contains(SiteExtension.SiteExtensionDescriptionAttribute.Name))
                    SiteExtensionObjects.Add(new SiteExtensionObject(SiteExtension, false));
            }

            // Load DatabaseExtensions
            foreach (String EnabledDatabaseExtension in UserConfiguration.EnabledDatabaseExtensions)
            {
                IDatabaseExtension DatabaseExtension = App.DatabaseExtensions.DLLCollection[EnabledDatabaseExtension];
                DatabaseExtensionObjects.Add(new DatabaseExtensionObject(DatabaseExtension, true));
            }
            foreach (IDatabaseExtension DatabaseExtension in App.DatabaseExtensions.DLLCollection)
            {
                if (!UserConfiguration.EnabledDatabaseExtensions.Contains(DatabaseExtension.DatabaseExtensionDescriptionAttribute.Name))
                    DatabaseExtensionObjects.Add(new DatabaseExtensionObject(DatabaseExtension, false));
            }
        }

        #region Extension Collections

        #region SiteExtension Collections
        public ObservableCollection<SiteExtensionObject> SiteExtensionObjects
        { get; private set; }
        #endregion

        #region DatabaseExtension Collections
        public ObservableCollection<DatabaseExtensionObject> DatabaseExtensionObjects
        { get; private set; }
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
            foreach (SiteExtensionObject SiteExtensionObject in SiteExtensionObjects)
            { if (SiteExtensionObject.Enabled) UserConfiguration.EnabledSiteExtensions.Add(SiteExtensionObject.Name); }

            UserConfiguration.EnabledDatabaseExtensions.Clear();
            foreach (DatabaseExtensionObject DatabaseExtensionObject in DatabaseExtensionObjects)
            { if (DatabaseExtensionObject.Enabled) UserConfiguration.EnabledDatabaseExtensions.Add(DatabaseExtensionObject.Name); }

            PropertyInfo[] UserConfigurationProperties = typeof(UserConfigurationObject).GetProperties();
            foreach (PropertyInfo Property in UserConfigurationProperties)
            { if(!IgnoreProperties.Contains(Property.Name)) Property.SetValue(App.UserConfiguration, Property.GetValue(UserConfiguration)); }

            Messenger.Instance.Send(true, "PreviousFocusRequest");
        }
        #endregion

        #region Cancel Configuration Command
        private DelegateCommand cancelConfigurationCommand;
        public ICommand CancelConfigurationCommand
        { get { return cancelConfigurationCommand ?? (cancelConfigurationCommand = new DelegateCommand(CancelConfiguration)); } }

        private void CancelConfiguration()
        {
            ResetData();
            App.ApplyTheme(App.UserConfiguration.Theme);
            Messenger.Instance.Send(true, "PreviousFocusRequest");
        }
        #endregion

        #region Authentication Dialog
        private static readonly DependencyPropertyKey AuthenticationDialogPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "AuthenticationDialog",
            typeof(AuthenticationDialogViewModel),
            typeof(SettingsViewModel),
            null);
        private static readonly DependencyProperty AuthenticationDialogProperty = AuthenticationDialogPropertyKey.DependencyProperty;

        public AuthenticationDialogViewModel AuthenticationDialog
        {
            get { return (AuthenticationDialogViewModel)GetValue(AuthenticationDialogProperty); }
            private set { SetValue(AuthenticationDialogPropertyKey, value); }
        }
        #endregion

        #region AuthenticateExtensionAsyncCommand
        private DelegateCommand<IExtension> authenticateExtensionAsyncCommand;
        public ICommand AuthenticateExtensionAsyncCommand
        { get { return authenticateExtensionAsyncCommand ?? (authenticateExtensionAsyncCommand = new DelegateCommand<IExtension>(AuthenticateExtensionAsync, CanAuthenticateExtensionAsync)); } }

        private Boolean CanAuthenticateExtensionAsync(IExtension Extension)
        {
            if (Equals(Extension, null)) return false;
            return true;
        }

        private void AuthenticateExtensionAsync(IExtension Extension)
        {
            Messenger.Instance.Send(Extension, "ShowExtensionAuthenticationDialog");
        }
        #endregion
    }
}

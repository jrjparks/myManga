using myManga_App.Objects.UserConfig;
using myMangaSiteExtension.Interfaces;
using System;
using System.Communication;
using System.Linq;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace myManga_App.ViewModels.Dialog
{
    public class AuthenticationDialogViewModel : DialogViewModel
    {
        public AuthenticationDialogViewModel()
            : base()
        {
            AuthenticationProgressReporter = new Progress<Int32>(ProgressValue =>
            {
                AuthenticationProgressActive = (0 < ProgressValue && ProgressValue < 100);
                AuthenticationProgress = ProgressValue;
            });
            if (!IsInDesignMode)
            {
            }
        }

        public override Task<bool> ShowDialogAsync(object state)
        {
            Extension = state as IExtension;
            return base.ShowDialogAsync(state);
        }

        #region Progress
        private IProgress<Int32> AuthenticationProgressReporter
        { get; set; }

        private static readonly DependencyPropertyKey AuthenticationProgressPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "AuthenticationProgress",
            typeof(Int32),
            typeof(AuthenticationDialogViewModel),
            new PropertyMetadata(0));
        private static readonly DependencyProperty AuthenticationProgressProperty = AuthenticationProgressPropertyKey.DependencyProperty;

        public Int32 AuthenticationProgress
        {
            get { return (Int32)GetValue(AuthenticationProgressProperty); }
            private set { SetValue(AuthenticationProgressPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey AuthenticationProgressActivePropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "AuthenticationProgressActive",
            typeof(Boolean),
            typeof(AuthenticationDialogViewModel),
            new PropertyMetadata(false));
        private static readonly DependencyProperty AuthenticationProgressActiveProperty = AuthenticationProgressActivePropertyKey.DependencyProperty;

        public Boolean AuthenticationProgressActive
        {
            get { return (Boolean)GetValue(AuthenticationProgressActiveProperty); }
            private set { SetValue(AuthenticationProgressActivePropertyKey, value); }
        }
        #endregion

        #region Extension
        private static readonly DependencyProperty ExtensionProperty = DependencyProperty.RegisterAttached(
            "Extension",
            typeof(IExtension),
            typeof(AuthenticationDialogViewModel),
            new PropertyMetadata(OnExtensionChanged));

        public IExtension Extension
        {
            get { return (IExtension)GetValue(ExtensionProperty); }
            private set { SetValue(ExtensionProperty, value); }
        }

        private static void OnExtensionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AuthenticationDialogViewModel control = d as AuthenticationDialogViewModel;
            IExtension Extension = e.NewValue as IExtension;

            control.AuthenticationUsername = String.Empty;
            if (!Equals(control.AuthenticationPassword, null))
            { try { control.AuthenticationPassword.Clear(); } catch { } }
            control.ResetAuthenticationRememberMe();

            if (!Equals(Extension, null))
            { control.Name = String.Format("{0} ({1})", Extension.ExtensionDescriptionAttribute.Name, Extension.ExtensionDescriptionAttribute.Language); }
        }

        private static readonly DependencyProperty NameProperty = DependencyProperty.RegisterAttached(
            "Name",
            typeof(String),
            typeof(AuthenticationDialogViewModel),
            null);

        public String Name
        {
            get { return (String)GetValue(NameProperty); }
            private set { SetValue(NameProperty, value); }
        }
        #endregion

        #region Authentication Properties
        private static readonly DependencyProperty AuthenticationUsernameProperty = DependencyProperty.RegisterAttached(
            "AuthenticationUsername",
            typeof(String),
            typeof(AuthenticationDialogViewModel),
            new PropertyMetadata(String.Empty));

        public String AuthenticationUsername
        {
            get { return (String)GetValue(AuthenticationUsernameProperty); }
            set { SetValue(AuthenticationUsernameProperty, value); }
        }

        private static readonly DependencyProperty AuthenticationPasswordProperty = DependencyProperty.RegisterAttached(
            "AuthenticationPassword",
            typeof(PasswordBox),
            typeof(AuthenticationDialogViewModel),
            null);

        public PasswordBox AuthenticationPassword
        {
            get { return (PasswordBox)GetValue(AuthenticationPasswordProperty); }
            set { SetValue(AuthenticationPasswordProperty, value); }
        }

        private static readonly DependencyProperty AuthenticationRememberMeProperty = DependencyProperty.RegisterAttached(
            "AuthenticationRememberMe",
            typeof(Boolean),
            typeof(AuthenticationDialogViewModel),
            new PropertyMetadata(true));

        public Boolean AuthenticationRememberMe
        {
            get { return (Boolean)GetValue(AuthenticationRememberMeProperty); }
            set { SetValue(AuthenticationRememberMeProperty, value); }
        }
        public void ResetAuthenticationRememberMe()
        {
            ClearValue(AuthenticationRememberMeProperty);
        }

        private static readonly DependencyPropertyKey AuthenticationErrorPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "AuthenticationError",
            typeof(String),
            typeof(AuthenticationDialogViewModel),
            null);
        private static readonly DependencyProperty AuthenticationErrorProperty = AuthenticationErrorPropertyKey.DependencyProperty;

        public String AuthenticationError
        {
            get { return (String)GetValue(AuthenticationErrorProperty); }
            private set { SetValue(AuthenticationErrorPropertyKey, value); }
        }
        #endregion

        #region Authenticate Async
        private CancellationTokenSource AuthenticationCTS { get; set; }

        protected override bool CanOK()
        {
            if (Equals(Extension, null)) return false;

            if (Equals(AuthenticationUsername, null)) return false;
            if (Equals(AuthenticationUsername, String.Empty)) return false;
            if (AuthenticationUsername.Length < 1) return false;

            if (Equals(AuthenticationPassword, null)) return false;
            if (Equals(AuthenticationPassword.SecurePassword, null)) return false;
            if (AuthenticationPassword.SecurePassword.Length < 1) return false;

            return base.CanOK();
        }

        protected override async void OK()
        {
            AuthenticationError = null;
            if (Equals(AuthenticationCTS, null))
            { AuthenticationCTS = new CancellationTokenSource(); }
            else if (AuthenticationCTS.Token.CanBeCanceled)
            { AuthenticationCTS.Cancel(); }

            if (AuthenticationCTS.Token.IsCancellationRequested)
            {
                await Task.Run(() => AuthenticationCTS.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)));
                AuthenticationCTS = new CancellationTokenSource();
            }

            Func<NetworkCredential, CancellationToken, IProgress<Int32>, Boolean> Authenticate = Extension.Authenticate;
            String Username = AuthenticationUsername;
            SecureString Password = AuthenticationPassword.SecurePassword;
            Boolean RememberMe = AuthenticationRememberMe;

            Boolean authenticationSuccess = await Task.Run(() => Authenticate(new NetworkCredential(Username, Password), AuthenticationCTS.Token, AuthenticationProgressReporter));
            String Name = Extension.ExtensionDescriptionAttribute.Name,
                Language = Extension.ExtensionDescriptionAttribute.Language;
            if (authenticationSuccess)
            {
                if (!Equals(Name, null))
                {
                    UserPluginAuthenticationObject UserPluginAuthentication = App.UserAuthentication.UserPluginAuthentications.FirstOrDefault(_ => _.PluginName.Equals(Name));
                    App.UserAuthentication.UserPluginAuthentications.Remove(UserPluginAuthentication);
                    UserPluginAuthentication = UserPluginAuthentication ?? new UserPluginAuthenticationObject();
                    UserPluginAuthentication.PluginName = Name;
                    UserPluginAuthentication.PluginLanguage = Language;
                    UserPluginAuthentication.Username = Username;
                    UserPluginAuthentication.Password = Password;
                    App.UserAuthentication.UserPluginAuthentications.Add(UserPluginAuthentication);
                    App.SaveUserAuthentication();
                }
                Extension = null;
                base.OK();
            }
            else
            {
                AuthenticationError = "Unable to Authenticate User!";
                AuthenticationUsername = String.Empty;
                if (!Equals(AuthenticationPassword, null))
                { try { AuthenticationPassword.Clear(); } catch { } }
                ResetAuthenticationRememberMe();
            }
        }
        #endregion

        #region Cancel Authenticate
        protected override void Cancel()
        {
            if (!Equals(AuthenticationCTS, null))
                if (AuthenticationCTS.Token.CanBeCanceled)
                { AuthenticationCTS.Cancel(); }
            Extension = null;
            base.Cancel();
        }
        #endregion
    }
}

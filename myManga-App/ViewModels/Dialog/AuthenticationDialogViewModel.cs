using myMangaSiteExtension.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace myManga_App.ViewModels.Dialog
{
    public class AuthenticationDialogViewModel : DependencyObject
    {
        public AuthenticationDialogViewModel() : base()
        {

        }

        public async Task<Boolean> Show(IExtension Extension)
        {
            return false;
        }

        private static readonly DependencyProperty AuthenticationUsernameProperty = DependencyProperty.RegisterAttached(
            "AuthenticationUsername",
            typeof(String),
            typeof(AuthenticationDialogViewModel),
            new PropertyMetadata(String.Empty));

        public String AuthenticationUsername
        {
            get
            {
                return (String)GetValue(AuthenticationUsernameProperty);
            }
            private set
            {
                SetValue(AuthenticationUsernameProperty, value);
            }
        }

        private static readonly DependencyProperty AuthenticationPasswordProperty = DependencyProperty.RegisterAttached(
            "AuthenticationPassword",
            typeof(SecureString),
            typeof(AuthenticationDialogViewModel),
            null);

        public SecureString AuthenticationPassword
        {
            get { return (SecureString)GetValue(AuthenticationPasswordProperty); }
            private set { SetValue(AuthenticationPasswordProperty, value); }
        }

        private static readonly DependencyProperty AuthenticationRememberMeProperty = DependencyProperty.RegisterAttached(
            "AuthenticationRememberMe",
            typeof(Boolean),
            typeof(AuthenticationDialogViewModel),
            new PropertyMetadata(false));

        public Boolean AuthenticationRememberMe
        {
            get { return (Boolean)GetValue(AuthenticationRememberMeProperty); }
            private set { SetValue(AuthenticationRememberMeProperty, value); }
        }
    }
}

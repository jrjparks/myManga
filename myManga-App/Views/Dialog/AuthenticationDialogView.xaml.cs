using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace myManga_App.Views.Dialog
{
    /// <summary>
    /// Interaction logic for AuthenticationDialogView.xaml
    /// </summary>
    public partial class AuthenticationDialogView : UserControl
    {
        public AuthenticationDialogView()
        {
            AuthenticationPassword.PasswordChanged += AuthenticationPassword_PasswordChanged;
            InitializeComponent();
        }

        private void AuthenticationPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox control = sender as PasswordBox;
            try { ((dynamic)DataContext).AuthenticationPassword = control.SecurePassword; }
            catch { }
        }
    }
}

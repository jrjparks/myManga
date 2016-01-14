using System.Windows.Controls;

namespace myManga_App.Views.Dialog
{
    /// <summary>
    /// Interaction logic for AuthenticationDialogView.xaml
    /// </summary>
    public partial class AuthenticationDialogView : DialogUserControl
    {
        public AuthenticationDialogView()
        {
            InitializeComponent();
            DataContextChanged += (s, e) =>
            {
                try { ((dynamic)DataContext).AuthenticationPassword = AuthenticationPassword; }
                catch { }
            };
        }
    }
}

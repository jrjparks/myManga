using System;
using System.Windows;
using System.Windows.Input;
using myManga.Properties;
using Microsoft.Windows.Shell;

namespace myManga
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.WindowState = Settings.Default.LastWindowState;
            this.StateChanged += (s, e) => UpdateLastWindowStateSetting();
            Settings.Default.PropertyChanged += (s, e) => { if (e.PropertyName.Equals("ChangeLastWindowState")) { UpdateLastWindowStateSetting(); } };
            InitializeComponent();
#if DEBUG
            this.Title = String.Format("{0} - DEBUG - v{1}", this.Title, System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
#endif
        }

        private void UpdateLastWindowStateSetting()
        {
            if (Settings.Default.ChangeLastWindowState)
                Settings.Default.LastWindowState = (!this.WindowState.Equals(WindowState.Minimized)) ? this.WindowState : Settings.Default.LastWindowState;
            Settings.Default.Save();
        }

        private void _OnSystemCommandCloseWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow((Window)e.Parameter);
        }

        private void _OnSystemCommandMaximizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            if (!this.WindowState.Equals(System.Windows.WindowState.Maximized))
                SystemCommands.MaximizeWindow((Window)e.Parameter);
            else
                SystemCommands.RestoreWindow((Window)e.Parameter);
        }

        private void _OnSystemCommandMinimizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow((Window)e.Parameter);
        }

        private void _OnSystemCommandShowSystemMenu(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.ShowSystemMenu((Window)e.Parameter, this.PointToScreen(new Point(28, 21)));
        }
    }
}

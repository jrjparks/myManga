using System;
using System.Windows;
using System.Windows.Input;
using myManga.Properties;
using Microsoft.Windows.Shell;
using BakaBox.MVVM.Communications;
using BakaBox.MVVM;

namespace myManga
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Point MousePoint;
        public MainWindow()
        {
            this.MouseMove += (s, e) => { MousePoint = this.PointToScreen(e.GetPosition(this)); };
            this.WindowState = Settings.Default.LastWindowState;
            this.StateChanged += (s, e) => UpdateLastWindowStateSetting();
            
            Settings.Default.PropertyChanged += (s, e) => { if (e.PropertyName.Equals("ChangeLastWindowState")) { UpdateLastWindowStateSetting(); } };

            Messenger.Instance.BroadcastMessage += Instance_BroadcastMessage;

            InitializeComponent();
#if DEBUG
            this.Title = String.Format("{0} - DEBUG - v{1}", this.Title, System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
#endif
        }

        void Instance_BroadcastMessage(object Sender, object Data)
        {
            if (Sender is ViewModelBase)
            {
                if (Data is String)
                    if (Data.Equals("!^RequestCanExecuteUpdate"))
                        CommandManager.InvalidateRequerySuggested();
            }
        }

        private void UpdateLastWindowStateSetting()
        {
            if (Settings.Default.ChangeLastWindowState)
                Settings.Default.LastWindowState = (!this.WindowState.Equals(WindowState.Minimized)) ? this.WindowState : Settings.Default.LastWindowState;
            Settings.Default.Save();
        }

        private void _OnSystemCommandCloseWindow(object sender, ExecutedRoutedEventArgs e)
        {
            if ((e.Parameter as Window) == this)
                SystemCommands.CloseWindow(this);
        }

        private void _OnSystemCommandMaximizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            if ((e.Parameter as Window) == this)
                if (!this.WindowState.Equals(System.Windows.WindowState.Maximized))
                    SystemCommands.MaximizeWindow(this);
                else
                    SystemCommands.RestoreWindow(this);
        }

        private void _OnSystemCommandMinimizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            if ((e.Parameter as Window) == this)
                SystemCommands.MinimizeWindow(this);
        }

        private void _OnSystemCommandShowSystemMenu(object sender, ExecutedRoutedEventArgs e)
        {
            if ((e.Parameter as Window) == this)
                SystemCommands.ShowSystemMenu(this, MousePoint);
        }
    }
}

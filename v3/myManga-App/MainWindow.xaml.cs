using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using myManga_App.Properties;

namespace myManga_App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int SYSMENU_X = 35, SYSMENU_Y = 25;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void _OnSystemCommandCanResizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.ResizeMode == ResizeMode.CanResize || this.ResizeMode == ResizeMode.CanResizeWithGrip;
        }

        private void _OnSystemCommandCanMinimizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.ResizeMode != ResizeMode.NoResize;
        }

        private void _OnSystemCommandCloseWindow(object target, ExecutedRoutedEventArgs e)
        {
            if ((target as Window) == this)
                SystemCommands.CloseWindow(this);
        }

        private void _OnSystemCommandMaximizeWindow(object target, ExecutedRoutedEventArgs e)
        {
            if ((target as Window) == this)
                SystemCommands.MaximizeWindow(this);
        }

        private void _OnSystemCommandRestoreWindow(object target, ExecutedRoutedEventArgs e)
        {
            if ((target as Window) == this)
                SystemCommands.RestoreWindow(this);
        }

        private void _OnSystemCommandMinimizeWindow(object target, ExecutedRoutedEventArgs e)
        {
            if ((target as Window) == this)
                SystemCommands.MinimizeWindow(this);
        }

        private void _OnSystemCommandShowSystemMenu(object target, ExecutedRoutedEventArgs e)
        {
            if ((target as Window) == this)
                SystemCommands.ShowSystemMenu(this, new Point(this.Left + SYSMENU_X, this.Top + SYSMENU_Y));
        }
    }
}

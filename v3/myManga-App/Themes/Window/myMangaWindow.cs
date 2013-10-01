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

namespace myManga_App.Themes.Window
{
    public class myMangaWindow : System.Windows.Window
    {
        public Int32 SYSMENU_X { get; set; }
        public Int32 SYSMENU_Y { get; set; }

        public static readonly DependencyProperty SYSMENU_XProperty =
        DependencyProperty.RegisterAttached(
            "SYSMENU_X",
            typeof(Int32),
            typeof(myMangaWindow),
            new PropertyMetadata(35));

        public static readonly DependencyProperty SYSMENU_YProperty =
        DependencyProperty.RegisterAttached(
            "SYSMENU_Y",
            typeof(Int32),
            typeof(myMangaWindow),
            new PropertyMetadata(25));

        public myMangaWindow()
        {
            this.CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, OnSystemCommandCloseWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, OnSystemCommandMaximizeWindow, OnSystemCommandCanResizeWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, OnSystemCommandMinimizeWindow, OnSystemCommandCanMinimizeWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, OnSystemCommandRestoreWindow));
            this.CommandBindings.Add(new CommandBinding(SystemCommands.ShowSystemMenuCommand, OnSystemCommandShowSystemMenu));
        }

        protected virtual void OnSystemCommandCanResizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.ResizeMode == ResizeMode.CanResize || this.ResizeMode == ResizeMode.CanResizeWithGrip;
        }

        protected virtual void OnSystemCommandCanMinimizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.ResizeMode != ResizeMode.NoResize;
        }

        protected virtual void OnSystemCommandCloseWindow(object target, ExecutedRoutedEventArgs e)
        {
            if ((target as myMangaWindow) == this)
                SystemCommands.CloseWindow(this);
        }

        protected virtual void OnSystemCommandMaximizeWindow(object target, ExecutedRoutedEventArgs e)
        {
            if ((target as myMangaWindow) == this)
                SystemCommands.MaximizeWindow(this);
        }

        protected virtual void OnSystemCommandRestoreWindow(object target, ExecutedRoutedEventArgs e)
        {
            if ((target as myMangaWindow) == this)
                SystemCommands.RestoreWindow(this);
        }

        protected virtual void OnSystemCommandMinimizeWindow(object target, ExecutedRoutedEventArgs e)
        {
            if ((target as myMangaWindow) == this)
                SystemCommands.MinimizeWindow(this);
        }

        protected virtual void OnSystemCommandShowSystemMenu(object target, ExecutedRoutedEventArgs e)
        {
            if ((target as myMangaWindow) == this)
                SystemCommands.ShowSystemMenu(this, new Point(this.Left + SYSMENU_X, this.Top + SYSMENU_Y));
        }
    }
}

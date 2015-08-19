using System;
using System.Windows;
using System.Windows.Input;

namespace myManga_App.Controls.Window
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

            this.PreviewKeyDown += (s, e) =>
            {
                Key key = (e.Key == Key.System ? e.SystemKey : e.Key);  // Handle true key
                if (key.Equals(Key.F11) || (e.KeyboardDevice.Modifiers.Equals(ModifierKeys.Alt) && key.Equals(Key.Enter)))
                {
                    if (!this.WindowStyle.Equals(WindowStyle.SingleBorderWindow))
                    { RestoreFromFullScreenMode(); }
                    else
                    { FullScreenMode(); }
                }
            };
        }

        #region SystemCommands
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
            { SystemCommands.CloseWindow(this); }
        }

        protected virtual void OnSystemCommandMaximizeWindow(object target, ExecutedRoutedEventArgs e)
        {
            if ((target as myMangaWindow) == this)
            { SystemCommands.MaximizeWindow(this); }
        }

        protected virtual void OnSystemCommandRestoreWindow(object target, ExecutedRoutedEventArgs e)
        {
            if ((target as myMangaWindow) == this)
            {
                if (!this.WindowStyle.Equals(WindowStyle.SingleBorderWindow))
                { RestoreFromFullScreenMode(); }
                SystemCommands.RestoreWindow(this);
            }
        }

        protected virtual void OnSystemCommandMinimizeWindow(object target, ExecutedRoutedEventArgs e)
        {
            if ((target as myMangaWindow) == this)
            { SystemCommands.MinimizeWindow(this); }
        }

        protected virtual void OnSystemCommandShowSystemMenu(object target, ExecutedRoutedEventArgs e)
        {
            if ((target as myMangaWindow) == this)
            { SystemCommands.ShowSystemMenu(this, new Point(this.Left + SYSMENU_X, this.Top + SYSMENU_Y)); }
        }
        #endregion

        #region Full Screen Control
        protected Boolean WasMaximized { get; set; }

        protected void FullScreenMode()
        {
            this.Topmost = true;
            this.ResizeMode = ResizeMode.CanMinimize;
            this.WindowStyle = WindowStyle.None;
            if (this.WindowState.Equals(WindowState.Maximized))
            { // Needed for maximized windows
                SystemCommands.RestoreWindow(this);
                WasMaximized = true;
            }
            else { WasMaximized = false; }
            SystemCommands.MaximizeWindow(this);
        }

        protected void RestoreFromFullScreenMode()
        {
            SystemCommands.RestoreWindow(this);
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.ResizeMode = ResizeMode.CanResize;
            this.Topmost = false;
            if (WasMaximized)
            { SystemCommands.MaximizeWindow(this); }
        }
        #endregion
    }
}

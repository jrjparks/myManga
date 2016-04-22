using System.Windows.Input;

namespace System.Windows
{
    public class ThemedWindow : Window
    {
        public Int32 SYSMENU_X { get; set; }
        public Int32 SYSMENU_Y { get; set; }

        public static readonly DependencyProperty SYSMENU_XProperty =
        DependencyProperty.RegisterAttached(
            "SYSMENU_X",
            typeof(Int32),
            typeof(ThemedWindow),
            new PropertyMetadata(35));

        public static readonly DependencyProperty SYSMENU_YProperty =
        DependencyProperty.RegisterAttached(
            "SYSMENU_Y",
            typeof(Int32),
            typeof(ThemedWindow),
            new PropertyMetadata(25));

        public ThemedWindow()
        {
            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, OnSystemCommandCloseWindow));
            CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, OnSystemCommandMaximizeWindow, OnSystemCommandCanResizeWindow));
            CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, OnSystemCommandMinimizeWindow, OnSystemCommandCanMinimizeWindow));
            CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, OnSystemCommandRestoreWindow));
            CommandBindings.Add(new CommandBinding(SystemCommands.ShowSystemMenuCommand, OnSystemCommandShowSystemMenu));

            PreviewKeyDown += (s, e) =>
            {
                Key key = (e.Key == Key.System ? e.SystemKey : e.Key);  // Handle true key
                if (key.Equals(Key.F11) || (e.KeyboardDevice.Modifiers.Equals(ModifierKeys.Alt) && key.Equals(Key.Enter)))
                {
                    if (!WindowStyle.Equals(WindowStyle.SingleBorderWindow))
                    { RestoreFromFullScreenMode(); }
                    else
                    { FullScreenMode(); }
                }
            };
        }

        #region SystemCommands
        protected virtual void OnSystemCommandCanResizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ResizeMode == ResizeMode.CanResize || ResizeMode == ResizeMode.CanResizeWithGrip;
        }

        protected virtual void OnSystemCommandCanMinimizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ResizeMode != ResizeMode.NoResize;
        }

        protected virtual void OnSystemCommandCloseWindow(object target, ExecutedRoutedEventArgs e)
        {
            if ((target as ThemedWindow) == this)
            { SystemCommands.CloseWindow(this); }
        }

        protected virtual void OnSystemCommandMaximizeWindow(object target, ExecutedRoutedEventArgs e)
        {
            if ((target as ThemedWindow) == this)
            { SystemCommands.MaximizeWindow(this); }
        }

        protected virtual void OnSystemCommandRestoreWindow(object target, ExecutedRoutedEventArgs e)
        {
            if ((target as ThemedWindow) == this)
            {
                if (!this.WindowStyle.Equals(WindowStyle.SingleBorderWindow))
                { RestoreFromFullScreenMode(); }
                SystemCommands.RestoreWindow(this);
            }
        }

        protected virtual void OnSystemCommandMinimizeWindow(object target, ExecutedRoutedEventArgs e)
        {
            if ((target as ThemedWindow) == this)
            { SystemCommands.MinimizeWindow(this); }
        }

        protected virtual void OnSystemCommandShowSystemMenu(object target, ExecutedRoutedEventArgs e)
        {
            if ((target as ThemedWindow) == this)
            { SystemCommands.ShowSystemMenu(this, new Point(Left + SYSMENU_X, Top + SYSMENU_Y)); }
        }
        #endregion

        #region Full Screen Control
        protected Boolean WasMaximized { get; set; }

        protected void FullScreenMode()
        {
            Topmost = true;
            ResizeMode = ResizeMode.CanMinimize;
            WindowStyle = WindowStyle.None;
            if (WindowState.Equals(WindowState.Maximized))
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
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            Topmost = false;
            if (WasMaximized)
            { SystemCommands.MaximizeWindow(this); }
        }
        #endregion
    }
}

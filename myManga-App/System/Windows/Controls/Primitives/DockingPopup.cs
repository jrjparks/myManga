namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// A type of Popup that will follow the window.
    /// </summary>
    public partial class DockingPopup : Popup
    {
        public DockingPopup() : base()
        { this.Loaded += DockingPopup_Loaded; }

        private void DockingPopup_Loaded(object sender, System.Windows.RoutedEventArgs args)
        {
            System.Windows.Window _window = System.Windows.Window.GetWindow(this.PlacementTarget ?? this);
            if (_window != null)
            {
                System.Reflection.MethodInfo RepositionMethodInfo = typeof(DockingPopup).GetMethod("Reposition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                _window.LocationChanged += (s, e) =>
                { if (this.IsOpen) { RepositionMethodInfo.Invoke(this, null); } };

                _window.SizeChanged += (s, e) =>
                { if (this.IsOpen) { RepositionMethodInfo.Invoke(this, null); } };
            }
        }
    }
}

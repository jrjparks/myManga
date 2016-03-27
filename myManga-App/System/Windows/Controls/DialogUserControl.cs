using System.ComponentModel;

namespace System.Windows.Controls
{
    public class DialogUserControl : UserControl
    {
        #region Dialog Content

        #region ActualWidth
        public static readonly DependencyProperty DialogActualWidthProperty = DependencyProperty.RegisterAttached(
            "DialogActualWidth",
            typeof(Double),
            typeof(DialogUserControl),
            new FrameworkPropertyMetadata(Double.NaN));

        [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
        [TypeConverter(typeof(LengthConverter))]
        public Double DialogActualWidth
        {
            get { return (Double)GetValue(DialogActualWidthProperty); }
            set { SetValue(DialogActualWidthProperty, value); }
        }
        #endregion

        #region ActualHeight
        public static readonly DependencyProperty DialogActualHeightProperty = DependencyProperty.RegisterAttached(
            "DialogActualHeight",
            typeof(Double),
            typeof(DialogUserControl),
            new FrameworkPropertyMetadata(Double.NaN));

        [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
        [TypeConverter(typeof(LengthConverter))]
        public Double DialogActualHeight
        {
            get { return (Double)GetValue(DialogActualHeightProperty); }
            set { SetValue(DialogActualHeightProperty, value); }
        }
        #endregion

        #region MinWidth
        public static readonly DependencyProperty DialogMinWidthProperty = DependencyProperty.RegisterAttached(
            "DialogMinWidth",
            typeof(Double),
            typeof(DialogUserControl),
            new FrameworkPropertyMetadata(Double.NaN));

        [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
        [TypeConverter(typeof(LengthConverter))]
        public Double DialogMinWidth
        {
            get { return (Double)GetValue(DialogMinWidthProperty); }
            set { SetValue(DialogMinWidthProperty, value); }
        }
        #endregion

        #region Width
        public static readonly DependencyProperty DialogWidthProperty = DependencyProperty.RegisterAttached(
            "DialogWidth",
            typeof(Double),
            typeof(DialogUserControl),
            new FrameworkPropertyMetadata(Double.NaN));

        [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
        [TypeConverter(typeof(LengthConverter))]
        public Double DialogWidth
        {
            get { return (Double)GetValue(DialogWidthProperty); }
            set { SetValue(DialogWidthProperty, value); }
        }
        #endregion

        #region MaxWidth
        public static readonly DependencyProperty DialogMaxWidthProperty = DependencyProperty.RegisterAttached(
            "DialogMaxWidth",
            typeof(Double),
            typeof(DialogUserControl),
            new FrameworkPropertyMetadata(Double.NaN));

        [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
        [TypeConverter(typeof(LengthConverter))]
        public Double DialogMaxWidth
        {
            get { return (Double)GetValue(DialogMaxWidthProperty); }
            set { SetValue(DialogMaxWidthProperty, value); }
        }
        #endregion

        #region MinHeight
        public static readonly DependencyProperty DialogMinHeightProperty = DependencyProperty.RegisterAttached(
            "DialogMinHeight",
            typeof(Double),
            typeof(DialogUserControl),
            new FrameworkPropertyMetadata(Double.NaN));

        [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
        [TypeConverter(typeof(LengthConverter))]
        public Double DialogMinHeight
        {
            get { return (Double)GetValue(DialogMinHeightProperty); }
            set { SetValue(DialogMinHeightProperty, value); }
        }
        #endregion

        #region Height
        public static readonly DependencyProperty DialogHeightProperty = DependencyProperty.RegisterAttached(
            "DialogHeight",
            typeof(Double),
            typeof(DialogUserControl),
            new FrameworkPropertyMetadata(Double.NaN));

        [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
        [TypeConverter(typeof(LengthConverter))]
        public Double DialogHeight
        {
            get { return (Double)GetValue(DialogHeightProperty); }
            set { SetValue(DialogHeightProperty, value); }
        }
        #endregion

        #region MaxHeight
        public static readonly DependencyProperty DialogMaxHeightProperty = DependencyProperty.RegisterAttached(
            "DialogMaxHeight",
            typeof(Double),
            typeof(DialogUserControl),
            new FrameworkPropertyMetadata(Double.NaN));

        [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
        [TypeConverter(typeof(LengthConverter))]
        public Double DialogMaxHeight
        {
            get { return (Double)GetValue(DialogMaxHeightProperty); }
            set { SetValue(DialogMaxHeightProperty, value); }
        }
        #endregion

        #region Margin
        public static readonly DependencyProperty DialogMarginProperty = DependencyProperty.RegisterAttached(
            "DialogMargin",
            typeof(Thickness),
            typeof(DialogUserControl),
            new PropertyMetadata(new Thickness(0)));

        [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
        [TypeConverter(typeof(ThicknessConverter))]
        public Thickness DialogMargin
        {
            get { return (Thickness)GetValue(DialogMarginProperty); }
            set { SetValue(DialogMarginProperty, value); }
        }
        #endregion

        #endregion

        public DialogUserControl() : base()
        {
            DialogActualWidth = DialogWidth;
            DialogActualHeight = DialogHeight;
            SizeChanged += DialogUserControl_SizeChanged;
        }

        private void DialogUserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Double.IsNaN(DialogHeight))
            {
                Double _DialogActualHeight = e.NewSize.Height; // - (DialogMargin.Top + DialogMargin.Bottom);
                if (_DialogActualHeight < DialogMinHeight) _DialogActualHeight = DialogMinHeight;
                if (_DialogActualHeight > DialogMaxHeight) _DialogActualHeight = DialogMaxHeight;

                DialogActualHeight = _DialogActualHeight;
            }
            else DialogActualHeight = DialogHeight;

            if (Double.IsNaN(DialogWidth))
            {
                Double _DialogActualWidth = e.NewSize.Width; // - (DialogMargin.Left + DialogMargin.Right);
                if (_DialogActualWidth < DialogMinWidth) _DialogActualWidth = DialogMinWidth;
                if (_DialogActualWidth > DialogMaxWidth) _DialogActualWidth = DialogMaxWidth;

                DialogActualWidth = _DialogActualWidth;
            }
            else DialogActualWidth = DialogWidth;
        }
    }
}

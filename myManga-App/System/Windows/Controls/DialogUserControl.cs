using System.ComponentModel;

namespace System.Windows.Controls
{
    public class DialogUserControl : UserControl
    {
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

        public DialogUserControl() : base() { }
    }
}

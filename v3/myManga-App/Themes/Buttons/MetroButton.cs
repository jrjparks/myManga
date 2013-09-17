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

namespace myManga_App.Resources.Theme.Buttons
{
    public class MetroButton : Button
    {
        static MetroButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MetroButton), new FrameworkPropertyMetadata(typeof(MetroButton)));
        }

        public static readonly DependencyProperty PrimaryBrushProperty =
        DependencyProperty.RegisterAttached(
            "PrimaryBrush",
            typeof(Brush),
            typeof(MetroButton),
            new FrameworkPropertyMetadata(Brushes.Black));

        public static readonly DependencyProperty SecondaryBrushProperty =
        DependencyProperty.RegisterAttached(
            "SecondaryBrush",
            typeof(Brush),
            typeof(MetroButton),
            new FrameworkPropertyMetadata(Brushes.Black));

        public static readonly DependencyProperty PrimaryDisabledBrushProperty =
        DependencyProperty.RegisterAttached(
            "PrimaryDisabledBrush",
            typeof(Brush),
            typeof(MetroButton),
            new FrameworkPropertyMetadata(Brushes.Black));

        public static readonly DependencyProperty SecondaryDisabledBrushProperty =
        DependencyProperty.RegisterAttached(
            "SecondaryDisabledBrush",
            typeof(Brush),
            typeof(MetroButton),
            new FrameworkPropertyMetadata(Brushes.Black));

        public Brush PrimaryBrush
        {
            get { return (Brush)GetValue(PrimaryBrushProperty); }
            set { SetValue(PrimaryBrushProperty, value); }
        }
        public Brush SecondaryBrush
        {
            get { return (Brush)GetValue(SecondaryBrushProperty); }
            set { SetValue(SecondaryBrushProperty, value); }
        }
        public Brush PrimaryDisabledBrush
        {
            get { return (Brush)GetValue(PrimaryDisabledBrushProperty); }
            set { SetValue(PrimaryDisabledBrushProperty, value); }
        }
        public Brush SecondaryDisabledBrush
        {
            get { return (Brush)GetValue(SecondaryDisabledBrushProperty); }
            set { SetValue(SecondaryDisabledBrushProperty, value); }
        }
    }
}

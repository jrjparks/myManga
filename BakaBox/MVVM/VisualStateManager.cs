using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace BakaBox.MVVM
{
    [DebuggerStepThrough]
    public class VisualStateManager : DependencyObject
    {
        public static String GetVisualStateProperty(DependencyObject DepObj)
        {
            return (String)DepObj.GetValue(VisualStatePropertyProperty);
        }

        public static void SetVisualStateProperty(DependencyObject DepObj, String Value)
        {
            DepObj.SetValue(VisualStatePropertyProperty, Value);
        }

        public static readonly DependencyProperty VisualStatePropertyProperty =
            DependencyProperty.RegisterAttached(
            "VisualStateProperty",
            typeof(String),
            typeof(VisualStateManager),
            new PropertyMetadata((s, e) =>
            {
                String PropertyName = (String)e.NewValue;
                Control Control = s as Control;
                if (Control == null)
                    throw new InvalidOperationException("This attached property only supports types derived from Control.");
                System.Windows.VisualStateManager.GoToState(Control, PropertyName, true);
            }));
    }
}
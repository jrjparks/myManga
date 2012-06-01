using System;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace BakaBox.MVVM
{
    [DebuggerStepThrough]
    public static class VisualStates
    {
        public static readonly DependencyProperty CurrentStateProperty =
            DependencyProperty.RegisterAttached("CurrentState", typeof(String), typeof(VisualStates), new PropertyMetadata(TransitionToState));

        public static String GetCurrentState(DependencyObject DepObj)
        {
            return (String)DepObj.GetValue(CurrentStateProperty);
        }

        public static void SetCurrentState(DependencyObject DepObj, String Value)
        {
            DepObj.SetValue(CurrentStateProperty, Value);
        }

        private static void TransitionToState(Object sender, DependencyPropertyChangedEventArgs e)
        {
            String PropertyName = (String)e.NewValue;
            Control Control = sender as Control;
            if (Control != null)
                System.Windows.VisualStateManager.GoToState(Control, PropertyName, true);
            else
                throw new ArgumentException("CurrentState is only supported on the Control type");
        }
    }
}
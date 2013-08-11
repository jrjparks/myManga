using System;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;

namespace BakaBox.MVVM
{
    [DebuggerStepThrough]
    public static class Commands
    {
        public static readonly DependencyProperty MouseEnterCommandProperty =
         DependencyProperty.RegisterAttached("MouseEnterCommand", typeof(ICommand), typeof(Commands),
         new PropertyMetadata(new PropertyChangedCallback(AttachOrRemoveMouseEnterEvent)));

        public static readonly DependencyProperty MouseLeaveCommandProperty =
            DependencyProperty.RegisterAttached("MouseLeaveCommand", typeof(ICommand), typeof(Commands),
            new PropertyMetadata(new PropertyChangedCallback(AttachOrRemoveMouseLeaveEvent)));

        public static ICommand GetMouseEnterCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(MouseEnterCommandProperty);
        }

        public static void SetMouseEnterCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(MouseEnterCommandProperty, value);
        }

        public static ICommand GetMouseLeaveCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(MouseLeaveCommandProperty);
        }

        public static void SetMouseLeaveCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(MouseLeaveCommandProperty, value);
        }

        private static void AttachOrRemoveMouseEnterEvent(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {

            FrameworkElement element = obj as FrameworkElement;
            if (element != null)
            {
                ICommand command = (ICommand)args.NewValue;

                if (args.OldValue == null && args.NewValue != null)
                {
                    element.MouseEnter += ExecuteMouseEnterCommand;
                }
                else if (args.NewValue == null && args.OldValue != null)
                {
                    element.MouseEnter -= ExecuteMouseEnterCommand;
                }
            }
            else
            {
                throw new ArgumentException("MouseEnterCommand is only supported on FrameworkElement");
            }
        }

        private static void AttachOrRemoveMouseLeaveEvent(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            FrameworkElement element = obj as FrameworkElement;
            if (element != null)
            {
                ICommand command = (ICommand)args.NewValue;
                if (args.OldValue == null && args.NewValue != null)
                {
                    element.MouseLeave += ExecuteMouseLeaveCommand;
                }
                else if (args.NewValue == null && args.OldValue != null)
                {
                    element.MouseLeave -= ExecuteMouseLeaveCommand;
                }
            }
            else
            {
                throw new ArgumentException("MouseLeaveCommand is only supported on FrameworkElement");
            }

        }

        private static void ExecuteMouseEnterCommand(object sender, MouseEventArgs args)
        {
            DependencyObject dSender = (DependencyObject)sender;
            ICommand toExecute = (ICommand)dSender.GetValue(MouseEnterCommandProperty);
            if (toExecute.CanExecute(args))
            {
                toExecute.Execute(args);
            }
        }

        private static void ExecuteMouseLeaveCommand(object sender, MouseEventArgs args)
        {
            DependencyObject dSender = (DependencyObject)sender;
            ICommand toExecute = (ICommand)dSender.GetValue(MouseLeaveCommandProperty);
            if (toExecute.CanExecute(args))
            {
                toExecute.Execute(args);
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace myManga_App.Controls
{
    /// <summary>
    /// Allow InputBinding Precedence
    /// </summary>
    [DebuggerStepThrough]
    public class InputBindingsBehavior
    {
        public static readonly DependencyProperty HasInputBindingPrecedenceProperty =
            DependencyProperty.RegisterAttached(
            "HasInputBindingPrecedence", 
            typeof(Boolean), 
            typeof(InputBindingsBehavior), 
            new UIPropertyMetadata(false, OnTakesInputBindingPrecedenceChanged));

        public static Boolean GetHasInputBindingPrecedence(UIElement element)
        { return (Boolean)element.GetValue(HasInputBindingPrecedenceProperty); }

        public static void SetHasInputBindingPrecedence(UIElement element, Boolean value)
        { element.SetValue(HasInputBindingPrecedenceProperty, value); }

        private static void OnTakesInputBindingPrecedenceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        { (d as UIElement).PreviewKeyDown += new KeyEventHandler(InputBindingsBehavior_PreviewKeyDown); }

        private static void InputBindingsBehavior_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var uielement = (UIElement)sender;

            var foundBinding = uielement.InputBindings
                .OfType<KeyBinding>()
                .FirstOrDefault(kb => kb.Key == e.Key && kb.Modifiers == e.KeyboardDevice.Modifiers);

            if (foundBinding != null)
            {
                if (foundBinding.Command.CanExecute(foundBinding.CommandParameter))
                { e.Handled = true; foundBinding.Command.Execute(foundBinding.CommandParameter); }
            }
        }
    }
}

using System;
using System.Collections;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows;

namespace myManga_App.Controls
{
    public static class ListBoxExtensions
    {
        public static readonly DependencyProperty HasBindableSelectedItemsProperty;
        public static readonly DependencyProperty BindableSelectedItemsProperty;
        static DependencyProperty SelectionChangedHandlerProperty;

        static ListBoxExtensions()
        {
            BindableSelectedItemsProperty = DependencyProperty.Register("BindableSelectedItems", typeof(IList), typeof(ListBox));
            HasBindableSelectedItemsProperty = DependencyProperty.RegisterAttached("HasBindableSelectedItems", typeof(bool), typeof(ListBox), new PropertyMetadata(false));
            SelectionChangedHandlerProperty = DependencyProperty.RegisterAttached("SelectionChangedHandler", typeof(SelectionChangedHandler), typeof(ListBox));
        }

        public static void SetHasBindableSelectedItems(ListBox source, bool value)
        {
            SelectionChangedHandler Handler = (SelectionChangedHandler)source.GetValue(SelectionChangedHandlerProperty);
            if (value && Handler == null)
            {
                Handler = new SelectionChangedHandler(source);
                source.SetValue(SelectionChangedHandlerProperty, Handler);
            }
            else if (!value && Handler != null)
            { source.ClearValue(SelectionChangedHandlerProperty); }
        }
    }

    internal class SelectionChangedHandler
    {
        Binding Binding;

        internal SelectionChangedHandler(ListBox owner)
        {
            Binding = new Binding("SelectedItems");
            Binding.Source = owner;
            owner.SetBinding(ListBoxExtensions.BindableSelectedItemsProperty, Binding);
            owner.SelectionChanged += new SelectionChangedEventHandler(Owner_SelectionChanged);
        }

        void Owner_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox Owner = sender as ListBox;
            BindingOperations.ClearBinding(Owner, ListBoxExtensions.BindableSelectedItemsProperty);
            Owner.SetBinding(ListBoxExtensions.BindableSelectedItemsProperty, Binding);
        }
    }
}

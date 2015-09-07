using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace myManga_App.Controls.Selectors
{
    public sealed class ListBox : System.Windows.Controls.ListBox
    {
        private static readonly DependencyProperty BindableSelectedItemsProperty = DependencyProperty.RegisterAttached(
            "BindableSelectedItems",
            typeof(IList),
            typeof(ListBox),
            new PropertyMetadata((IList)null));

        public IList BindableSelectedItems
        {
            get { return (IList)GetValue(BindableSelectedItemsProperty); }
            set { SetValue(BindableSelectedItemsProperty, value); }
        }

        public ListBox() : base()
        {
            this.SelectionChanged += SelectedItemsAttachedProperty_SelectionChanged;
        }

        private static void SelectedItemsAttachedProperty_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox ListObject = (sender as ListBox);
            IList ModelSelectedItems = (IList)ListObject.GetValue(BindableSelectedItemsProperty);

            ModelSelectedItems.Clear();

            if (ListObject.SelectedItems != null)
            {
                foreach (var item in ListObject.SelectedItems)
                { ModelSelectedItems.Add(item); }
            }
            ListObject.SetValue(BindableSelectedItemsProperty, ModelSelectedItems);
        }
    }
}

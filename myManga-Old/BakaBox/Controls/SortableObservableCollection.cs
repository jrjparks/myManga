using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;

namespace BakaBox.Controls
{
    [DebuggerStepThrough]
    public class SortableObservableCollection<T> : ObservableCollection<T>
    {
        public SortableObservableCollection()
            : base() { }

        public SortableObservableCollection(List<T> list)
            : base(list) { }

        public SortableObservableCollection(IEnumerable<T> collection)
            : base(collection) { }

        public void Sort<TKey>(Func<T, TKey> keySelector, System.ComponentModel.ListSortDirection direction)
        {
            switch (direction)
            {
                case System.ComponentModel.ListSortDirection.Ascending:
                    {
                        ApplySort(Items.OrderBy(keySelector));
                        break;
                    }
                case System.ComponentModel.ListSortDirection.Descending:
                    {
                        ApplySort(Items.OrderByDescending(keySelector));
                        break;
                    }
            }
        }

        public void Sort<TKey>(Func<T, TKey> keySelector, IComparer<TKey> comparer)
        {
            ApplySort(Items.OrderBy(keySelector, comparer));
        }

        private void ApplySort(IEnumerable<T> sortedItems)
        {
            List<T> sortedItemsList = sortedItems.ToList();

            for (int i = 0; i < sortedItemsList.Count; ++i)
                Move(IndexOf(sortedItemsList[i]), i);
        }

        public T First()
        { return Items.First(); }
        public T First(Func<T, Boolean> predicate)
        { return Items.First(predicate); }

        public T Last()
        { return Items.Last(); }
        public T Last(Func<T, Boolean> predicate)
        { return Items.Last(predicate); }
    }
}

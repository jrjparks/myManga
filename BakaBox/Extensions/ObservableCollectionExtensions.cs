using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BakaBox.Extensions
{
    public static class ObservableCollectionExtensions
    {
        public static void Sort<T>(this ObservableCollection<T> collection, Comparison<T> comparison)
        {
            var comparer = new Comparer<T>(comparison);

            List<T> sorted = collection.OrderBy(x => x, comparer).ToList();

            for (int i = 0; i < sorted.Count(); i++)
                collection.Move(collection.IndexOf(sorted[i]), i);
        }

        public static void DescendingSort<T>(this ObservableCollection<T> collection, Comparison<T> comparison)
        {
            var comparer = new ReverseComparer<T>(comparison);

            List<T> sorted = collection.OrderBy(x => x, comparer).ToList();

            for (int i = 0; i < sorted.Count(); i++)
                collection.Move(collection.IndexOf(sorted[i]), i);
        }
    }

    internal class Comparer<T> : IComparer<T>
    {
        private readonly Comparison<T> comparison;

        public Comparer(Comparison<T> comparison)
        {
            this.comparison = comparison;
        }
        #region IComparer<T> Members

        public int Compare(T x, T y)
        {
            return comparison.Invoke(x, y);
        }
        #endregion
    }

    internal class ReverseComparer<T> : IComparer<T>
    {
        private readonly Comparison<T> comparison;

        public ReverseComparer(Comparison<T> comparison)
        {
            this.comparison = comparison;
        }
        #region IComparer<T> Members

        public int Compare(T x, T y)
        {
            return -comparison.Invoke(x, y);
        }
        #endregion
    } 
}

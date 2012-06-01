using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BakaBox.Extensions
{
    public static class QueueExtensions
    {
        public static Boolean Remove<T>(this Queue<T> queue, T value)
        {
            T[] items = new T[queue.Count];
            queue.CopyTo(items, 0);
            queue.Clear();
            Boolean removed = false;
            foreach (T item in queue)
                if (!item.Equals(value))
                    queue.Enqueue(item);
                else if (!removed)
                    removed = true;
            return removed;
        }
    }
}

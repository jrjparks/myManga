using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace System.Threading.Tasks
{
    public static class TaskForEach
    {
        public static Task ForEachAsyncTask<T>(this IEnumerable<T> collection, Func<T, Task> asyncFunction, Int32 partitionCount = 1, Action<Task> continueWith = null)
        {
            continueWith = continueWith ?? new Action<Task>(task => { });
            return Task.WhenAll(
                from partition in Partitioner.Create(collection).GetPartitions(partitionCount)
                select Task.Run(async delegate { using (partition) { while (partition.MoveNext()) await asyncFunction(partition.Current).ContinueWith(continueWith); } }));
        }
    }
}

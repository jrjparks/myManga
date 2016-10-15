using System.Diagnostics;

namespace System.Threading.Tasks
{
    public static class TaskRetry
    {
        /// <summary>
        /// Async Task Retry
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="task">Task to Retry</param>
        /// <param name="timeout">Allowed TimeSpan timeout</param>
        /// <returns>Awaitable task</returns>
        public static Task<TResult> Retry<TResult>(this Task<TResult> task, TimeSpan timeout)
        { return Retry(task: task, timeout: timeout, delay: TimeSpan.FromSeconds(1), delayIncrement: TimeSpan.Zero); }

        /// <summary>
        /// Async Task Retry
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="task">Task to Retry</param>
        /// <param name="timeout">Allowed TimeSpan timeout</param>
        /// <param name="delay"></param>
        /// <returns>Awaitable task</returns>
        public static Task<TResult> Retry<TResult>(this Task<TResult> task, TimeSpan timeout, TimeSpan delay)
        { return Retry(task: task, timeout: timeout, delay: delay, delayIncrement: TimeSpan.Zero); }

        /// <summary>
        /// Async Task Retry
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="task">Task to Retry</param>
        /// <param name="timeout">Allowed TimeSpan timeout</param>
        /// <param name="delay"></param>
        /// <param name="delayIncrement"></param>
        /// <returns>Awaitable task</returns>
        public static async Task<TResult> Retry<TResult>(this Task<TResult> task, TimeSpan timeout, TimeSpan delay, TimeSpan delayIncrement)
        {
            Stopwatch watch = Stopwatch.StartNew();
            do
            {
                try { return await task; }
                // Handle OperationCanceledException and throw it.
                catch (OperationCanceledException ocex) { throw ocex; }
                // An exception has occurred, see if we should retry or throw the exception.
                catch (Exception ex)
                {
                    // If the timeout has elapsed, throw the Exception.
                    if (watch.Elapsed >= timeout) throw new TimeoutException("Task Timeout", ex);

                    // await for the delay.
                    if (delay > TimeSpan.Zero) await Task.Delay(delay);

                    // If there is a delayIncrement and it's greater than 0 add it to the delay.
                    if (delayIncrement > TimeSpan.Zero) delay.Add(delayIncrement);
                }
            }
            while (watch.Elapsed < timeout);
            // A timeout occurred.
            throw new TimeoutException("Task Timeout");
        }

        /// <summary>
        /// Async Task Retry
        /// </summary>
        /// <param name="task">Task to Retry</param>
        /// <param name="timeout">Allowed TimeSpan timeout</param>
        /// <returns>Awaitable task</returns>
        public static Task Retry(this Task task, TimeSpan timeout)
        { return Retry(task: task, timeout: timeout, delay: TimeSpan.FromSeconds(1), delayIncrement: TimeSpan.Zero); }

        /// <summary>
        /// Async Task Retry
        /// </summary>
        /// <param name="task">Task to Retry</param>
        /// <param name="timeout">Allowed TimeSpan timeout</param>
        /// <param name="delay"></param>
        /// <returns>Awaitable task</returns>
        public static Task Retry(this Task task, TimeSpan timeout, TimeSpan delay)
        { return Retry(task: task, timeout: timeout, delay: delay, delayIncrement: TimeSpan.Zero); }

        /// <summary>
        /// Async Task Retry
        /// </summary>
        /// <param name="task">Task to Retry</param>
        /// <param name="timeout">Allowed TimeSpan timeout</param>
        /// <param name="delay"></param>
        /// <param name="delayIncrement"></param>
        /// <returns>Awaitable task</returns>
        public static async Task Retry(this Task task, TimeSpan timeout, TimeSpan delay, TimeSpan delayIncrement)
        {
            Stopwatch watch = Stopwatch.StartNew();
            do
            {
                try { await task; }
                // Handle OperationCanceledException and throw it.
                catch (OperationCanceledException ocex) { throw ocex; }
                // An exception has occurred, see if we should retry or throw the exception.
                catch (Exception ex)
                {
                    // If the timeout has elapsed, throw the Exception.
                    if (watch.Elapsed >= timeout) throw new TimeoutException("Task Timeout", ex);

                    // await for the delay.
                    if (delay > TimeSpan.Zero) await Task.Delay(delay);

                    // If there is a delayIncrement and it's greater than 0 add it to the delay.
                    if (delayIncrement > TimeSpan.Zero) delay.Add(delayIncrement);
                }
            }
            while (watch.Elapsed < timeout);
            // A timeout occurred.
            throw new TimeoutException("Task Timeout");
        }
    }
}

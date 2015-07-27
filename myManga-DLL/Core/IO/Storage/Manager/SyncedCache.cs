using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.IO.Storage.Manager
{
    public class SyncedCache<TKey, TValue>
    {
        [Flags]
        public enum SetStatus
        {
            Timeout = 0x00,
            Added = 0x01,
            Updated = 0x02,
            Unchanged = 0x04
        }

        protected readonly ReaderWriterLockSlim cacheLock;
        protected readonly Dictionary<TKey, TValue> cache;

        public SyncedCache()
        {
            cacheLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            cache = new Dictionary<TKey, TValue>();
        }

        /// <summary>
        /// Get value from synced cache at key.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="timeout">Timeout, -1 == No Timeout[default]</param>
        /// <returns>Success status.</returns>
        public virtual TValue Get(TKey key, Int32 timeout = -1)
        {
            if (cacheLock.TryEnterReadLock(timeout))
            {
                try { return cache[key]; }
                finally { cacheLock.ExitReadLock(); }
            }
            else return default(TValue);
        }

        /// <summary>
        /// Set value to the synced cache at key.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <param name="timeout">Timeout, -1 == No Timeout[default]</param>
        /// <returns>Success status.</returns>
        public virtual SetStatus Set(TKey key, TValue value, Int32 timeout = -1)
        {
            if (cacheLock.TryEnterUpgradeableReadLock(timeout))
            {
                try
                {
                    TValue cache_result = default(TValue);
                    if (cache.TryGetValue(key, out cache_result))
                    {
                        if (value.Equals(cache_result))
                            return SetStatus.Unchanged;
                        else
                            if (cacheLock.TryEnterWriteLock(timeout))
                            {
                                try { cache[key] = value; }
                                finally { cacheLock.ExitWriteLock(); }
                                return SetStatus.Updated;
                            }
                            else return SetStatus.Timeout;
                    }
                    else
                        if (cacheLock.TryEnterWriteLock(timeout))
                        {
                            try { cache.Add(key, value); }
                            finally { cacheLock.ExitWriteLock(); }
                            return SetStatus.Added;
                        }
                        else return SetStatus.Timeout;
                }
                finally { cacheLock.ExitUpgradeableReadLock(); }
            }
            else return SetStatus.Timeout;
        }

        /// <summary>
        /// Delete value from synced cache at key.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="timeout">Timeout, -1 == No Timeout[default]</param>
        /// <returns>Success status.</returns>
        public virtual Boolean Delete(TKey key, Int32 timeout = -1)
        {
            if (cacheLock.TryEnterWriteLock(timeout))
            {
                try { return cache.Remove(key); }
                finally { cacheLock.ExitWriteLock(); }
            }
            else return false;
        }
    }
}

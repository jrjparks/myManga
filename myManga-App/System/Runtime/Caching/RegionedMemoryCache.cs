using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Runtime.Caching
{
    public class RegionedMemoryCache : MemoryCache
    {
        private static object s_initLock = new object();
        private static RegionedMemoryCache s_defaultCache;

        public static new RegionedMemoryCache Default
        {
            get
            {
                if (s_defaultCache == null)
                {
                    lock (s_initLock)
                    {
                        if (s_defaultCache == null)
                        {
                            s_defaultCache = new RegionedMemoryCache();
                        }
                    }
                }
                return s_defaultCache;
            }
        }

        public RegionedMemoryCache() : base("defaultRegionedMemoryCache") { }

        public RegionedMemoryCache(string name, NameValueCollection config = null) : base(name, config)
        {
        }

        public override DefaultCacheCapabilities DefaultCacheCapabilities
        {
            get { return (base.DefaultCacheCapabilities | DefaultCacheCapabilities.CacheRegions); }
        }

        private String CreateKeyWithRegion(String Key, String Region = "NullRegion")
        { return String.Format("REGION:{0};KEY:{1}", Region, Key); }

        public override bool Contains(string key, string regionName = null)
        {
            return base.Contains(CreateKeyWithRegion(key, regionName));
        }

        #region Add
        public override bool Add(CacheItem item, CacheItemPolicy policy)
        {
            return Add(item.Key, item.Value, policy, item.RegionName);
        }

        public override bool Add(string key, object value, CacheItemPolicy policy, string regionName = null)
        {
            return base.Add(CreateKeyWithRegion(key, regionName), value, policy);
        }

        public override bool Add(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)
        {
            return base.Add(CreateKeyWithRegion(key, regionName), value, absoluteExpiration);
        }
        #endregion

        #region Set
        public override void Set(CacheItem item, CacheItemPolicy policy)
        {
            Set(item.Key, item.Value, policy, item.RegionName);
        }

        public override void Set(string key, object value, CacheItemPolicy policy, string regionName = null)
        {
            base.Set(CreateKeyWithRegion(key, regionName), value, policy);
        }

        public override void Set(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)
        {
            base.Set(CreateKeyWithRegion(key, regionName), value, absoluteExpiration);
        }
        #endregion

        #region Get
        public override CacheItem GetCacheItem(string key, string regionName = null)
        {
            CacheItem temporary = base.GetCacheItem(CreateKeyWithRegion(key, regionName));
            return new CacheItem(key, temporary.Value, regionName);
        }

        public override object Get(string key, string regionName = null)
        {
            return base.Get(CreateKeyWithRegion(key, regionName));
        }
        #endregion

        #region AddOrGetExisting
        public override CacheItem AddOrGetExisting(CacheItem item, CacheItemPolicy policy)
        {
            return new CacheItem(CreateKeyWithRegion(item.Key, item.RegionName), base.AddOrGetExisting(CreateKeyWithRegion(item.Key, item.RegionName), item.Value, policy));
        }

        public override object AddOrGetExisting(string key, object value, CacheItemPolicy policy, string regionName = null)
        {
            return base.AddOrGetExisting(CreateKeyWithRegion(key, regionName), value, policy);
        }

        public override object AddOrGetExisting(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)
        {
            return base.AddOrGetExisting(CreateKeyWithRegion(key, regionName), value, absoluteExpiration);
        }
        #endregion

        #region Remove
        public override object Remove(string key, string regionName = null)
        {
            return base.Remove(CreateKeyWithRegion(key, regionName));
        }
        #endregion
    }
}

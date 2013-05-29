using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace BakaBox.DLL
{
    public class PluginManager
    {
        #region Instance
        private static PluginManager _Instance;
        private static Object SyncObj = new Object();
        public static PluginManager Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (SyncObj)
                    {
                        if (_Instance == null)
                        { _Instance = new PluginManager(); }
                    }
                }
                return _Instance;
            }
        }

        private PluginManager()
        {
            SyncContext = SynchronizationContext.Current;
            AppDomain = AppDomain.CreateDomain(AppDomainName);
        }
        #endregion

        #region Fields
        private SynchronizationContext SyncContext { get; set; }
        public AppDomain AppDomain { get; set; }

        public String AppDomainName
        {
            get {
                if (AppDomain != null)
                    return AppDomain.FriendlyName;
                return "PluginDomain";
            }
            set {
                if (AppDomain != null)
                {
                    AppDomain.Unload(AppDomain);
                }
                AppDomain = AppDomain.CreateDomain(AppDomainName);
            }
        }
        #endregion

        #region Methods
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections;

namespace BakaBox.DLL
{
    public class PluginManager<T, C> where C : ICollection<T>, new()
    {
        #region Constructor
        public PluginManager()
        {
            SyncContext = SynchronizationContext.Current;
            PluginCollection = new C();
        }
        #endregion

        #region Fields
        public C PluginCollection { get; set; }

        protected SynchronizationContext SyncContext { get; set; }
        protected AppDomain pluginAppDomain { get; set; }
        public AppDomain PluginAppDomain
        {
            get
            {
                if (pluginAppDomain == null)
                    pluginAppDomain = AppDomain.CreateDomain(AppDomainName);
                return pluginAppDomain;
            }
            set { pluginAppDomain = value; }
        }

        protected String pluginAppDomainName { get; set; }
        /// <summary>
        /// Changing the AppDomainName will unload the current AppDomain and Clear the current PluginCollection.
        /// </summary>
        public String AppDomainName
        {
            get
            {
                if (pluginAppDomainName == null || pluginAppDomainName == String.Empty)
                    pluginAppDomainName = "PluginDomain";
                return pluginAppDomainName;
            }
            set
            {
                pluginAppDomainName = value;
                if (PluginAppDomain != null)
                    AppDomain.Unload(PluginAppDomain);
                if (PluginCollection.Count > 0)
                    PluginCollection.Clear();
                PluginAppDomain = AppDomain.CreateDomain(AppDomainName);
            }
        }
        #endregion

        #region Methods
        public void LoadPlugin(String FilePath)
        {
            foreach (T plugin in PluginLoader<T>.LoadPlugin(FilePath))
                PluginCollection.Add(plugin);
        }
        public void LoadPluginDirectory(String Directory)
        {
            foreach (T plugin in PluginLoader<T>.LoadPluginDirectory(Directory))
                PluginCollection.Add(plugin);
        }
        #endregion
    }
}

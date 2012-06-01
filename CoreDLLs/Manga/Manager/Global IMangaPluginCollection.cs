using System;
using Manga.Plugin;

namespace Manga.Manager
{
    public sealed class Global_IMangaPluginCollection : BakaBox.MVVM.ModelBase
    {
        #region Instance
        private static Global_IMangaPluginCollection _Instance;
        private static Object SyncObj = new Object();
        public static Global_IMangaPluginCollection Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (SyncObj)
                    {
                        if (_Instance == null)
                        { _Instance = new Global_IMangaPluginCollection(); }
                    }
                }
                return _Instance;
            }
        }

        private Global_IMangaPluginCollection()
        { }
        #endregion

        #region Collection
        private IMangaPluginCollection _Plugins;
        public IMangaPluginCollection Plugins
        {
            get
            {
                if (_Plugins == null)
                    _Plugins = new IMangaPluginCollection();
                return _Plugins;
            }
        }
        #endregion

        #region Public Members
        public void AddPlugins(params IMangaPlugin[] Plugins)
        {
            OnPropertyChanging("Plugins");
            this.Plugins.AddRange(Plugins);
            OnPropertyChanged("Plugins");
        }

        public void RemovePlugins(params IMangaPlugin[] Plugins)
        {
            OnPropertyChanging("Plugins");
            foreach (IMangaPlugin Plugin in Plugins)
                this.Plugins.Remove(Plugin);
            OnPropertyChanged("Plugins");
        }

        public void RemoveRangePlugins(Int32 Start, Int32 Count)
        {
            if ((Start + Count) >= Plugins.Count)
                throw new Exception("Range extends beyond collection.");

            OnPropertyChanging("Plugins");
            this.Plugins.RemoveRange(Start, Count);
            OnPropertyChanged("Plugins");
        }

        public void RemoveAtPlugins(params Int32[] PluginsAt)
        {
            OnPropertyChanging("Plugins");
            foreach (Int32 Index in PluginsAt)
                this.Plugins.RemoveAt(Index);
            OnPropertyChanged("Plugins");
        }
        #endregion
    }
}

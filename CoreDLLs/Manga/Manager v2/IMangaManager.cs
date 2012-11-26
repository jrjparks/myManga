using System;
using System.Collections.Generic;
using BakaBox.DLL;
using BakaBox.MVVM;
using Manga.Plugin;

namespace Manga.Manager_v2
{
    public class IMangaManager : ModelBase
    {
        #region Vars
        private Dictionary<Guid, String> _zdz;
        private Dictionary<Guid, String> zdz
        {
            get
            {
                if (_zdz == null)
                    _zdz = new Dictionary<Guid, String>();
                return _zdz;
            }
        }
        #endregion

        #region IMangaManager
        public IMangaManager()
        {
            Downloader.DownloadComplete += Downloader_DownloadComplete;
        }
        #endregion

        #region Downloader
        private Downloader _Downloader;
        private Downloader Downloader
        {
            get
            {
                if (_Downloader == null)
                    _Downloader = new Downloader();
                return _Downloader;
            }
        }

        void Downloader_DownloadComplete(object sender, System.IO.FileInfo e)
        {
            ZipManager.SaveFileToZip("", e.ToString());
        }

        public void Download(String URI, String ZipFile)
        {
            zdz.Add(Downloader.Download(URI, "", Plugins.PluginToUse_SiteUrl(URI)), ZipFile);
        }
        #endregion

        #region Zip
        private Zip_v2.Zip_v2 _ZipManager;
        private Zip_v2.Zip_v2 ZipManager
        {
            get
            {
                if (_ZipManager == null)
                    _ZipManager = new Zip_v2.Zip_v2();
                return _ZipManager;
            }
        }
        #endregion

        #region Plugins
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

        public void LoadPlugins(String folderPath)
        { AddPlugins(PluginLoader<IMangaPlugin>.LoadPluginDirectory(folderPath, "*.manga.dll")); }

        public void AddPlugins(params IMangaPlugin[] Plugins)
        {
            OnPropertyChanging("Plugins");
            this.Plugins.AddRange(Plugins);
            OnPropertyChanged("Plugins");
        }
        #endregion

    }
}

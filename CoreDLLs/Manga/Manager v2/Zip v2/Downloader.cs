using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BakaBox.Controls.Threading;
using System.IO;
using Manga.Plugin;
using BakaBox.Controls;
using System.Text.RegularExpressions;

namespace Manga.Manager_v2
{
    public class Downloader
    {
        #region Events

        public event EventHandler<FileInfo> DownloadComplete;
        private void OnDownloadComplete(FileInfo fi)
        {
            if (DownloadComplete != null)
                DownloadComplete(this, fi);
        }

        #endregion

        #region Properties

        private readonly QueuedBackgroundWorker<DownloadData> _qbc;

        #endregion

        #region Constructer

        public Downloader()
        {
            _qbc = new QueuedBackgroundWorker<DownloadData>();
            _qbc.WorkerReportsProgress = _qbc.WorkerSupportsCancellation = true;
            _qbc.DoTaskWork += _qbc_DoTaskWork;
            _qbc.TaskComplete += _qbc_TaskComplete;
        }
        #endregion

        #region Methods

        public Guid Download(String _address, String _localPath, IMangaPlugin _plugin)
        {
            DownloadData _dd = new DownloadData(_address, _localPath, _plugin.SiteRefererHeader);
            return _qbc.AddToQueue(_dd);
        }

        void _qbc_DoTaskWork(object Sender, QueuedTask<DownloadData> Task)
        {
            using (WebClient _wc = new WebClient())
            {
                #region Random
                MatchCollection RandomNumbers = Regex.Matches(Task.Data.Address, @"\[R(\d+)-(\d+)\]");
                Random r = new Random();
                foreach (Match rNumberMatch in RandomNumbers)
                    Task.Data.Address = Task.Data.Address.Replace(rNumberMatch.Value, r.Next(Int32.Parse(rNumberMatch.Groups[1].Value), Int32.Parse(rNumberMatch.Groups[2].Value)).ToString());
                #endregion

                _wc.Headers.Clear();
                if (!Task.Data.RefererHeader.Equals(String.Empty))
                    _wc.Headers.Add(System.Net.HttpRequestHeader.Referer, Task.Data.RefererHeader);

                _wc.DownloadFile(Task.Data.Address, Task.Data.LocalPath);
            }
        }

        void _qbc_TaskComplete(object Sender, QueuedTask<DownloadData> Task)
        {
            FileInfo fi = new FileInfo(Task.Data.LocalPath);
            OnDownloadComplete(fi);
        }

        #endregion
    }

    public class DownloadData
    {
        private String _address;
        private String _localPath;
        private String _refererHeader;

        public String Address
        {
            get { return _address; }
            set { _address = value; }
        }

        public String LocalPath
        {
            get { return _localPath; }
            set { _localPath = value; }
        }

        public String RefererHeader
        {
            get { return _refererHeader; }
        }

        public DownloadData(String Address, String LocalPath, String RefererHeader)
        {
            _address = Address;
            _localPath = LocalPath;
            _refererHeader = RefererHeader;
        }
    }
}

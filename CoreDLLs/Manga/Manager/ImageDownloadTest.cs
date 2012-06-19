using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Manga.Archive;
using BakaBox.Controls;
using System.IO;
using BakaBox.IO;
using Manga.Zip;
using BakaBox;

namespace Manga.Manager
{
    internal sealed class ImageDownloadTest
    {
        #region Instance
        private static ImageDownloadTest _Instance;
        private static Object SyncObj = new Object();
        public static ImageDownloadTest Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (SyncObj)
                    {
                        if (_Instance == null)
                        { _Instance = new ImageDownloadTest(); }
                    }
                }
                return _Instance;
            }
        }

        private ImageDownloadTest()
        { }
        #endregion

        #region DownloadImages
        public void DownloadImages(LocationInfoCollection LocationInfoCollection, MangaArchiveInfo MangaArchiveInfo, String SiteRefererHeader)
        {
            Task[] DownloadTasks = LocationInfoCollection.Select(i => Task.Factory.StartNew(() =>
                {
                    using (WebClient WebClient = new WebClient())
                    {
                        String RemotePath = i.FullOnlinePath,
                            LocalPath = Path.Combine(MangaArchiveInfo.TempPath().SafeFolder(), Path.GetFileName(RemotePath).SafeFileName());
                        UInt32 FileSize;
                        FileInfo LocalFile = new FileInfo(LocalPath);
                        if (LocalFile.Exists)
                            LocalFile.Delete();
                        
                        WebClient.Headers.Clear();
                        WebClient.Headers.Add(System.Net.HttpRequestHeader.Referer, SiteRefererHeader);
                        WebClient.DownloadFile(RemotePath, LocalPath);
                        
                        FileSize = Parse.TryParse<UInt32>(WebClient.ResponseHeaders[System.Net.HttpResponseHeader.ContentLength].ToString(), 0);
                        
                        LocalFile = new FileInfo(LocalPath);
                        if (!LocalFile.Exists)
                            throw new Exception("File not downloaded.");

                        else if (LocalFile.Length < FileSize)
                        {
                            LocalFile.Delete();
                            throw new Exception("File not completely downloaded.");
                        }
                    }
                })).ToArray();
            Task.WaitAll(DownloadTasks);
        }
        #endregion
    }
}

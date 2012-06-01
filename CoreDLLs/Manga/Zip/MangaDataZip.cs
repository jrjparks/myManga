using System;
using System.IO;
using System.Text;
using Ionic.Zip;
using Manga.Archive;
using Manga.Info;
using BakaBox.IO.XML;
using System.Diagnostics;
using BakaBox.IO;

namespace Manga.Zip
{
    [DebuggerStepThrough]
    public sealed class MangaDataZip : NotifyPropChangeBase, IDisposable
    {
        #region Instance
        private static MangaDataZip _Instance;
        private static Object SyncObj = new Object();
        public static MangaDataZip Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (SyncObj)
                    {
                        if (_Instance == null)
                        { _Instance = new MangaDataZip(); }
                    }
                }
                return _Instance;
            }
        }
        #endregion

        #region Events
        public delegate void ProgressChange(Object Sender, Int32 Progress, FileType FileType, Object Data);
        public event ProgressChange ProgressChanged;
        private void OnProgressChanged(Int32 Progress, FileType FileType)
        {
            if (ProgressChanged != null)
                ProgressChanged(this, Progress, FileType, default(MangaArchiveInfo));
        }
        private void OnProgressChanged(Int32 Progress, FileType FileType, Object Data)
        {
            if (ProgressChanged != null)
                ProgressChanged(this, Progress, FileType, Data);
        }

        public delegate void MangaInfoUpdate(Object Sender, MangaInfo MangaInfo, String FullFilePath);
        public event MangaInfoUpdate MangaInfoUpdated;
        private void OnMangaInfoUpdated(MangaInfo MangaInfo, String FullFilePath)
        {
            if (MangaInfoUpdated != null)
                MangaInfoUpdated(this, MangaInfo, FullFilePath);
        }

        public delegate void MangaArchiveUpdate(Object Sender, MangaArchiveInfo MangaArchiveInfo, String FullFilePath);
        public event MangaArchiveUpdate MangaArchiveUpdated;
        private void OnMangaArchiveUpdated(MangaArchiveInfo MangaArchiveInfo, String FullFilePath)
        {
            if (MangaArchiveUpdated != null)
                MangaArchiveUpdated(this, MangaArchiveInfo, FullFilePath);
        }
        #endregion

        #region Variables
        public enum FileType
        {
            MIZA,
            MZA
        }

        private String _SaveLocation { get; set; }
        public String SaveLocation
        {
            get { return _SaveLocation; }
            set { _SaveLocation = value; OnPropertyChanged("SaveLocation"); }
        }

        public static String DefaultSaveLocation { get { return Environment.CurrentDirectory; } }
        #endregion

        #region Constructor
        private MangaDataZip()
        { SaveLocation = DefaultSaveLocation; }

        public String CreateSavePath(String FileName)
        { return Path.Combine(SaveLocation, FileName); }
        #endregion

        #region Disposable
        public void Dispose()
        {

        }
        #endregion

        #region MangaInfo
        public String MIZAPath
        { get { return Path.Combine(SaveLocation, "MangaInfo"); } }
        public String CreateMIZAPath(MangaInfo MangaInfo)
        { return Path.Combine(MIZAPath, MangaInfo.MISaveName.SafeFileName()); }

        public String MIZA(MangaInfo MangaInfo, CoverData CoverData)
        { return MIZA(MangaInfo, CoverData, CreateMIZAPath(MangaInfo)); }
        public String MIZA(MangaInfo MangaInfo, CoverData CoverData, String MIZA_Path)
        {
            OnProgressChanged(1, FileType.MIZA);
            using (ZipFile zipFile = new ZipFile(Encoding.UTF8))
            {
                OnProgressChanged(5, FileType.MIZA);
                zipFile.Comment = MangaInfo.Name;
                zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                zipFile.CompressionMethod = CompressionMethod.Deflate;
                zipFile.UseZip64WhenSaving = Zip64Option.AsNecessary;
                OnProgressChanged(5, FileType.MIZA);
                if (CoverData != null && CoverData.CoverStream != null)
                {
                    CoverData.CoverStream.Position = 0;
                    if (zipFile.ContainsEntry(MangaInfoData.CoverName))
                        zipFile.UpdateEntry(MangaInfoData.CoverName, CoverData.CoverStream);
                    else
                        zipFile.AddEntry(MangaInfoData.CoverName, CoverData.CoverStream);
                }
                OnProgressChanged(25, FileType.MIZA);

                if (zipFile.ContainsEntry(MangaInfoData.InfoFileName))
                    zipFile.UpdateEntry(MangaInfoData.InfoFileName, MangaInfo.SerializeObject());
                else
                    zipFile.AddEntry(MangaInfoData.InfoFileName, MangaInfo.SerializeObject());

                OnProgressChanged(50, FileType.MIZA);

                zipFile.SaveProgress += (s, e) =>
                {
                    switch (e.EventType)
                    {
                        default: break;

                        case ZipProgressEventType.Saving_AfterWriteEntry:
                            OnProgressChanged(50 + (Int32)(((Double)e.EntriesSaved / (e.EntriesTotal != 0 ? (Double)e.EntriesTotal : 1D) * 100)),
                                FileType.MIZA);
                            break;
                    }
                };

                Path.GetDirectoryName(MIZA_Path).SafeFolder();
                zipFile.Save(MIZA_Path);
                CoverData.CoverStream.Close();
            }
            OnProgressChanged(100, FileType.MIZA);
            OnMangaInfoUpdated(MangaInfo, MIZA_Path);
            return MIZA_Path;
        }

        #region Write
        public String WriteMIZA(MangaInfo MangaInfo, CoverData CoverData)
        { return WriteMIZA(MangaInfo, CoverData, CreateMIZAPath(MangaInfo)); }
        public String WriteMIZA(MangaInfo MangaInfo, CoverData CoverData, String MIZA_Path)
        {
            OnProgressChanged(1, FileType.MIZA);
            using (ZipFile zipFile = new ZipFile(Encoding.UTF8))
            {
                OnProgressChanged(5, FileType.MIZA);
                zipFile.Comment = MangaInfo.Name;
                zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                zipFile.CompressionMethod = CompressionMethod.Deflate;
                zipFile.UseZip64WhenSaving = Zip64Option.AsNecessary;
                OnProgressChanged(5, FileType.MIZA);
                if (CoverData != null && CoverData.CoverStream != null)
                {
                    CoverData.CoverStream.Position = 0;
                    zipFile.AddEntry(MangaInfoData.CoverName, CoverData.CoverStream);
                }
                OnProgressChanged(25, FileType.MIZA);

                zipFile.AddEntry(MangaInfoData.InfoFileName, MangaInfo.SerializeObject());
                OnProgressChanged(50, FileType.MIZA);

                zipFile.SaveProgress += (s, e) =>
                    {
                        switch (e.EventType)
                        {
                            default: break;

                            case ZipProgressEventType.Saving_AfterWriteEntry:
                                OnProgressChanged(50 + (Int32)(((Double)e.EntriesSaved / (e.EntriesTotal != 0 ? (Double)e.EntriesTotal : 1D) * 100)),
                                    FileType.MIZA);
                                break;
                        }
                    };

                Path.GetDirectoryName(MIZA_Path).SafeFolder();
                zipFile.Save(MIZA_Path);
                CoverData.CoverStream.Close();
            }
            OnProgressChanged(100, FileType.MIZA);
            OnMangaInfoUpdated(MangaInfo, MIZA_Path);
            return MIZA_Path;
        }
        #endregion

        #region Update
        public String UpdateMIZA(MangaInfo MangaInfo)
        { return UpdateMIZA(MangaInfo, null, CreateMIZAPath(MangaInfo)); }
        public String UpdateMIZA(MangaInfo MangaInfo, String MIZA_Path)
        { return UpdateMIZA(MangaInfo, null, MIZA_Path); }
        public String UpdateMIZA(MangaInfo MangaInfo, CoverData CoverData)
        { return UpdateMIZA(MangaInfo, CoverData, CreateMIZAPath(MangaInfo)); }
        public String UpdateMIZA(MangaInfo MangaInfo, CoverData CoverData, String MIZA_Path)
        {
            OnProgressChanged(1, FileType.MIZA);
            using (ZipFile zipFile = ZipFile.Read(MIZA_Path, new ReadOptions() { Encoding = Encoding.UTF8 }))
            {
                OnProgressChanged(2, FileType.MIZA);
                zipFile.Comment = MangaInfo.Name;
                zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                zipFile.CompressionMethod = CompressionMethod.Deflate;
                OnProgressChanged(5, FileType.MIZA);
                if (CoverData != null && CoverData.CoverStream != null)
                {
                    if (zipFile.ContainsEntry(MangaInfoData.CoverName))
                        zipFile.UpdateEntry(CoverData.CoverName, CoverData.CoverStream);
                    else
                        zipFile.AddEntry(CoverData.CoverName, CoverData.CoverStream);
                }
                OnProgressChanged(25, FileType.MIZA);

                if (zipFile.ContainsEntry(MangaInfoData.InfoFileName))
                    zipFile.UpdateEntry(MangaInfoData.InfoFileName, MangaInfo.SerializeObject());
                else
                    zipFile.AddEntry(MangaInfoData.InfoFileName, MangaInfo.SerializeObject());
                OnProgressChanged(50, FileType.MIZA);
               
                zipFile.SaveProgress += (s, e) =>
                {
                    switch (e.EventType)
                    {
                        default: break;

                        case ZipProgressEventType.Saving_AfterWriteEntry:
                            OnProgressChanged(50 + (Int32)(((Double)e.EntriesSaved / (e.EntriesTotal != 0 ? (Double)e.EntriesTotal : 1D) * 100)),
                                FileType.MIZA);
                            break;
                    }
                };

                zipFile.Save();
            }
            OnProgressChanged(100, FileType.MIZA);
            OnMangaInfoUpdated(MangaInfo, MIZA_Path);
            return MIZA_Path;
        }
        #endregion

        #region Read
        public Stream CoverStream(String MIZA_Path)
        { return GetCoverStream(MIZA_Path); }
        public static Stream GetCoverStream(String MIZA_Path)
        {
            MemoryStream _Cover = new MemoryStream();

            if (File.Exists(MIZA_Path))
            {
                using (ZipFile zipFile = ZipFile.Read(MIZA_Path, new ReadOptions() { Encoding = Encoding.UTF8 }))
                {
                    if (zipFile.ContainsEntry(MangaInfoData.CoverName))
                    {
                        Stream _Data = new MemoryStream();
                        zipFile[MangaInfoData.CoverName].Extract(_Data);
                        _Data.Position = 0;
                        _Data.CopyTo(_Cover);
                        _Cover.Position = 0;
                        _Data.Close();
                    }
                }
            }
            _Cover.Position = 0;
            return _Cover;
        }

        public MangaInfo MangaInfo(String MIZA_Path)
        { return GetMangaInfo(MIZA_Path); }
        public static MangaInfo GetMangaInfo(String MIZA_Path)
        {
            MangaInfo _mInfo = null;

            if (File.Exists(MIZA_Path))
            {
                using (ZipFile zipFile = ZipFile.Read(MIZA_Path, new ReadOptions() { Encoding = Encoding.UTF8 }))
                {
                    if (zipFile.ContainsEntry(MangaInfoData.InfoFileName))
                    {
                        Stream _Data = new MemoryStream();
                        zipFile[MangaInfoData.InfoFileName].Extract(_Data);
                        _Data.Position = 0;
                        _mInfo = _Data.DeserializeStream<MangaInfo>();
                        _Data.Close();
                    }
                }
            }

            return _mInfo;
        }
        #endregion

        #endregion

        #region MangaArchive
        public String MZAPath
        { get { return Path.Combine(SaveLocation, "MangaArchives"); } }
        public String CreateMZAPath(MangaArchiveInfo MangaArchiveInfo)
        { return Path.Combine(MZAPath, MangaArchiveInfo.Name.SafeFileName(), MangaArchiveInfo.MAISaveName.SafeFileName()); }

        #region Write
        public String WriteMZA(MangaArchiveInfo MangaArchiveInfo)
        { return WriteMZA(MangaArchiveInfo, CreateMZAPath(MangaArchiveInfo)); }
        public String WriteMZA(MangaArchiveInfo MangaArchiveInfo, String MZA_Path)
        {
            if (!File.Exists(MZA_Path))
            {
                Double Progress = 0, Step = 0;
                OnProgressChanged((Int32)Math.Round(++Progress), FileType.MZA);

                using (ZipFile zipFile = new ZipFile(Encoding.UTF8))
                {
                    zipFile.Comment = MangaArchiveInfo.Name;
                    zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                    zipFile.CompressionMethod = CompressionMethod.Deflate;
                    zipFile.UseZip64WhenSaving = Zip64Option.AsNecessary;

                    zipFile.AddEntry(MangaArchiveData.InfoFileName, MangaArchiveInfo.SerializeObject());

                    OnProgressChanged((Int32)Math.Round(++Progress), FileType.MZA);
                    Step = (49D - Progress) / MangaArchiveInfo.PageEntries.Count;
                    
                    String _PagePath;
                    foreach (PageEntry Page in MangaArchiveInfo.PageEntries)
                    {
                        _PagePath = MangaArchiveInfo.GetTmpPagePath(Page.PageNumber);
                        if (File.Exists(_PagePath))
                            zipFile.AddFile(_PagePath, ".");

                        OnProgressChanged((Int32)Math.Round(Progress += Step), FileType.MZA);
                    }

                    zipFile.SaveProgress += (s, e) =>
                    {
                        switch (e.EventType)
                        {
                            default: break;

                            case ZipProgressEventType.Saving_AfterWriteEntry:
                                OnProgressChanged(50 + (Int32)(((Double)e.EntriesSaved / (e.EntriesTotal != 0 ? (Double)e.EntriesTotal : 1D) * 100)),
                                    FileType.MZA);
                                break;
                        }
                    };

                    Path.GetDirectoryName(MZA_Path).SafeFolder();
                    zipFile.Save(MZA_Path);
                }
                CleanUnusedFolders(MangaArchiveInfo, MangaArchiveInfo.TmpFolderLocation);
                OnProgressChanged(100, FileType.MZA);
                OnMangaArchiveUpdated(MangaArchiveInfo, MZA_Path);
                return MZA_Path;
            }
            else
            {
                return UpdateMZA(MangaArchiveInfo, MZA_Path);
            }
        }

        public String InitWriteMZA(MangaArchiveInfo MangaArchiveInfo)
        { return InitWriteMZA(MangaArchiveInfo, CreateMZAPath(MangaArchiveInfo)); }
        public String InitWriteMZA(MangaArchiveInfo MangaArchiveInfo, String MZA_Path)
        {
            Double Progress = 0;
            OnProgressChanged((Int32)Math.Round(++Progress), FileType.MZA);

            using (ZipFile zipFile = new ZipFile(Encoding.UTF8))
            {
                zipFile.Comment = MangaArchiveInfo.Name;
                zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                zipFile.CompressionMethod = CompressionMethod.Deflate;
                zipFile.UseZip64WhenSaving = Zip64Option.AsNecessary;

                zipFile.AddEntry(MangaArchiveData.InfoFileName, MangaArchiveInfo.SerializeObject());

                OnProgressChanged((Int32)(Progress = 50), FileType.MZA);
                
                zipFile.SaveProgress += (s, e) =>
                {
                    switch (e.EventType)
                    {
                        default: break;

                        case ZipProgressEventType.Saving_AfterWriteEntry:
                            OnProgressChanged(50 + (Int32)(((Double)e.EntriesSaved / (e.EntriesTotal != 0 ? (Double)e.EntriesTotal : 1D) * 100)),
                                FileType.MZA);
                            break;
                    }
                };

                Path.GetDirectoryName(MZA_Path).SafeFolder();
                zipFile.Save(MZA_Path);
            }
            CleanUnusedFolders(MangaArchiveInfo, MangaArchiveInfo.TmpFolderLocation);
            OnProgressChanged(100, FileType.MZA);
            OnMangaArchiveUpdated(MangaArchiveInfo, MZA_Path);
            return MZA_Path;
        }
        #endregion

        #region Update
        public String UpdateMZA(MangaArchiveInfo MangaArchiveInfo)
        { return UpdateMZA(MangaArchiveInfo, CreateMZAPath(MangaArchiveInfo)); }
        public String UpdateMZA(MangaArchiveInfo MangaArchiveInfo, String MZA_Path)
        {
            if (File.Exists(MZA_Path))
            {
                Double Progress = 0, Step = 0;
                OnProgressChanged((Int32)Math.Round(++Progress), FileType.MZA);

                using (ZipFile zipFile = ZipFile.Read(MZA_Path, new ReadOptions() { Encoding = Encoding.UTF8 }))
                {
                    zipFile.Comment = MangaArchiveInfo.Name;
                    zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                    zipFile.CompressionMethod = CompressionMethod.Deflate;
                    zipFile.UseZip64WhenSaving = Zip64Option.AsNecessary;

                    if (zipFile.ContainsEntry(MangaArchiveData.InfoFileName))
                        zipFile.UpdateEntry(MangaArchiveData.InfoFileName, MangaArchiveInfo.SerializeObject());
                    else
                        zipFile.AddEntry(MangaArchiveData.InfoFileName, MangaArchiveInfo.SerializeObject());

                    OnProgressChanged((Int32)Math.Round(++Progress), FileType.MZA);
                    Step = (49D - Progress) / MangaArchiveInfo.PageEntries.Count;

                    String _PagePath;
                    foreach (PageEntry Page in MangaArchiveInfo.PageEntries)
                    {
                        _PagePath = MangaArchiveInfo.GetTmpPagePath(Page.PageNumber);
                        if (File.Exists(_PagePath))
                        {
                            if (zipFile.ContainsEntry(MangaArchiveData.InfoFileName))
                                zipFile.UpdateFile(_PagePath);
                            else
                                zipFile.AddFile(_PagePath);
                        }
                        OnProgressChanged((Int32)Math.Round(Progress += Step), FileType.MZA);
                    }

                    zipFile.SaveProgress += (s, e) =>
                    {
                        switch (e.EventType)
                        {
                            default: break;

                            case ZipProgressEventType.Saving_AfterWriteEntry:
                                OnProgressChanged(50 + (Int32)(((Double)e.EntriesSaved / (e.EntriesTotal != 0 ? (Double)e.EntriesTotal : 1D) * 100)),
                                    FileType.MZA);
                                break;
                        }
                    };
                    zipFile.Save();
                }
                OnProgressChanged(100, FileType.MZA);
                OnMangaArchiveUpdated(MangaArchiveInfo, MZA_Path);
                return MZA_Path;
            }
            else
            {
                return WriteMZA(MangaArchiveInfo, MZA_Path);
            }
        }

        public String UpdateMZA(MangaArchiveInfo MangaArchiveInfo, params String[] Files)
        { return UpdateMZA(MangaArchiveInfo, CreateMZAPath(MangaArchiveInfo), Files); }
        public String UpdateMZA(MangaArchiveInfo MangaArchiveInfo, String MZA_Path, params String[] Files)
        {
            return "";
        }
        #endregion

        #region Read
        public Stream PageStream(UInt32 PageNumber, MangaArchiveInfo MangaArchiveInfo, String MZA_Path)
        {
            MemoryStream _Page = new MemoryStream();

            if (File.Exists(MZA_Path))
            {
                using (ZipFile zipFile = ZipFile.Read(MZA_Path, new ReadOptions() { Encoding = Encoding.UTF8 }))
                {
                    if (MangaArchiveInfo.PageEntries.Contains(PageNumber))
                    {
                        String imgName = MangaArchiveInfo.GetPageName(PageNumber);
                        if (zipFile.ContainsEntry(imgName))
                        {
                            Stream _Data = new MemoryStream();
                            zipFile[imgName].Extract(_Data);
                            _Data.Position = 0;
                            _Data.CopyTo(_Page);
                            _Data.Close();
                        }
                    }
                }
            }
            _Page.Position = 0;
            return _Page;
        }
        public Stream PageStream(UInt32 PageNumber, String MZA_Path)
        { return GetPageStream(PageNumber, MZA_Path); }
        public static Stream GetPageStream(UInt32 PageNumber, String MZA_Path)
        {
            MemoryStream _Page = new MemoryStream();

            if (File.Exists(MZA_Path))
            {
                using (ZipFile zipFile = ZipFile.Read(MZA_Path, new ReadOptions() { Encoding = Encoding.UTF8 }))
                {
                    MangaArchiveInfo _MangaArchiveInfo;
                    if (zipFile.ContainsEntry(MangaArchiveData.InfoFileName))
                    {
                        Stream _Data = new MemoryStream();
                        zipFile[MangaArchiveData.InfoFileName].Extract(_Data);
                        _Data.Position = 0;
                        _MangaArchiveInfo = _Data.DeserializeStream<MangaArchiveInfo>();
                        _Data.Close();
                        if (_MangaArchiveInfo.PageEntries.Contains(PageNumber))
                        {
                            String imgName = _MangaArchiveInfo.GetPageName(PageNumber);
                            if (zipFile.ContainsEntry(imgName))
                            {
                                _Data = new MemoryStream();
                                zipFile[imgName].Extract(_Data);
                                _Data.Position = 0;
                                _Data.CopyTo(_Page);
                                _Data.Close();
                            }
                        }
                    }
                    _MangaArchiveInfo = null;
                }
            }
            _Page.Position = 0;
            return _Page;
        }

        public MangaArchiveInfo MangaArchiveInfo(String MZA_Path)
        { return GetMangaArchiveInfo(MZA_Path); }
        public static MangaArchiveInfo GetMangaArchiveInfo(String MZA_Path)
        {
            MangaArchiveInfo _mArchiveInfo = null;

            if (File.Exists(MZA_Path))
            {
                using (ZipFile zipFile = ZipFile.Read(MZA_Path, new ReadOptions() { Encoding = Encoding.UTF8 }))
                {
                    if (zipFile.ContainsEntry(MangaArchiveData.InfoFileName))
                    {
                        Stream _Data = new MemoryStream();
                        zipFile[MangaArchiveData.InfoFileName].Extract(_Data);
                        _Data.Position = 0;
                        _mArchiveInfo = _Data.DeserializeStream<MangaArchiveInfo>();
                        _Data.Close();
                    }
                }
            }

            return _mArchiveInfo;
        }
        #endregion

        #endregion

        #region Static Methods

        /// <summary>
        /// Cleans the directory where a chapter was stored.
        /// </summary>
        /// <param name="MangaArchiveInfo">The Manga Archive Info</param>
        /// <param name="SavePath">The higher path</param>
        public static void CleanUnusedFolders(MangaArchiveInfo MangaArchiveInfo, String SavePath)
        {
            Boolean Retried;
            DirectoryInfo DirectoryPath = new DirectoryInfo(SavePath);
            while (DirectoryPath.GetDirectories().Length == 0)
            {
                Retried = false;
            retry:
                try
                {
                    DirectoryPath.Delete(true);
                }
                catch
                {
                    if (!Retried)
                    {
                        System.Threading.Thread.Sleep(500);
                        Retried = true;
                        goto retry;
                    }
                }
                DirectoryPath = DirectoryPath.Parent;
            }
        }

        /// <summary>
        /// Return the Stream of a file within a zip file.
        /// </summary>
        /// <param name="FileName">File name within the zip.</param>
        /// <param name="ZipPath">Location of the zip file.</param>
        /// <returns></returns>
        public static Stream GetFileStream(String FileName, String ZipPath)
        {
            Stream _FileStream = new MemoryStream();
            if (File.Exists(ZipPath))
            {
                using (ZipFile zipFile = ZipFile.Read(ZipPath, new ReadOptions() { Encoding = Encoding.UTF8 }))
                {
                    if (zipFile.ContainsEntry(FileName))
                    {
                        Stream _Data = new MemoryStream();
                        zipFile[FileName].Extract(_Data);
                        _Data.Position = 0;
                        _Data.CopyTo(_FileStream);
                        _Data.Close();
                    }
                }
            }
            _FileStream.Position = 0;
            return _FileStream;
        }
        #endregion
    }
}

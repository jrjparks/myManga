using System;
using System.IO;
using System.Text;
using Ionic.Zip;
using Manga.Archive;
using Manga.Info;
using BakaBox.IO.XML;
using System.Diagnostics;
using BakaBox.IO;
using System.ComponentModel;

namespace Manga.Zip
{
    //[DebuggerStepThrough]
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
        public event ProgressChangedEventHandler MangaInfoUpdateChanged;
        private void OnMangaInfoUpdateChanged(Int32 Progress)
        { OnMangaInfoUpdateChanged(Progress, null); }
        private void OnMangaInfoUpdateChanged(Int32 Progress, Object UserState)
        {
            if (MangaInfoUpdateChanged != null)
                MangaInfoUpdateChanged(this, new ProgressChangedEventArgs(Progress, UserState));
        }

        public event ProgressChangedEventHandler MangaArchiveUpdateChanged;
        private void OnMangaArchiveUpdateChanged(Int32 Progress)
        { OnMangaArchiveUpdateChanged(Progress, null); }
        private void OnMangaArchiveUpdateChanged(Int32 Progress, Object UserState)
        {
            if (MangaArchiveUpdateChanged != null)
                MangaArchiveUpdateChanged(this, new ProgressChangedEventArgs(Progress, UserState));
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
        public static String DefaultSaveLocation
        { get { return Environment.CurrentDirectory; } }
        private String _SaveLocation { get; set; }
        public String SaveLocation
        {
            get { return _SaveLocation; }
            set { _SaveLocation = value; OnPropertyChanged("SaveLocation"); }
        }

        private ReadOptions _ZipReadOptions;
        private ReadOptions ZipReadOptions
        {
            get
            {
                if (_ZipReadOptions == null)
                {
                    _ZipReadOptions = new ReadOptions();
                    _ZipReadOptions.Encoding = Encoding.UTF8;
                }
                return _ZipReadOptions;
            }
        }
        #endregion

        #region Constructor
        private MangaDataZip()
        { SaveLocation = DefaultSaveLocation; }
        #endregion

        #region Disposable
        public void Dispose()
        {

        }
        #endregion

        #region Members
        private void MangaInfoZipSaveProgress(Object s, SaveProgressEventArgs e)
        {
            switch (e.EventType)
            {
                default: break;

                case ZipProgressEventType.Saving_AfterWriteEntry:
                    OnMangaInfoUpdateChanged(50 + (Int32)(((Double)e.EntriesSaved / (e.EntriesTotal != 0 ? (Double)e.EntriesTotal : 1D) * 50)));
                    break;
            }
        }
        private void MangaArchiveZipSaveProgress(Object s, SaveProgressEventArgs e)
        {
            switch (e.EventType)
            {
                default: break;

                case ZipProgressEventType.Saving_AfterWriteEntry:
                    OnMangaArchiveUpdateChanged(50 + (Int32)(((Double)e.EntriesSaved / (e.EntriesTotal != 0 ? (Double)e.EntriesTotal : 1D) * 50)));
                    break;
            }
        }
        #endregion

        #region MangaInfo
        public String MIZAPath
        { get { return Path.Combine(SaveLocation, "MangaInfo"); } }
        public String CreateMIZAPath(MangaInfo MangaInfo)
        { return Path.Combine(MIZAPath, MangaInfo.MangaDataName()); }

        #region Write
        public String MIZA(MangaInfo MangaInfo)
        { return MIZA(MangaInfo, null, MIZAPath, MangaInfo.MangaDataName()); }
        public String MIZA(MangaInfo MangaInfo, CoverData CoverData)
        { return MIZA(MangaInfo, CoverData, MIZAPath, MangaInfo.MangaDataName()); }
        public String MIZA(MangaInfo MangaInfo, CoverData CoverData, String DestFolder, String FileName)
        {
            OnMangaInfoUpdateChanged(1);
            String DestFilePath = Path.Combine(DestFolder, FileName);
            Boolean FileExists = File.Exists(DestFilePath),
                CoverDataExists = (CoverData != null && CoverData.CoverStream != null && CoverData.CoverStream.Length > 0);
            using (ZipFile zipFile = FileExists ? ZipFile.Read(DestFilePath, ZipReadOptions) : new ZipFile(Encoding.UTF8))
            {
                OnMangaInfoUpdateChanged(5);
                zipFile.Comment = MangaInfo.Name;
                zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                zipFile.CompressionMethod = CompressionMethod.Deflate;
                zipFile.UseZip64WhenSaving = Zip64Option.AsNecessary;
                OnMangaInfoUpdateChanged(10);
                if (CoverDataExists)
                {
                    CoverData.CoverStream.Position = 0;
                    zipFile.UpdateEntry(MangaInfoData.CoverName, CoverData.CoverStream);
                }
                OnMangaInfoUpdateChanged(25);

                zipFile.UpdateEntry(MangaInfoData.InfoFileName, MangaInfo.SerializeObject());

                OnMangaInfoUpdateChanged(50);

                zipFile.SaveProgress += MangaInfoZipSaveProgress;

                Path.GetDirectoryName(DestFolder).SafeFolder();
                if (FileExists)
                    zipFile.Save();
                else
                    zipFile.CloudSafeSave(MangaInfo.TempPath(), DestFolder, MangaInfo.MangaDataName());

                zipFile.SaveProgress -= MangaInfoZipSaveProgress;

                if (CoverDataExists)
                    CoverData.CoverStream.Close();
            }
            OnMangaInfoUpdateChanged(100);
            OnMangaInfoUpdated(MangaInfo, DestFilePath);
            return DestFilePath;
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
        { return Path.Combine(MZAPath, MangaArchiveInfo.Name.SafeFileName(), MangaArchiveInfo.MangaDataName().SafeFileName()); }
        
        #region Write
        public String MZA(MangaArchiveInfo MangaArchiveInfo)
        { return MZA(MangaArchiveInfo, Path.Combine(MZAPath, MangaArchiveInfo.Name.SafeFileName()), MangaArchiveInfo.MangaDataName()); }
        public String MZA(MangaArchiveInfo MangaArchiveInfo, String DestFolder, String FileName)
        {
            Double Progress = 0, Step = 0;
            String DestFilePath = Path.Combine(DestFolder, FileName);
            Boolean FileExists = File.Exists(DestFilePath);
            OnMangaArchiveUpdateChanged((Int32)Math.Round(++Progress));

            using (ZipFile zipFile = FileExists ? ZipFile.Read(DestFilePath, ZipReadOptions) : new ZipFile(Encoding.UTF8))
            {
                zipFile.Comment = MangaArchiveInfo.Name;
                zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                zipFile.CompressionMethod = CompressionMethod.Deflate;
                zipFile.UseZip64WhenSaving = Zip64Option.AsNecessary;

                zipFile.UpdateEntry(MangaArchiveData.InfoFileName, MangaArchiveInfo.SerializeObject());

                OnMangaArchiveUpdateChanged((Int32)Math.Round(++Progress));
                Step = (49D - Progress) / MangaArchiveInfo.PageEntries.Count;

                String _PagePath;
                foreach (PageEntry Page in MangaArchiveInfo.PageEntries)
                {
                    _PagePath = MangaArchiveInfo.TempPath(Page.PageNumber);
                    if (File.Exists(_PagePath))
                        zipFile.UpdateFile(_PagePath, ".");

                    OnMangaArchiveUpdateChanged((Int32)Math.Round(Progress += Step));
                }

                zipFile.SaveProgress += MangaArchiveZipSaveProgress;

                if (FileExists)
                    zipFile.Save();
                else
                    zipFile.CloudSafeSave(MangaArchiveInfo.TempPath(), DestFolder, FileName);

                zipFile.SaveProgress -= MangaArchiveZipSaveProgress;
            }
            CleanUnusedFolders(MangaArchiveInfo, MangaArchiveInfo.TempPath());
            OnMangaArchiveUpdateChanged(100);
            OnMangaArchiveUpdated(MangaArchiveInfo, DestFilePath);
            return DestFilePath;
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
                        String imgName = MangaArchiveInfo.PageName(PageNumber);
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
                            String imgName = _MangaArchiveInfo.PageName(PageNumber);
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

using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using BakaBox.IO;
using BakaBox.IO.XML;
using Ionic.Zip;
using Manga.Archive;
using Manga.Info;
using Manga.Core;

namespace Manga.Zip
{
#if RELEASE
    [DebuggerStepThrough]
#endif
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
            set { _SaveLocation = value.SafeFolder(); OnPropertyChanged("SaveLocation"); }
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
        { SaveLocation = DefaultSaveLocation.SafeFolder(); }
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

        #region Auto
        public String AutoZip(MangaData MangaData)
        {
            if (MangaData is MangaInfo)
                return MIZA(MangaData as MangaInfo);
            else if (MangaData is MangaArchiveInfo)
                return MZA(MangaData as MangaArchiveInfo);
            else
                return String.Empty;
        }
        #endregion

        #region MangaInfo
        public String MIZAPath
        { get { return Path.Combine(SaveLocation, "MangaInfo").SafeFolder(); } }
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
                zipFile.CompressionMethod = Ionic.Zip.CompressionMethod.Deflate;
                zipFile.UseZip64WhenSaving = Zip64Option.AsNecessary;
                OnMangaInfoUpdateChanged(10);
                if (CoverDataExists)
                {
                    CoverData.CoverStream.Position = 0;
                    zipFile.UpdateEntry(MangaInfoConst.CoverName, CoverData.CoverStream);
                }
                OnMangaInfoUpdateChanged(25);

                zipFile.UpdateEntry(MangaInfoConst.InfoFileName, MangaInfo.SerializeObject());

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
        {
            MemoryStream CoverStream = new MemoryStream();

            if (File.Exists(MIZA_Path))
            {
                using (ZipFile zipFile = ZipFile.Read(MIZA_Path, ZipReadOptions))
                {
                    if (zipFile.ContainsEntry(MangaInfoConst.CoverName))
                    {
                        using (Stream CoverData = new MemoryStream())
                        {
                            zipFile[MangaInfoConst.CoverName].Extract(CoverData);
                            CoverData.Position = 0;
                            CoverData.CopyTo(CoverStream);
                            CoverStream.Position = 0;
                            CoverData.Close();
                        }
                    }
                    else
                        CoverStream = (MemoryStream)Stream.Null;
                }
            }
            CoverStream.Position = 0;
            return CoverStream;
        }

        public MangaInfo GetMangaInfo(String MIZA_Path)
        {
            MangaInfo MangaInfoInfo = null;

            if (File.Exists(MIZA_Path))
            {
                using (ZipFile zipFile = ZipFile.Read(MIZA_Path, ZipReadOptions))
                {
                    if (zipFile.ContainsEntry(MangaInfoConst.InfoFileName))
                    {
                        using (Stream MangaInfoData = new MemoryStream())
                        {
                            zipFile[MangaInfoConst.InfoFileName].Extract(MangaInfoData);
                            MangaInfoData.Position = 0;
                            MangaInfoInfo = MangaInfoData.DeserializeStream<MangaInfo>();
                            MangaInfoData.Close();
                        }
                    }
                }
            }

            return MangaInfoInfo;
        }
        #endregion

        #endregion

        #region MangaArchive
        public String MZAPath
        { get { return Path.Combine(SaveLocation, "MangaArchives").SafeFolder(); } }
        public String CreateMZAPath(MangaArchiveInfo MangaArchiveInfo)
        { return Path.Combine(MZAPath, MangaArchiveInfo.Name, MangaArchiveInfo.MangaDataName()).SafeFolder(); }
        
        #region Write
        public String MZA(MangaArchiveInfo MangaArchiveInfo)
        { return MZA(MangaArchiveInfo, Path.Combine(MZAPath, MangaArchiveInfo.Name.SafeFileName()), MangaArchiveInfo.MangaDataName()); }
        public String MZA(MangaArchiveInfo MangaArchiveInfo, String DestFolder, String FileName)
        {
            Double Progress = 0, Step = 0;
            String DestFilePath = Path.Combine(DestFolder, FileName), TempSource = MangaArchiveInfo.TempPath();
            Boolean FileExists = File.Exists(DestFilePath);
            OnMangaArchiveUpdateChanged((Int32)Math.Round(++Progress));

            using (ZipFile zipFile = FileExists ? ZipFile.Read(DestFilePath, ZipReadOptions) : new ZipFile(Encoding.UTF8))
            {
                zipFile.Comment = MangaArchiveInfo.Name;
                zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                zipFile.CompressionMethod = Ionic.Zip.CompressionMethod.Deflate;
                zipFile.UseZip64WhenSaving = Zip64Option.AsNecessary;

                zipFile.UpdateEntry(MangaArchiveConst.InfoFileName, MangaArchiveInfo.SerializeObject());

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
                    zipFile.CloudSafeSave(TempSource, DestFolder, FileName);

                zipFile.SaveProgress -= MangaArchiveZipSaveProgress;
            }
            CleanUnusedFolders(TempSource);
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
                using (ZipFile zipFile = ZipFile.Read(MZA_Path, ZipReadOptions))
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
        {
            MemoryStream _Page = new MemoryStream();

            if (File.Exists(MZA_Path))
            {
                using (ZipFile zipFile = ZipFile.Read(MZA_Path, ZipReadOptions))
                {
                    MangaArchiveInfo _MangaArchiveInfo;
                    if (zipFile.ContainsEntry(MangaArchiveConst.InfoFileName))
                    {
                        Stream _Data = new MemoryStream();
                        zipFile[MangaArchiveConst.InfoFileName].Extract(_Data);
                        _Data.Position = 0;
                        _MangaArchiveInfo = _Data.DeserializeStream<MangaArchiveInfo>();
                        _Data.Close();
                        if (_MangaArchiveInfo.PageEntries.Contains(PageNumber))
                        {
                            String imgName = _MangaArchiveInfo.PageName(PageNumber);
                            if (zipFile.ContainsEntry(imgName))
                            {
                                using (Stream StreamData = new MemoryStream())
                                {
                                    zipFile[imgName].Extract(StreamData);
                                    StreamData.Position = 0;
                                    StreamData.CopyTo(_Page);
                                    StreamData.Close();
                                }
                            }
                        }
                    }
                    _MangaArchiveInfo = null;
                }
            }
            _Page.Position = 0;
            return _Page;
        }

        public MangaArchiveInfo GetMangaArchiveInfo(String MZA_Path)
        {
            MangaArchiveInfo ArchiveInfo = null;

            if (File.Exists(MZA_Path))
            {
                using (ZipFile zipFile = ZipFile.Read(MZA_Path, ZipReadOptions))
                {
                    if (zipFile.ContainsEntry(MangaArchiveConst.InfoFileName))
                    {
                        using (Stream StreamData = new MemoryStream())
                        {
                            zipFile[MangaArchiveConst.InfoFileName].Extract(StreamData);
                            StreamData.Position = 0;
                            ArchiveInfo = StreamData.DeserializeStream<MangaArchiveInfo>();
                            StreamData.Close();
                        }
                    }
                }
            }

            return ArchiveInfo;
        }
        #endregion

        #endregion

        #region Private Methods

        /// <summary>
        /// Cleans the directory where a chapter was stored.
        /// </summary>
        /// <param name="MangaArchiveInfo">The Manga Archive Info</param>
        /// <param name="SavePath">The higher path</param>
        public static void CleanUnusedFolders(String SavePath)
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
        private Stream GetFileStream(String FileName, String ZipPath)
        {
            Stream _FileStream = new MemoryStream();
            if (File.Exists(ZipPath))
            {
                using (ZipFile zipFile = ZipFile.Read(ZipPath, ZipReadOptions))
                {
                    if (zipFile.ContainsEntry(FileName))
                    {
                        using (Stream StreamData = new MemoryStream())
                        {
                            zipFile[FileName].Extract(StreamData);
                            StreamData.Position = 0;
                            StreamData.CopyTo(_FileStream);
                            StreamData.Close();
                        }
                    }
                }
            }
            _FileStream.Position = 0;
            return _FileStream;
        }
        private T GetObject<T>(String ObjFileName, String ZipPath)
        {
            T Obj = default(T);
            using (Stream tmpStream = GetFileStream(ObjFileName, ZipPath))
            {
                if (tmpStream != null && !tmpStream.Equals(Stream.Null))
                {
                    Obj = tmpStream.DeserializeStream<T>();
                }
                tmpStream.Close();
            }
            return Obj;
        }
        #endregion
    }
}

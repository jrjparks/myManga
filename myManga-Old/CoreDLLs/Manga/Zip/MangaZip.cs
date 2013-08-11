using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

using Manga.Core;
using Manga.Archive;
using Manga.Info;
using Manga.Xml;

using ICSharpCode.SharpZipLib.Zip;

namespace Manga.Zip
{
    public class MangaZip : IDisposable
    {
        #region Events
        public delegate void ProgressChange(Object Sender, Int32 Progress, Object Data);
        public event ProgressChange ProgressChanged;
        protected virtual void OnProgressChanged(Int32 Progress)
        {
            if (ProgressChanged != null)
                ProgressChanged(this, Progress, default(MangaArchiveInfo));
        }
        protected virtual void OnProgressChanged(Int32 Progress, Object Data)
        {
            if (ProgressChanged != null)
                ProgressChanged(this, Progress, Data);
        }
        #endregion

        #region Vars
        private ZipFile mzaZif { get; set; }
        public MangaArchiveInfo MAI { get; private set; }
        private ZipFile mizaZif { get; set; }
        public MangaInfo MI { get; private set; }
        #endregion

        #region Constructors
        public MangaZip() { }
        #endregion

        #region MZA
        #region Static
        public static String MZA_SavePath(MangaArchiveInfo MAI) 
        { return Path.Combine(MAI.TmpMangaFolderLocation, MAI.MAISaveName); }

        public static void TestMZAPageFiles(ZipFile ZipFile, MangaArchiveInfo MAI)
        {
            if (ZipFile != null)
            {
                MAI.PageEntries.ForEach(
                    delegate(PageEntry Entry)
                    {
                        Int32 FileEntryIndex = ZipFile.FindEntry(Entry.FileName, true);
                        Entry.Downloaded = FileEntryIndex >= 0;
                        if (Entry.Downloaded)
                            Entry.Downloaded = ZipFile.GetEntry(Entry.FileName).Size.Equals(Entry.FileSize);
                    });
            }
        }

        public static Bitmap LoadWinImageFromMZA(UInt32 Page, String MZAPath)
        { return new Bitmap(LoadImageStreamFromMZA(Page, MZAPath)); }
        public static MemoryStream LoadImageStreamFromMZA(UInt32 Page, String MZAPath)
        {
            MemoryStream ImageStream = new MemoryStream();
            if (File.Exists(MZAPath))
            {
                ZipFile MZA = new ZipFile(MZAPath);
                MangaArchiveInfo MAI = null;
                if (MZA.FindEntry(MangaArchiveData.InfoFileName, true) >= 0)
                {
                    MAI = XmlDataSave.XMLDeserializeStream<MangaArchiveInfo>(
                        MZA.GetInputStream(
                            MZA.GetEntry(MangaArchiveData.InfoFileName)
                        )
                    );
                    TestMZAPageFiles(MZA, MAI);
                    if (MAI.PageEntries.Contains(Page))
                    {
                        String ImageName = MAI.GetPageName(Page);
                        if (MZA.FindEntry(ImageName, false) >= 0)
                        {
                            long ImageStart = ImageStream.Position;
                            MZA.GetInputStream(MZA.GetEntry(ImageName)).CopyTo(ImageStream);
                            ImageStream.Position = ImageStart;
                        }
                    }
                }
                MZA.Close();
            }
            GC.Collect();
            return ImageStream;
        }

        public static MangaArchiveInfo LoadMZA_MAI(String ZipPath)
        { return XmlDataSave.XMLDeserializeStream<MangaArchiveInfo>(LoadFileStream(ZipPath, MangaArchiveData.InfoFileName)); }

        public static String staticSaveMZA(MangaArchiveInfo MAI)
        {
            String mzaSavePath = MZA_SavePath(MAI);
            if (File.Exists(mzaSavePath))
                File.Delete(mzaSavePath);

            using (FileStream mzaFS = File.Create(mzaSavePath))
            {
                using (ZipOutputStream mzaOutStream = new ZipOutputStream(mzaFS))
                {
                    mzaOutStream.UseZip64 = UseZip64.Dynamic;
                    mzaOutStream.SetLevel(9);

                    AddStreamToZip(XmlDataSave.XMLSerializeData<MangaArchiveInfo>(MAI), MangaArchiveData.InfoFileName, mzaOutStream);
                    foreach (PageEntry MAIE in MAI.PageEntries)
                    {
                        AddFileToZip(MAI.GetTmpPagePath(MAIE.PageNumber), mzaOutStream);
                    }

                    mzaOutStream.IsStreamOwner = true;
                    mzaOutStream.Close();
                }
            }
            CleanUnusedMangaImages(MAI);
            return mzaSavePath;
        }
        #endregion

        public void LoadMZA(String FilePath)
        {
            FileInfo FI = new FileInfo(FilePath);
            if (FI.Exists && FI.Extension.Equals(".mza"))
            {
                if (mzaZif != null)
                    mzaZif.Close();
                mzaZif = new ZipFile(FilePath);
                if (mzaZif.FindEntry(MangaArchiveData.InfoFileName, true) >= 0)
                {
                    MAI = XmlDataSave.XMLDeserializeStream<MangaArchiveInfo>(mzaZif.GetInputStream(mzaZif.GetEntry(MangaArchiveData.InfoFileName)));
                    TestMZAPageFiles();
                }
            }
        }

        public String SaveMZA(MangaArchiveInfo MAI)
        {
            Double Progress = 0, Step = 0;
            OnProgressChanged((Int32)Math.Round(++Progress));
            String mzaSavePath = MZA_SavePath(MAI);
            if (File.Exists(mzaSavePath))
                File.Delete(mzaSavePath);
            OnProgressChanged((Int32)Math.Round(++Progress));
            Step = (99D - Progress) / MAI.PageEntries.Count;

            using (FileStream mzaFS = File.Create(mzaSavePath))
            {
                using (ZipOutputStream mzaOutStream = new ZipOutputStream(mzaFS))
                {
                    mzaOutStream.UseZip64 = UseZip64.Dynamic;
                    mzaOutStream.SetLevel(9);

                    AddStreamToZip(XmlDataSave.XMLSerializeData<MangaArchiveInfo>(MAI), MangaArchiveData.InfoFileName, mzaOutStream);
                    foreach (PageEntry MAIE in MAI.PageEntries)
                    {
                        AddFileToZip(MAI.GetTmpPagePath(MAIE.PageNumber), mzaOutStream);
                        OnProgressChanged((Int32)Math.Round(Progress += Step));
                    }

                    mzaOutStream.IsStreamOwner = true;
                    mzaOutStream.Close();
                }
            }
            CleanUnusedMangaImages(MAI);
            OnProgressChanged(100);
            return mzaSavePath;
        }

        public void TestMZAPageFiles() { TestMZAPageFiles(mzaZif, MAI); }

        public Bitmap LoadWinImageFromMZA(UInt32 Page)
        { return new Bitmap(LoadImageStreamFromMZA(Page)); }
        public Stream LoadImageStreamFromMZA(UInt32 Page)
        {
            MemoryStream ImageStream = new MemoryStream();
            if (mzaZif != null && MAI.PageEntries.Contains(Page))
            {
                String ImageName = MAI.GetPageName(Page);
                if (mzaZif.FindEntry(ImageName, false) >= 0)
                {
                    long ImageStart = ImageStream.Position;
                    mzaZif.GetInputStream(mzaZif.GetEntry(ImageName)).CopyTo(ImageStream);
                    ImageStream.Position = ImageStart;
                }
            }
            return ImageStream;
        }
        #endregion

        #region MI
        #region Static
        public static Stream LoadMICoverStream(String ZipPath)
        {
            return LoadFileStream(ZipPath, MangaInfoData.CoverName);
        }
        public static Bitmap LoadMICover(String ZipPath)
        {
            Stream Image = LoadMICoverStream(ZipPath);
            if (Image == null)
                return new Bitmap(1, 1);
            return new Bitmap(Image);
        }

        public static MangaInfo LoadMIZA_MI(String ZipPath)
        { return XmlDataSave.XMLDeserializeStream<MangaInfo>(LoadFileStream(ZipPath, MangaInfoData.InfoFileName)); }

        public static String SaveMIZA_MI(MangaInfo MI)
        {
            String mizaSavePath = Path.Combine(Environment.CurrentDirectory, "MangaInfo", MI.MISaveName);
            return SaveMIZA_MI(MI, mizaSavePath);
        }
        public static String SaveMIZA_MI(MangaInfo MI, String SavePath)
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);

            using (FileStream mizaFS = File.Create(SavePath))
            {
                using (ZipOutputStream mizaOutStream = new ZipOutputStream(mizaFS))
                {
                    mizaOutStream.UseZip64 = UseZip64.Dynamic;
                    mizaOutStream.SetLevel(9);
                    if (MI.CoverStream != null)
                        AddStreamToZip(MI.CoverStream, MangaInfoData.CoverName, mizaOutStream);
                    MI.CoverStream.Close();
                    AddStreamToZip(XmlDataSave.XMLSerializeData<MangaInfo>(MI), MangaInfoData.InfoFileName, mizaOutStream);
                }
            }
            return SavePath;
        }
        
        public static String Update_MIZA(MangaInfo MI)
        {
            String mizaUpdatePath = Path.Combine(Environment.CurrentDirectory, "MangaInfo", MI.MISaveName);
            return Update_MIZA(MI, mizaUpdatePath);
        }
        public static String Update_MIZA(MangaInfo MI, String UpdatePath)
        {
            if (File.Exists(UpdatePath))
            {
                CoverData _CoverData = new CoverData("", LoadMICoverStream(UpdatePath));
                SaveMIZA_MI(MI, UpdatePath);
            }
            return UpdatePath;
        }
        #endregion
        public Bitmap LoadMICover()
        { return new Bitmap(LoadFileStreamFromMIZA(MangaInfoData.CoverName)); }

        public void LoadMIZA(String FilePath)
        {
            FileInfo FI = new FileInfo(FilePath);
            if (FI.Exists && FI.Extension.Equals(".miza"))
            {
                if (mizaZif != null)
                    mizaZif.Close();
                mizaZif = new ZipFile(FilePath);
                if (mizaZif.FindEntry(MangaInfoData.InfoFileName, true) >= 0)
                {
                    MI = XmlDataSave.XMLDeserializeStream<MangaInfo>(mzaZif.GetInputStream(mzaZif.GetEntry(MangaInfoData.InfoFileName)));
                }
            }
        }

        public String SaveMIZA() { return SaveMIZA(this.MI); }
        public String SaveMIZA(MangaInfo MI)
        {
            OnProgressChanged(1);
            String mizaSavePath = Path.Combine(Environment.CurrentDirectory, "MangaInfo", MI.MISaveName);
            if (File.Exists(mizaSavePath))
                File.Delete(mizaSavePath);
            OnProgressChanged(5);

            using (FileStream mizaFS = File.Create(mizaSavePath))
            {
                using (ZipOutputStream mizaOutStream = new ZipOutputStream(mizaFS))
                {
                    OnProgressChanged(10);
                    mizaOutStream.UseZip64 = UseZip64.Dynamic;
                    mizaOutStream.SetLevel(9);
                    OnProgressChanged(45);
                    if (MI.CoverStream != null)
                    {
                        AddStreamToZip(MI.CoverStream, MangaInfoData.CoverName, mizaOutStream);
                        MI.CoverStream.Close();
                    }
                    OnProgressChanged(65);
                    AddStreamToZip(XmlDataSave.XMLSerializeData<MangaInfo>(MI), MangaInfoData.InfoFileName, mizaOutStream);
                    OnProgressChanged(85);
                }
            }
            OnProgressChanged(100);
            return mizaSavePath;
        }

        public String UpdateMIZA() { return UpdateMIZA(this.MI); }
        public String UpdateMIZA(MangaInfo MI, CoverData CoverData)
        {
            String mizaUpdatePath = Path.Combine(Environment.CurrentDirectory, "MangaInfo", MI.MISaveName);
            return UpdateMIZA(MI, CoverData, mizaUpdatePath);
        }
        public String UpdateMIZA(MangaInfo MI, CoverData CoverData, String UpdatePath)
        {
            OnProgressChanged(5);
            if (File.Exists(UpdatePath))
            {
                OnProgressChanged(25);
                //MI.CoverStream = LoadMICoverStream(UpdatePath);
                OnProgressChanged(50);
                SaveMIZA_MI(MI, UpdatePath);
                OnProgressChanged(75);
            }
            OnProgressChanged(100);
            return UpdatePath;
        }
        #endregion

        #region ZipMethods
        #region Static
        private static void AddFileToZip(String FilePath, ZipOutputStream zipOutputStream) { AddFileToZip(FilePath, Path.GetFileName(FilePath), zipOutputStream); }
        private static void AddFileToZip(String FilePath, ZipOutputStream zipOutputStream, Int32 Offset) { AddFileToZip(FilePath, Path.GetFileName(FilePath), zipOutputStream, Offset); }
        private static void AddFileToZip(String FilePath, String InternalPath, ZipOutputStream zipOutputStream) { AddFileToZip(FilePath, InternalPath, zipOutputStream, 0); }
        private static void AddFileToZip(String FilePath, String InternalPath, ZipOutputStream zipOutputStream, Int32 Offset)
        {
            if (File.Exists(FilePath))
            {
                FileInfo fileInfo = new FileInfo(FilePath);
                ZipEntry newEntry = new ZipEntry(ZipEntry.CleanName(InternalPath));
                newEntry.DateTime = fileInfo.LastAccessTime;
                newEntry.Size = fileInfo.Length;
                newEntry.Offset = Offset;

                zipOutputStream.PutNextEntry(newEntry);
                using (FileStream fileReader = File.OpenRead(FilePath))
                    fileReader.CopyTo(zipOutputStream);
                zipOutputStream.CloseEntry();
            }
            GC.Collect();
        }

        private static void AddStreamToZip(Stream DataStream, String InternalPath, ZipOutputStream zipOutputStream) { AddStreamToZip(DataStream, InternalPath, zipOutputStream, 0); }
        private static void AddStreamToZip(Stream DataStream, String InternalPath, ZipOutputStream zipOutputStream, Int32 Offset)
        {
            if (DataStream != null)
            {
                DataStream.Position = 0;
                ZipEntry newEntry = new ZipEntry(ZipEntry.CleanName(InternalPath));
                newEntry.DateTime = DateTime.Now;
                newEntry.Size = DataStream.Length;
                newEntry.Offset = Offset;

                zipOutputStream.PutNextEntry(newEntry);
                DataStream.CopyTo(zipOutputStream);
                zipOutputStream.CloseEntry();
            }
            GC.Collect();
        }

        private static MemoryStream LoadFileStream(String ZipPath, String FileName)
        {
            MemoryStream FileStream = new MemoryStream();
            if (File.Exists(ZipPath))
            {
                ZipFile zip = new ZipFile(ZipPath);
                if (zip.FindEntry(FileName, true) >= 0)
                {
                    long FileStart = FileStream.Position;
                    zip.GetInputStream(zip.GetEntry(FileName)).CopyTo(FileStream);
                    FileStream.Position = FileStart;
                }
                zip.Close();
            }
            GC.Collect();
            return FileStream;
        }
        #endregion

        private MemoryStream LoadFileStreamFromMZA(String FileName)
        {
            MemoryStream FileStream = new MemoryStream();
            if (mzaZif != null)
            {
                if (mzaZif.FindEntry(FileName, true) >= 0)
                {
                    long FileStart = FileStream.Position;
                    mzaZif.GetInputStream(mzaZif.GetEntry(FileName)).CopyTo(FileStream);
                    FileStream.Position = FileStart;
                }
            }
            GC.Collect();
            return FileStream;
        }

        private MemoryStream LoadFileStreamFromMIZA(String FileName)
        {
            MemoryStream FileStream = new MemoryStream();
            if (mizaZif != null)
            {
                if (mizaZif.FindEntry(FileName, true) >= 0)
                {
                    long FileStart = FileStream.Position;
                    mizaZif.GetInputStream(mizaZif.GetEntry(FileName)).CopyTo(FileStream);
                    FileStream.Position = FileStart;
                }
            }
            GC.Collect();
            return FileStream;
        }
        #endregion

        #region Static Methods

        public static void CleanUnusedMangaImages(MangaArchiveInfo MAI)
        {
            Boolean Retried;
            DirectoryInfo DirectoryPath = new DirectoryInfo(MAI.TmpFolderLocation);
            do
            {
                Retried = false;
                if (DirectoryPath.GetDirectories().Length > 0)
                    break;
            retry:
                try
                {
                    DirectoryPath.Delete(true);
                }
                catch
                {
                    if (!Retried)
                    {
                        Retried = true;
                        goto retry;
                    }
                }
                DirectoryPath = DirectoryPath.Parent;
            } while (!DirectoryPath.FullName.Equals(MAI.TmpMangaFolderLocation));
        }

        #endregion

        public void Dispose()
        {
            if (mzaZif != null)
                mzaZif.Close();
            if (mizaZif != null)
                mizaZif.Close();
        }
    }
}

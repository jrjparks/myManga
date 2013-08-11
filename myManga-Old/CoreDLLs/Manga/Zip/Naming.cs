using System;
using System.Diagnostics;
using System.IO;
using BakaBox.IO;
using Ionic.Zip;
using Manga.Archive;
using Manga.Core;

namespace Manga.Zip
{
    [DebuggerStepThrough]
    public static class ZipNamingExtensions
    {
        #region MangaArchive
        public static String TempSaveLocation
        { get { return Path.Combine(Path.GetTempPath(), "MangaArchives"); } }

        public static String TempPath(this MangaData value)
        { return Path.Combine(TempSaveLocation, value.Name, String.Format("{0}.{1}.{2}", value.Volume, value.Chapter, value.SubChapter)).SafeFolder(); }
        public static String TempPath(this MangaArchiveInfo value, UInt32 page)
        { return Path.Combine(value.TempPath(), value.PageName(page)); }
        public static String PageName(this MangaArchiveInfo value, UInt32 page)
        {
            String PageName = String.Empty;
            if (value.PageEntries.Contains(page))
                PageName = value.PageEntries.GetPageByNumber(page).LocationInfo.FileName;
            return PageName.SafeFileName();
        }
        #endregion

        /// <summary>
        /// Saving a ZIP file creates a TMP file in the directory of saving.
        /// Services like DorpBox will index these files and start to sync them, this can sometimes lock the file.
        /// To overcome this, this method will save the ZIP to a TMP folder outside of the Cloud Syncing folder then move the file to the final destination.
        /// </summary>
        /// <param name="value">The ZipFile to save.</param>
        /// <param name="TempFolder">Where to save the zip to temporarily. (Folder)</param>
        /// <param name="DestFolder">The Final destination of the zip file. (Folder)</param>
        /// <param name="FileName">The File name of the zip file. (FileName)</param>
        /// <example>zipFile.CloudSafeSave("C:\MyTempFolder\","C:\DropBox\","MyZipFile.zip");</example>
        public static void CloudSafeSave(this ZipFile value, String TempFolder, String DestFolder, String FileName)
        {
            // Cloud-Sync Safety
            TempFolder = TempFolder.SafeFolder();
            DestFolder = DestFolder.SafeFolder();
            FileName = FileName.SafeFileName();
            String TempFilePath = Path.Combine(TempFolder, FileName),
                DestFilePath = Path.Combine(DestFolder, FileName);
            value.Save(TempFilePath);
            File.Move(TempFilePath, DestFilePath);
        }
    }
}

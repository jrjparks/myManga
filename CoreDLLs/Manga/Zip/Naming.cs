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
        { return Path.Combine(TempSaveLocation, value.Name, String.Format("{0}.{1}.{2}", value.Volume, value.Chapter, value.SubChapter)); }
        public static String TempPath(this MangaArchiveInfo value, UInt32 page)
        { return Path.Combine(TempSaveLocation, value.Name, String.Format("{0}.{1}.{2}", value.Volume, value.Chapter, value.SubChapter), value.PageName(page)); }
        public static String PageName(this MangaArchiveInfo value, UInt32 page)
        {
            String PageName = String.Empty;
            if (value.PageEntries.Contains(page))
                PageName = value.PageEntries.GetPageByNumber(page).LocationInfo.FileName;
            return PageName;
        }
        #endregion

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

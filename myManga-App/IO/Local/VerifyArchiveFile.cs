using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.IO.Storage.Manager.BaseInterfaceClasses;
using Core.IO;
using myMangaSiteExtension.Utilities;
using myMangaSiteExtension.Objects;
using System.IO;
using myManga_App.IO.Network;

namespace myManga_App.IO.Local
{
    public static class VerifyArchiveFile
    {
        private static readonly App App = App.Current as App;
        public static Boolean VerifyArchive(ZipStorage zip_storage, String filename)
        {
            FileInfo fi = new FileInfo(filename);
            if (fi.Length == 0)
            {
                // Delete empty files
                File.Delete(filename);
                return false;
            }
            try
            {
                String[] index_filenames = { typeof(MangaObject).Name, typeof(ChapterObject).Name };
                ZipStorageInformationObject zip_storage_information_object = zip_storage.GetInformation(filename);
                Ionic.Zip.ZipEntry index_zipentry = zip_storage_information_object.ArchiveEntries.FirstOrDefault(x => index_filenames.Contains(x.FileName));

                if (index_zipentry == null)
                    return false;

                Object index = null;
                Stream index_file_stream;
                using (index_file_stream = null)
                {
                    if (zip_storage.TryRead(filename, index_zipentry.FileName, out index_file_stream))
                    {
                        if (index_zipentry.FileName.Equals(index_filenames[0]))
                            index = index_file_stream.Deserialize<MangaObject>(App.UserConfig.SaveType);
                        else if (index_zipentry.FileName.Equals(index_filenames[1]))
                            index = index_file_stream.Deserialize<ChapterObject>(App.UserConfig.SaveType);
                    }
                }
                if (index != null)
                {
                    // TODO: Fix this code when looking up archive files.
                    if (index is MangaObject)
                    {
                        String[] MissingCovers = (index as MangaObject).Covers.Where(
                            c => zip_storage_information_object.ArchiveEntries.Count(
                                ze => ze.FileName.Equals(Path.GetFileName(c))) == 0).ToArray();
                        foreach (String Cover in MissingCovers)
                        {
                            if (Cover != null) App.DownloadManager.Download(Cover, filename);
                        }
                    }
                    else if (index is ChapterObject)
                    {
                        PageObject[] MissingPages = (index as ChapterObject).Pages.Where(
                            po => zip_storage_information_object.ArchiveEntries.Count(
                                ze => ze.FileName.Equals(po.Name)) == 0).ToArray();
                        foreach (PageObject Page in MissingPages)
                        {
                            if (Page.ImgUrl != null) App.DownloadManager.Download(Page.ImgUrl, filename);
                        }
                    }
                    else return true;
                }
            }
            catch { }
            return false;
        }
    }
}

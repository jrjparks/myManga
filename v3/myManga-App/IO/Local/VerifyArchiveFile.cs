using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.IO.Storage.Manager.BaseInterfaceClasses;
using myMangaSiteExtension.Objects;
using System.IO;

namespace myManga_App.IO.Local
{
    public static class VerifyArchiveFile
    {
        public static Boolean VerifyArchive(ZipStorage zip_storage, String filename)
        {
            String[] index_filenames = { typeof(MangaObject).Name, typeof(ChapterObject).Name };
            ZipStorageInformationObject zip_storage_information_object = zip_storage.GetInformation(filename);
            Ionic.Zip.ZipEntry index_zipentry = zip_storage_information_object.ArchiveEntries.FirstOrDefault(x => index_filenames.Contains(x.FileName));
            Stream index_file_stream;
            using (index_file_stream = null)
            {
                if (zip_storage.TryRead(filename, index_zipentry.FileName, out index_file_stream))
                {

                }
            }
            return false;
        }
    }
}

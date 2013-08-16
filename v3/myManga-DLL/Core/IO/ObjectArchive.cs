using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Ionic.Zip;

namespace Core.IO
{
    public static class ObjectArchive
    {
        private const ReadOptions DefaultZipReadOptions = new ReadOptions() { Encoding = Encoding.UTF8 };

        public static Boolean SaveArchive<T>(this T Object, String ArchiveFilePath, String FileName, SaveType SaveType = SaveType.Binary) where T : class
        {
            return Object.SaveArchive(ArchiveFilePath, FileName, DefaultZipReadOptions, SaveType);
        }
        public static Boolean SaveArchive<T>(this T Object, String ArchiveFilePath, String FileName, ReadOptions ZipReadOption, SaveType SaveType = SaveType.Binary) where T : class
        {
            if (Object != null)
            {
                Boolean FileExists = (File.Exists(ArchiveFilePath) && ZipFile.IsZipFile(ArchiveFilePath));
                String FileIOPath = Path.GetTempFileName();
                try
                {
                    using (ZipFile zipFile = FileExists ? ZipFile.Read(ArchiveFilePath, ZipReadOption) : new ZipFile(FileIOPath, Encoding.UTF8))
                    {
                        DateTime dt = DateTime.Now;
                        zipFile.Comment = String.Format("{0} - {1}", dt.ToLongDateString(), dt.ToLongTimeString());
                        zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                        zipFile.CompressionMethod = CompressionMethod.Deflate;

                        zipFile.UpdateEntry(FileName, Object.Serialize(SaveType));

                        zipFile.Save(FileIOPath);
                        File.Move(FileIOPath, ArchiveFilePath);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    if (File.Exists(FileIOPath))
                        File.Delete(FileIOPath);
                    GC.Collect();
                    throw new Exception(String.Format("Error saving archive from {0} to {1}.", Object.ToString(), ArchiveFilePath), ex);
                }
            }
            return false;
        }

        public static T LoadArchive<T>(this String ArchiveFilePath, String FileName, SaveType SaveType = SaveType.Binary) where T : class
        {
            if (File.Exists(ArchiveFilePath))
            {
                T Object = null;
                using (Stream DataStream = ArchiveFilePath.LoadArchive(FileName))
                {
                    Object = DataStream.Deserialize<T>(SaveType);
                }
                return Object;
            }
            return null;
        }

        public static Stream LoadArchive(this String ArchiveFilePath, String FileName)
        {
            return ArchiveFilePath.LoadArchive(FileName, DefaultZipReadOptions);
        }
        public static Stream LoadArchive(this String ArchiveFilePath, String FileName, ReadOptions ZipReadOption)
        {
            if (File.Exists(ArchiveFilePath))
            {
                MemoryStream DataStream = new MemoryStream();
                using (ZipFile zipFile = ZipFile.Read(ArchiveFilePath, ZipReadOption))
                {
                    using (Stream ZipData = new MemoryStream())
                    {
                        zipFile[FileName].Extract(ZipData);
                        ZipData.Position = 0;
                        ZipData.CopyTo(DataStream);
                    }
                }
                DataStream.Position = 0;
                return DataStream;
            }
            return null;
        }
    }
}

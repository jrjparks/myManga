using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using System.IO.Compression;

namespace Core.IO
{
    /// <summary>
    /// Save and Load Objects
    /// </summary>
    [DebuggerStepThrough]
    public static class ObjectStorage
    {
        public static Boolean SaveObject<T>(this T Object, String FilePath, SaveType SaveType = SaveType.Binary) where T : class
        {
            if (Object != null)
            {
                Path.GetDirectoryName(FilePath).SafeFolder();
                String FileIOPath = Path.GetTempFileName();
                try
                {
                    using (Stream FileIOStream = new FileInfo(FileIOPath).OpenWrite())
                    {
                        using (Stream tmpStream = Object.Serialize(SaveType))
                        {
                            tmpStream.Position = 0;
                            tmpStream.CopyTo(FileIOStream);
                        }
                    }
                    File.Move(FileIOPath, FilePath);
                    GC.Collect();
                    return true;
                }
                catch (Exception ex)
                {
                    if (File.Exists(FileIOPath))
                        File.Delete(FileIOPath);
                    GC.Collect();
                    throw new Exception(String.Format("Error saving data from {0}.", Object.ToString()), ex);
                }
            }
            return false;
        }

        public static T LoadObject<T>(this String FilePath, SaveType SaveType = SaveType.Binary) where T : class
        {
            try
            {
                T Object = null;
                if (File.Exists(FilePath))
                    using (Stream FileStream = new FileInfo(FilePath).OpenRead())
                    {
                        Object = FileStream.Deserialize<T>(SaveType);
                    }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Error loading data from {0}.", FilePath.ToString()), ex);
            }
            return null;
        }

        public static Stream Serialize<T>(this T Object, SaveType SaveType = SaveType.Binary) where T : class
        {
            if (Object != null)
            {
                try
                {
                    Stream ObjectStream = new MemoryStream();
                    using (Stream tmpStream = new MemoryStream())
                    {
                        switch (SaveType)
                        {
                            default:
                            case ObjectStorage.SaveType.Binary:
                                using (GZipStream gzipStream = new GZipStream(tmpStream, CompressionMode.Compress))
                                {
                                    BinaryFormatter ObjectFormatter = new BinaryFormatter();
                                    ObjectFormatter.Serialize(gzipStream, Object);
                                    gzipStream.Position = 0;
                                    gzipStream.CopyTo(ObjectStream);
                                }
                                break;

                            case ObjectStorage.SaveType.XML:
                                using (TextWriter ObjectXMLWriter = new StreamWriter(tmpStream, Encoding.UTF8))
                                {
                                    XmlSerializer ObjectSerializer = new XmlSerializer(typeof(T));
                                    ObjectSerializer.Serialize(ObjectXMLWriter, Object);
                                    tmpStream.Position = 0;
                                    tmpStream.CopyTo(ObjectStream);
                                }
                                break;
                        }
                    }
                    ObjectStream.Position = 0;
                    return ObjectStream;
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("Error serializing data as {0}.", SaveType.ToString()), ex);
                }
            }
            return null;
        }

        public static T Deserialize<T>(this Stream ObjectStream, SaveType SaveType = SaveType.Binary) where T : class
        {
            if (ObjectStream != null)
            {
                try
                {
                    T Object = null;
                    switch (SaveType)
                    {
                        default:
                        case ObjectStorage.SaveType.Binary:
                            using (GZipStream gzipStream = new GZipStream(ObjectStream, CompressionMode.Decompress))
                            {
                                BinaryFormatter ObjectFormatter = new BinaryFormatter();
                                Object = (T)ObjectFormatter.Deserialize(gzipStream);
                            }
                            break;

                        case ObjectStorage.SaveType.XML:
                            XmlSerializer ObjectSerializer = new XmlSerializer(typeof(T));
                            Object = (T)ObjectSerializer.Deserialize(ObjectStream);
                            break;
                    }
                    return Object;
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("Error deserializing data as {0}.", SaveType.ToString()), ex);
                }
            }
            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using Ionic.Zlib;

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
                return Object.Serialize(SaveType).SaveStream(FilePath);
            return false;
        }

        public static Boolean SaveStream(this Stream Stream, String FilePath)
        {
            if (Stream != null)
            {
                long origPos = Stream.Position;
                if (FilePath.Contains('\\'))
                    Path.GetDirectoryName(FilePath).SafeFolder();
                String FileIOPath = Path.GetTempFileName();
                try
                {
                    Stream.Seek(0, SeekOrigin.Begin);
                    using (Stream FileIOStream = new FileInfo(FileIOPath).OpenWrite())
                    {
                        Stream.CopyTo(FileIOStream);
                    }
                    Stream.Seek(origPos, SeekOrigin.Begin);
                    File.Copy(FileIOPath, FilePath
                        , true);
                    File.Delete(FileIOPath);
                }
                catch (Exception ex)
                {
                    if (File.Exists(FileIOPath))
                        File.Delete(FileIOPath);
                    GC.Collect();
                    throw new Exception("Error saving stream.", ex);
                }
            }
            return false;
        }

        public static T LoadObject<T>(this T Object, String FilePath, SaveType SaveType = SaveType.Binary) where T : class
        {
            Object = null;
            try
            {
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
            return Object;
        }

        public static Stream LoadStream(this Stream Stream, String FilePath)
        {
            Stream = new MemoryStream();
            try
            {
                if (File.Exists(FilePath))
                    using (Stream FileStream = new FileInfo(FilePath).OpenRead())
                    {
                        FileStream.CopyTo(Stream);
                    }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Error loading stream from {0}.", FilePath.ToString()), ex);
            }
            return Stream;
        }

        public static Stream Serialize<T>(this T Object, SaveType SaveType = SaveType.Binary) where T : class
        {
            if (Object != null)
            {
                try
                {
                    MemoryStream ObjectStream = new MemoryStream();
                    using (MemoryStream SerializeStream = new MemoryStream())
                    {
                        switch (SaveType)
                        {
                            default:
                            case SaveType.Binary:
                                BinaryFormatter ObjectFormatter = new BinaryFormatter();
                                using (GZipStream GZipSerializeStream = new GZipStream(SerializeStream, CompressionMode.Compress, true))
                                { ObjectFormatter.Serialize(GZipSerializeStream, Object); } // Compress stream with GZip
                                break;

                            case SaveType.XML:
                                XmlSerializer ObjectSerializer = new XmlSerializer(typeof(T));
                                using (TextWriter ObjectXMLWriter = new StreamWriter(SerializeStream, Encoding.UTF8, 2 << 18, true)) // 2 << 18 = 512 KB buffer
                                { ObjectSerializer.Serialize(ObjectXMLWriter, Object); }
                                break;
                        }
                        SerializeStream.Seek(0, SeekOrigin.Begin);
                        SerializeStream.CopyTo(ObjectStream);
                    }
                    ObjectStream.Seek(0, SeekOrigin.Begin);
                    return ObjectStream;
                }
                catch (Exception ex)
                { throw new Exception(String.Format("Error serializing data as {0}.", SaveType.ToString()), ex); }
            }
            return null;
        }

        public static T Deserialize<T>(this Stream ObjectStream, SaveType SaveType = SaveType.Binary) where T : class
        {
            if (ObjectStream != null)
            {
                ObjectStream.Seek(0, SeekOrigin.Begin);
                try
                {
                    T Object = null;
                    switch (SaveType)
                    {
                        default:
                        case SaveType.Binary:
                            BinaryFormatter ObjectFormatter = new BinaryFormatter();
                            using (GZipStream GZipObjectStream = new GZipStream(ObjectStream, CompressionMode.Decompress, true))
                            { Object = (T)ObjectFormatter.Deserialize(GZipObjectStream); } // Decompress stream with GZip
                            break;

                        case SaveType.XML:
                            XmlSerializer ObjectSerializer = new XmlSerializer(typeof(T));
                            Object = (T)ObjectSerializer.Deserialize(ObjectStream);
                            break;
                    }
                    return Object;
                }
                catch (Exception ex)
                { throw new Exception(String.Format("Error deserializing data as {0}.", SaveType.ToString()), ex); }
            }
            return null;
        }
    }
}
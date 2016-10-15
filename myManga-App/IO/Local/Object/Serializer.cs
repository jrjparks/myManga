using System;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using System.IO.Compression;

namespace myManga_App.IO.Local.Object
{
    public enum SerializeType
    {
        XML,
        Binary,
    }

    [DebuggerStepThrough]
    public static class Serializer
    {
        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(Serializer));

        #region Serialize
        public static Stream Serialize<ObjectType>(this ObjectType Object)
            where ObjectType : class
        { return Object.Serialize(SerializeType.XML); }

        public static Stream Serialize<ObjectType>(this ObjectType Object, SerializeType SerializeType)
            where ObjectType : class
        {
            if (!Equals(Object, null))
            {
                Stream SerializedObjectStream = new MemoryStream();
                try
                {
                    switch (SerializeType)
                    {
                        default:
                        case SerializeType.XML:
                            XmlSerializer XmlSerializer = new XmlSerializer(typeof(ObjectType));
                            using (Stream SerializingStream = new MemoryStream())
                            {
                                XmlSerializer.Serialize(SerializingStream, Object);
                                SerializingStream.Seek(0, SeekOrigin.Begin);
                                SerializingStream.CopyTo(SerializedObjectStream);
                            }
                            XmlSerializer = null;
                            break;

                        case SerializeType.Binary:
                            BinaryFormatter BinaryFormatter = new BinaryFormatter();
                            using (GZipStream GZipSerializeStream = new GZipStream(new MemoryStream(), CompressionMode.Compress, true))
                            {
                                BinaryFormatter.Serialize(GZipSerializeStream, Object);
                                GZipSerializeStream.Seek(0, SeekOrigin.Begin);
                                GZipSerializeStream.CopyTo(SerializedObjectStream);
                            }
                            BinaryFormatter = null;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    String ExceptionMessage = String.Format("Error serializing data as {0}.", SerializeType.ToString());
                    logger.Error(ExceptionMessage, ex);
                    throw new Exception(ExceptionMessage, ex);
                }
                SerializedObjectStream.Seek(0, SeekOrigin.Begin);
                return SerializedObjectStream;
            }
            return null;
        }
        #endregion

        #region Deserialize
        public static ObjectType Deserialize<ObjectType>(this Stream ObjectStream)
            where ObjectType : class
        { return ObjectStream.Deserialize<ObjectType>(SerializeType.XML); }

        public static ObjectType Deserialize<ObjectType>(this Stream ObjectStream, SerializeType SerializeType)
            where ObjectType : class
        {
            ObjectType DeserializedObject = null;
            if (!Equals(ObjectStream, null))
            {
                ObjectStream.Seek(0, SeekOrigin.Begin);
                try
                {
                    switch (SerializeType)
                    {
                        default:
                        case SerializeType.XML:
                            XmlSerializer XmlSerializer = new XmlSerializer(typeof(ObjectType));
                            DeserializedObject = (ObjectType)XmlSerializer.Deserialize(ObjectStream);
                            XmlSerializer = null;
                            break;

                        case SerializeType.Binary:
                            BinaryFormatter BinaryFormatter = new BinaryFormatter();
                            using (GZipStream GZipObjectStream = new GZipStream(ObjectStream, CompressionMode.Decompress, true))
                            { DeserializedObject = (ObjectType)BinaryFormatter.Deserialize(GZipObjectStream); }
                            BinaryFormatter = null;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    String ExceptionMessage = String.Format("Error deserializing data as {0}.", SerializeType.ToString());
                    logger.Error(ExceptionMessage, ex);
                    throw new Exception(ExceptionMessage, ex);
                }
            }
            return DeserializedObject;
        }
        #endregion
    }
}

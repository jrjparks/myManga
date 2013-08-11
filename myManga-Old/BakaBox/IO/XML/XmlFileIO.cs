using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;

namespace BakaBox.IO.XML
{
    /// <summary>
    /// Read/Write Type to Stream.
    /// </summary>
    [DebuggerStepThrough]
    public static class XmlFileIO
    {
        /// <summary>
        /// Save Object to XML File.
        /// </summary>
        /// <typeparam name="T">Type of Object.</typeparam>
        /// <param name="Object">Object to Save.</param>
        /// <param name="SavePath">Path to save to.</param>
        /// <returns>Boolean value of save status.</returns>
        public static Boolean SaveObject<T>(this T Object, String SavePath)
        {
            Boolean Saved = false;
            try
            {
                XmlSerializer XmlSer = new XmlSerializer(typeof(T));

                Path.GetDirectoryName(SavePath).SafeFolder();
                if (File.Exists(SavePath))
                    File.Delete(SavePath);

                using (Stream SaveStream = new FileInfo(SavePath).OpenWrite())
                {
                    using (Stream TmpStream = Object.SerializeObject())
                    {
                        TmpStream.Position = 0;
                        TmpStream.CopyTo(SaveStream);
                    }
                }
                Saved = true;
            }
            finally { }
            GC.Collect();
            return Saved;
        }

        /// <summary>
        /// Load Object from XML File.
        /// </summary>
        /// <typeparam name="T">Type of Object.</typeparam>
        /// <param name="LoadPath">Path to load from.</param>
        /// <returns>Loaded Object.</returns>
        public static T LoadObject<T>(this String LoadPath)
        {
            T Object = default(T);
            try
            {
                if (File.Exists(LoadPath))
                {
                    using (Stream Save = new FileInfo(LoadPath).OpenRead())
                    {
                        using (TextReader XMLreader = new StreamReader(Save, Encoding.UTF8))
                        {
                            Object = DeserializeStream<T>(XMLreader);
                        }
                    }
                }
            }
            finally { }
            return Object;
        }

        /// <summary>
        /// Serialize Object to Stream.
        /// </summary>
        /// <typeparam name="T">Type of Object.</typeparam>
        /// <param name="Object">Object to Serialize.</param>
        /// <returns>Stream of Serialize Object.</returns>
        public static Stream SerializeObject<T>(this T Object)
        {
            if (Object != null)
            {
                Stream DataStream = new MemoryStream();
                XmlSerializer XmlSer = new XmlSerializer(typeof(T));

                using (Stream TmpStream = new MemoryStream())
                {
                    using (TextWriter XMLwriter = new StreamWriter(TmpStream, Encoding.UTF8))
                    {
                        XmlSer.Serialize(XMLwriter, Object);
                        TmpStream.Position = 0;
                        TmpStream.CopyTo(DataStream);
                    }
                }
                DataStream.Position = 0;
                return DataStream;
            }
            return null;
        }
        
        /// <summary>
        /// Binary Serialize Object to Stream.
        /// </summary>
        /// <typeparam name="T">Type of Object.</typeparam>
        /// <param name="Object">Object to Serialize.</param>
        /// <returns>Stream of Binary Serialize Object.</returns>
        public static Stream BinarySerializeObject<T>(this T Object)
        {
            if (Object != null)
            {
                Stream DataStream = new MemoryStream();
                BinaryFormatter BinSer = new BinaryFormatter();

                BinSer.Serialize(DataStream, Object);

                DataStream.Position = 0;
                return DataStream;
            }
            return null;
        }

        /// <summary>
        /// Deserialize Object from Stream.
        /// </summary>
        /// <typeparam name="T">Type of Object.</typeparam>
        /// <param name="DataStream">Stream of Object XML.</param>
        /// <returns>Deserialized Object.</returns>
        public static T DeserializeStream<T>(this Stream DataStream)
        {
            T Obj = default(T);
            if (DataStream != null)
            {
                try
                {
                    XmlSerializer XmlSer = new XmlSerializer(typeof(T));

                    Obj = (T)XmlSer.Deserialize(DataStream);
                }
                catch { Obj = default(T); }
            }
            GC.Collect();
            return Obj;
        }
        /// <summary>
        /// Deserialize Object from Stream.
        /// </summary>
        /// <typeparam name="T">Type of Object.</typeparam>
        /// <param name="XMLreader">TextReader of Object XML.</param>
        /// <returns>Deserialized Object.</returns>
        public static T DeserializeStream<T>(this TextReader XMLreader)
        {
            T Obj = default(T);
            if (XMLreader != null)
            {
                try
                {
                    XmlSerializer XmlSer = new XmlSerializer(typeof(T));

                    Obj = (T)XmlSer.Deserialize(XMLreader);
                }
                catch { Obj = default(T); }
            }
            GC.Collect();
            return Obj;
        }
        /// <summary>
        /// Binary Deserialize Object from Stream.
        /// </summary>
        /// <typeparam name="T">Type of Object.</typeparam>
        /// <param name="DataStream">Stream of Object XML.</param>
        /// <returns>Binary Deserialized Object.</returns>
        public static T BinaryDeserializeStream<T>(this Stream DataStream)
        {
            T Obj = default(T);
            if (DataStream != null)
            {
                try
                {
                    BinaryFormatter BinSer = new BinaryFormatter();

                    Obj = (T)BinSer.Deserialize(DataStream);
                }
                catch { Obj = default(T); }
            }
            GC.Collect();
            return Obj;
        }
    }
}

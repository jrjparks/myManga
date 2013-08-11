using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Core.IO
{
    /// <summary>
    /// Save and Load Objects
    /// </summary>
    [DebuggerStepThrough]
    public class ObjectStorage
    {
        public enum SaveType
        {
            Binary = "Binary",
            XML = "XML"
        }

        public static Boolean SaveObject<T>(this T Object, String SavePath, SaveType SaveType = SaveType.Binary)
        {
            return false;
        }

        public static Stream SerializeBinary<T>(this T Object)
        {
            if (Object != null)
            {
                Stream ObjectStream = new MemoryStream();
                BinaryFormatter ObjectFormatter = new BinaryFormatter();
                ObjectFormatter.Serialize(ObjectStream, Object);
                ObjectStream.Position = 0;
                return ObjectStream;
            }
            return null;
        }

        public static T DeserializeBinary<T>(this Stream ObjectStream)
        {
            if (ObjectStream != null)
            {
                try
                {
                    BinaryFormatter ObjectFormatter = new BinaryFormatter();
                    return (T)ObjectFormatter.Deserialize(ObjectStream);
                }
                catch { }
            }
            return default(T);
        }
    }
}

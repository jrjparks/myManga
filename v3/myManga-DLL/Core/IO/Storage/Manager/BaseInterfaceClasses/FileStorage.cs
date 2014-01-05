using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Core.IO.Storage.Manager.BaseInterfaceClasses
{
    public class FileStorage : StorageInterface
    {
        public bool Write(string filename, System.IO.Stream stream, params object[] args)
        {
            try
            {
                using (Stream fstream = File.Open(filename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                {
                    Byte[] buffer = new Byte[8 * 1024]; Int32 length;
                    while ((length = stream.Read(buffer, 0, buffer.Length)) > 0)
                        fstream.Write(buffer, 0, length);
                }
                return true;
            }
            catch { return false; }
        }

        public System.IO.Stream Read(string filename, params object[] args)
        {
            Stream stream = new MemoryStream();
            using (Stream fstream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                Byte[] buffer = new Byte[8 * 1024]; Int32 length;
                while ((length = fstream.Read(buffer, 0, buffer.Length)) > 0)
                    stream.Write(buffer, 0, length);
            }
            return stream;
        }

        public bool TryRead(string filename, out System.IO.Stream stream, params object[] args)
        {
            try { stream = Read(filename, args); return true; }
            catch { stream = null; return false; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.IO.Storage.Manager.BaseInterfaceClasses
{
    public class ZipStorage : StorageInterface
    {
        public bool Write(string filename, System.IO.Stream stream, params object[] args)
        {
            throw new NotImplementedException();
        }

        public System.IO.Stream Read(string filename, params object[] args)
        {
            throw new NotImplementedException();
        }

        public bool TryRead(string filename, out System.IO.Stream stream, params object[] args)
        {
            throw new NotImplementedException();
        }
    }
}

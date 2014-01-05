using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.IO.Storage.Manager
{
    public interface StorageInterface
    {
        Boolean Write(String filename, Stream stream, params Object[] args);
        Stream Read(String filename, params Object[] args);
        Boolean TryRead(String filename, out Stream stream, params Object[] args);
    }
}

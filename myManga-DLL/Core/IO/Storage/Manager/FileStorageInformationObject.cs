using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.IO.Storage.Manager
{
    public class FileStorageInformationObject
    {
        public String Name { get; set; }
        public String FullPath { get; set; }
        public Int64 Size { get; set; }
        public DateTime LastAccess { get; set; }
        public DateTime LastWrite { get; set; }
    }
}

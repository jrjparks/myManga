using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Manga.Manager_v2.Zip
{
    class ZipDataHandler
    {
        private ZipDataType _ZipDataType;
        public void SetZipDataType(ZipDataType value)
        {
            _ZipDataType = value;
        }
        public ZipDataType GetZipDataType()
        {
            return _ZipDataType;
        }

        private readonly Stream _DataStream;
        public Stream GetDataStream()
        {
            return _DataStream;
        }

        public ZipDataHandler(Stream DataStream)
        {
            _DataStream = DataStream;
        }
    }
}

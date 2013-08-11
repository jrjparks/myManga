using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Manga.Manager_v2.Zip_v2
{
    public enum DataType { Data, MangaInfo, MangaArchiveInfo }
    public enum StreamDirection { Read, Write }

    class Zip_v2_TaskData
    {
        private readonly String zipFileLocation;
        private readonly String zipFileName;
        private readonly DataType dataType;
        private readonly StreamDirection streamDirection;
        private Stream data;

        public Zip_v2_TaskData(String zipFileLocation, String zipFileName, DataType dataType, StreamDirection streamDirection)
        {
            this.zipFileLocation = zipFileLocation;
            this.zipFileName = zipFileName;
            this.dataType = dataType;
            this.streamDirection = streamDirection;
        }

        public String ZipFileLocation
        {
            get { return zipFileLocation; }
        }

        public String ZipFileName
        {
            get { return zipFileName; }
        }

        public DataType DataType
        {
            get { return dataType; }
        }

        public StreamDirection StreamDirection
        {
            get { return streamDirection; }
        }

        public Stream Data
        {
            get { return data; }
            set { data = value; }
        }
    }
}

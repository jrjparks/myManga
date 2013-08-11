using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Manga.Info;
using Manga.Archive;
using BakaBox.IO.XML;
using BakaBox.Controls.Threading;
using Ionic.Zip;

namespace Manga.Manager_v2.Zip_v2
{
    public class Zip_v2
    {
        #region Events
        public delegate void EmptyReturnEvent(Object Sender, Guid Id);
        public delegate void ErrReturnEvent(Object Sender, Guid Id, Exception e);
        public delegate void DataReturnEvent(Object Sender, Guid Id, Stream Data);
        public delegate void MangaInfoReturnEvent(Object Sender, Guid Id, MangaInfo Data);
        public delegate void MangaArchiveInfoReturnEvent(Object Sender, Guid Id, MangaArchiveInfo Data);

        public event EmptyReturnEvent EmptyReturn;
        public event ErrReturnEvent ErrReturn;
        public event DataReturnEvent DataReturn;
        public event MangaInfoReturnEvent MangaInfoReturn;
        public event MangaArchiveInfoReturnEvent MangaArchiveInfoReturn;

        private void OnErrReturn(Object Sender, Guid Id, Exception e)
        {
            if (ErrReturn != null)
                ErrReturn(Sender, Id, e);
        }
        private void OnDataReturn(Object Sender, Guid Id, Stream Data)
        {
            if (DataReturn != null)
                DataReturn(Sender, Id, Data);
        }
        private void OnEmptyReturn(Object Sender, Guid Id)
        {
            if (EmptyReturn != null)
                EmptyReturn(Sender, Id);
        }
        private void OnMangaInfoReturn(Object Sender, Guid Id, MangaInfo Data)
        {
            if (MangaInfoReturn != null)
                MangaInfoReturn(Sender, Id, Data);
        }
        private void OnMangaArchiveInfoReturn(Object Sender, Guid Id, MangaArchiveInfo Data)
        {
            if (MangaArchiveInfoReturn != null)
                MangaArchiveInfoReturn(Sender, Id, Data);
        }
        #endregion

        #region Constructor
        private readonly QueuedBackgroundWorker<Zip_v2_TaskData> _ZipLoader;

        private ReadOptions _ZipReadOptions;
        private ReadOptions ZipReadOptions
        {
            get
            {
                if (_ZipReadOptions == null)
                {
                    _ZipReadOptions = new ReadOptions();
                    _ZipReadOptions.Encoding = Encoding.UTF8;
                }
                return _ZipReadOptions;
            }
        }

        public Zip_v2()
        {
            _ZipLoader = new QueuedBackgroundWorker<Zip_v2_TaskData>();
            _ZipLoader.WorkerSupportsCancellation = true;
            _ZipLoader.DoTaskWork += _ZipLoader_DoTaskWork;
            _ZipLoader.TaskComplete += _ZipLoader_TaskComplete;
        }
        #endregion

        #region Save Methods
        public Guid SaveFileToZip(String zipLocation, String filename)
        {
            Zip_v2_TaskData z2td = new Zip_v2_TaskData(zipLocation, filename, DataType.Data, StreamDirection.Write);
            return _ZipLoader.AddToQueue(z2td);
        }

        public Guid SaveMangaInfoToZip(String zipLocation, MangaInfo mi)
        {
            Zip_v2_TaskData z2td = new Zip_v2_TaskData(zipLocation, MangaInfoConst.InfoFileName, DataType.MangaInfo, StreamDirection.Write);
            z2td.Data = mi.SerializeObject();
            return _ZipLoader.AddToQueue(z2td);
        }

        public Guid SaveMangaArchiveInfoToZip(String zipLocation, MangaArchiveInfo mai)
        {
            Zip_v2_TaskData z2td = new Zip_v2_TaskData(zipLocation, MangaArchiveConst.InfoFileName, DataType.MangaArchiveInfo, StreamDirection.Write);
            z2td.Data = mai.SerializeObject();
            return _ZipLoader.AddToQueue(z2td);
        }
        #endregion

        #region Load Methods
        /// <summary>
        /// Load Data from zip file.
        /// </summary>
        /// <param name="zipLocation">Location for zip archive.</param>
        /// <param name="filename">Archive file to load.</param>
        /// <returns>Guid for callback.</returns>
        public Guid LoadData(String zipLocation, String filename)
        {
            Zip_v2_TaskData z2td = new Zip_v2_TaskData(zipLocation, filename, DataType.Data, StreamDirection.Read);
            return _ZipLoader.AddToQueue(z2td);
        }

        /// <summary>
        /// Load MangaInfo  from zip file.
        /// </summary>
        /// <param name="zipLocation">Location for zip archive.</param>
        /// <param name="filename">Archive file to load.</param>
        /// <returns>Guid for callback.</returns>
        public Guid LoadMangaInfo(String zipLocation)
        {
            Zip_v2_TaskData z2td = new Zip_v2_TaskData(zipLocation, MangaInfoConst.InfoFileName, DataType.MangaInfo, StreamDirection.Read);
            return _ZipLoader.AddToQueue(z2td);
        }

        /// <summary>
        /// Load MangaArchiveInfo  from zip file.
        /// </summary>
        /// <param name="zipLocation">Location for zip archive.</param>
        /// <param name="filename">Archive file to load.</param>
        /// <returns>Guid for callback.</returns>
        public Guid LoadMangaArchiveInfo(String zipLocation)
        {
            Zip_v2_TaskData z2td = new Zip_v2_TaskData(zipLocation, MangaArchiveConst.InfoFileName, DataType.MangaArchiveInfo, StreamDirection.Read);
            return _ZipLoader.AddToQueue(z2td);
        }
        #endregion

        #region QueuedBackgroundWorker Methods
        void _ZipLoader_DoTaskWork(object Sender, QueuedTask<Zip_v2_TaskData> Task)
        {
            Boolean FileExists = File.Exists(Task.Data.ZipFileLocation);
            switch (Task.Data.StreamDirection)
            {
                case StreamDirection.Write:
                    using (ZipFile zipFile = FileExists ? ZipFile.Read(Task.Data.ZipFileLocation, ZipReadOptions) : new ZipFile(Encoding.UTF8))
                    {
                        if (!FileExists)
                        {
                            zipFile.Comment = "Zip Archive for myManga";
                            zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                            zipFile.CompressionMethod = Ionic.Zip.CompressionMethod.Deflate;
                            zipFile.UseZip64WhenSaving = Zip64Option.AsNecessary;
                        }
                        switch (Task.Data.DataType)
                        {
                            default:
                            case DataType.Data:
                                zipFile.UpdateEntry(Task.Data.ZipFileName, Task.Data.Data);
                                break;

                            case DataType.MangaInfo:
                                zipFile.Comment = Task.Data.Data.DeserializeStream<MangaInfo>().Name;
                                zipFile.UpdateEntry(Task.Data.ZipFileName, Task.Data.Data);
                                break;

                            case DataType.MangaArchiveInfo:
                                zipFile.Comment = Task.Data.Data.DeserializeStream<MangaArchiveInfo>().Name;
                                zipFile.UpdateEntry(Task.Data.ZipFileName, Task.Data.Data);
                                break;
                        }
                    }
                    break;

                case StreamDirection.Read:
                    if (FileExists)
                    {
                        using (ZipFile zipFile = ZipFile.Read(Task.Data.ZipFileLocation, ZipReadOptions))
                        {
                            if (zipFile.ContainsEntry(Task.Data.ZipFileName))
                            {
                                Task.Data.Data = new MemoryStream();
                                zipFile[Task.Data.ZipFileName].Extract(Task.Data.Data);
                                Task.Data.Data.Seek(0, SeekOrigin.Begin);
                            }
                        }
                    }
                    break;
            }
        }

        void _ZipLoader_TaskComplete(object Sender, QueuedTask<Zip_v2_TaskData> Task)
        {
            switch (Task.Data.StreamDirection)
            {
                case StreamDirection.Write:
                    OnEmptyReturn(this, Task.Guid);
                    break;

                case StreamDirection.Read:
                    switch (Task.Data.DataType)
                    {
                        default:
                        case DataType.Data:
                            MemoryStream _data = new MemoryStream();
                            Task.Data.Data.CopyTo(_data);
                            OnDataReturn(this, Task.Guid, _data);
                            break;

                        case DataType.MangaInfo:
                            MangaInfo mi = Task.Data.Data.DeserializeStream<MangaInfo>();
                            OnMangaInfoReturn(this, Task.Guid, mi);
                            break;

                        case DataType.MangaArchiveInfo:
                            MangaArchiveInfo mai = Task.Data.Data.DeserializeStream<MangaArchiveInfo>();
                            OnMangaArchiveInfoReturn(this, Task.Guid, mai);
                            break;
                    }
                    break;
            }
            Task.Data.Data.Close();
        }
        #endregion
    }
}

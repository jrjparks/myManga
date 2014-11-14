using Ionic.Zip;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.IO.Storage.Manager.BaseInterfaceClasses
{
    public class ZipStorage : StorageInterface
    {
        protected class ZipStorageObject
        {
            public String ArchiveFilename { get; set; }
            public String Filename { get; set; }
            public Stream Stream { get; set; }

            public DateTime CreatedTime { get; private set; }

            public ZipStorageObject(String ArchiveFilename, String Filename, Stream Stream)
            {
                this.ArchiveFilename = ArchiveFilename;
                this.Filename = Filename;
                this.Stream = Stream;

                CreatedTime = DateTime.Now;
            }
        }

        protected readonly ConcurrentQueue<ZipStorageObject> write_queue = new ConcurrentQueue<ZipStorageObject>();
        protected readonly ReadOptions ZipReadOptions = new ReadOptions();
        protected readonly Task write_consumer_task;
        protected Boolean run_queue = true;
        protected readonly UInt16 retry_count;

        public ZipStorage()
            : base()
        {
            this.write_consumer_task = new Task(WriteConsumer);
            this.write_consumer_task.Start();
            ZipReadOptions.Encoding = Encoding.UTF8;
        }

        public bool Write(string archive_filename, string filename, Stream stream)
        {
            return Write(archive_filename, stream, filename);
        }

        public bool Write(string filename, Stream stream, params object[] args)
        {
            ZipStorageObject zso = new ZipStorageObject(filename, (String)args[0], new MemoryStream());
            stream.CopyTo(zso.Stream);
            write_queue.Enqueue(zso);
            return true;
        }

        public Stream Read(string archive_filename, string filename, Stream stream)
        {
            return Read(archive_filename, stream, filename);
        }

        public Stream Read(string filename, params object[] args)
        {
            if (File.Exists(filename))
            {
                MemoryStream DataStream = new MemoryStream();
                using (Stream fstream = File.Open(filename, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
                {
                    using (ZipFile zipFile = ZipFile.Read(fstream, this.ZipReadOptions))
                    {
                        using (Stream ZipData = new MemoryStream())
                        {
                            zipFile[(String)args[0]].Extract(ZipData);
                            ZipData.Position = 0;
                            ZipData.CopyTo(DataStream);
                        }
                    }
                }
                DataStream.Position = 0;
                return DataStream;
            }
            return null;
        }

        public bool TryRead(string filename, out Stream stream, params object[] args)
        {
            try { stream = Read(filename, args); return true; }
            catch { stream = null; return false; }
        }

        public void Destroy()
        {
            this.run_queue = false;
            write_consumer_task.Wait(1000 * 10); // Wait up to 10 seconds for the write thread to end.
            write_consumer_task.Dispose();
        }

        /// <summary>
        /// Thread method to handel zip file writing
        /// </summary>
        protected void WriteConsumer()
        {
            ZipStorageObject write_item;
            while (run_queue)
            {
                while (write_queue.TryDequeue(out write_item))
                {
                    Boolean FileExists = (File.Exists(write_item.ArchiveFilename) && ZipFile.IsZipFile(write_item.ArchiveFilename));
                    String FileIOPath = Path.GetTempFileName();
                    try
                    {
                        using (Stream fstream = File.Open(write_item.ArchiveFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
                        {
                            using (ZipFile zipFile = FileExists ? ZipFile.Read(fstream, this.ZipReadOptions) : new ZipFile(Encoding.UTF8))
                            {
                                DateTime dt = DateTime.Now;
                                zipFile.Comment = String.Format("{0} - {1}", dt.ToLongDateString(), dt.ToLongTimeString());
                                zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                                zipFile.CompressionMethod = CompressionMethod.Deflate;

                                write_item.Stream.Seek(0, SeekOrigin.Begin);
                                zipFile.UpdateEntry(write_item.Filename, write_item.Stream);

                                zipFile.Save(FileIOPath);
                            }
                        }
                        File.Copy(FileIOPath, write_item.ArchiveFilename, true);
                        write_item.Stream.Close();
                    }
                    catch
                    {
                        // Reappend write_item if write fails
                        write_queue.Enqueue(write_item);
                        Thread.Sleep(1000);
                    }
                    File.Delete(FileIOPath);
                }
                Thread.Sleep(1000);
            }
        }
    }
}

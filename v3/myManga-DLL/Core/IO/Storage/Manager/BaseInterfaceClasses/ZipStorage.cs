using Ionic.Zip;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Core.IO;

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

                this.CreatedTime = DateTime.Now;
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
            zso.Stream.Seek(0, SeekOrigin.Begin);
            write_queue.Enqueue(zso);
            return true;
        }

        public Stream Read(string archive_filename, string filename)
        { return Read(archive_filename, filename as Object); }

        public Stream Read(string filename, params object[] args)
        {
            if (args.Length > 0)
            {
                String EntryName = args[0] as String;
                MemoryStream stream = null;
                using (Stream fstream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (ZipFile zipFile = ZipFile.Read(fstream, this.ZipReadOptions))
                    {
                        if (zipFile.ContainsEntry((String)args[0]))
                            using (Stream ZipData = new MemoryStream())
                            {
                                zipFile[(String)args[0]].Extract(ZipData);
                                ZipData.Seek(0, SeekOrigin.Begin);

                                stream = new MemoryStream();
                                ZipData.CopyTo(stream);
                                stream.Seek(0, SeekOrigin.Begin);
                            }
                    }
                }
                return stream;
            }
            return null;
        }

        public bool TryRead(string archive_filename, string filename, out Stream stream)
        { return TryRead(archive_filename, out stream, filename); }

        public bool TryRead(string filename, out Stream stream, params object[] args)
        {
            try { stream = Read(filename, args); return stream != null; }
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

            while (this.run_queue)
            {
                while (write_queue.TryDequeue(out write_item))
                {
                    try // Try to write to the Zip archive file
                    {
                        Stream zip_stream = new MemoryStream();
                        String DirName = Path.GetDirectoryName(write_item.ArchiveFilename).SafeFolder();

                        // Open file stream and check to see if it is a zip file
                        using (Stream fstream = File.Open(write_item.ArchiveFilename, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                        { fstream.CopyTo(zip_stream); zip_stream.Seek(0, SeekOrigin.Begin); }
                        Boolean IsZip = ZipFile.IsZipFile(zip_stream, false);
                        zip_stream.Seek(0, SeekOrigin.Begin);       // Seek back to the beginning of the file to read it.

                        using (ZipFile zipFile = IsZip ? ZipFile.Read(zip_stream, this.ZipReadOptions) : new ZipFile(Encoding.UTF8))
                        {   // Read existing or create a new zip file
                            DateTime dt = DateTime.Now;
                            zipFile.Comment = String.Format("Last updated at: {0} - {1}", dt.ToLongDateString(), dt.ToLongTimeString());
                            zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                            zipFile.UseZip64WhenSaving = Zip64Option.AsNecessary;
                            zipFile.AlternateEncoding = Encoding.ASCII;
                            zipFile.AlternateEncodingUsage = ZipOption.AsNecessary;

                            ZipEntry zipEntry = zipFile.UpdateEntry(write_item.Filename, write_item.Stream);
                            zipEntry.Comment = String.Format("Last updated at: {0} - {1}", write_item.CreatedTime.ToLongDateString(), write_item.CreatedTime.ToLongTimeString());

                            using (Stream fstream = File.Open(write_item.ArchiveFilename, FileMode.Truncate, FileAccess.Write, FileShare.Read))
                            { zipFile.Save(fstream); } // Overwrite old zip file with new data.
                        }
                        write_item.Stream.Close();
                    }
                    catch
                    {
                        // Reappend write_item if write fails and are within time limit of 30min
                        if (this.run_queue && DateTime.Now.Subtract(write_item.CreatedTime).Duration().Minutes < 30)
                        {
                            write_queue.Enqueue(write_item);
                            Thread.Sleep(1000);
                        }
                    }
                }
                Thread.Sleep(1000);
            }
        }
    }
}

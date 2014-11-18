using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;

namespace Core.IO.Storage.Manager.BaseInterfaceClasses
{
    public class FileStorage : StorageInterface
    {
        protected class FileStorageObject
        {
            public String Filename { get; set; }
            public Stream Stream { get; set; }

            public DateTime CreatedTime { get; private set; }

            public FileStorageObject(String Filename, Stream Stream)
            {
                this.Filename = Filename;
                this.Stream = Stream;

                CreatedTime = DateTime.Now;
            }
        }

        protected readonly ConcurrentQueue<FileStorageObject> write_queue = new ConcurrentQueue<FileStorageObject>();
        protected readonly Task write_consumer_task;
        protected Boolean run_queue = true;

        public FileStorage()
            : base()
        {
            this.write_consumer_task = new Task(WriteConsumer);
            this.write_consumer_task.Start();
        }

        public bool Write(string filename, Stream stream, params object[] args)
        {
            write_queue.Enqueue(new FileStorageObject(filename, stream));
            return true;
        }

        public Stream Read(string filename, params object[] args)
        {
            Stream stream = new MemoryStream();
            using (Stream fstream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Byte[] buffer = new Byte[8 * 1024]; Int32 length;
                while ((length = fstream.Read(buffer, 0, buffer.Length)) > 0)
                    stream.Write(buffer, 0, length);
            }
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
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
        /// Thread method to handel file writing
        /// </summary>
        protected void WriteConsumer()
        {
            FileStorageObject write_item;
            while (run_queue)
            {
                while (write_queue.TryDequeue(out write_item))
                {
                    String FileIOPath = Path.GetTempFileName();
                    try
                    {
                        using (Stream fstream = File.Open(FileIOPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                        {
                            write_item.Stream.Seek(0, SeekOrigin.Begin);
                            Byte[] buffer = new Byte[8 * 1024]; Int32 length; // 8MB buffer
                            while ((length = write_item.Stream.Read(buffer, 0, buffer.Length)) > 0) // Read to buffer
                                fstream.Write(buffer, 0, length); // Write buffer to file
                        }
                        File.Copy(FileIOPath, write_item.Filename, true);
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

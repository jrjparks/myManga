using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace myManga_App.IO.Local
{
    public sealed class ZipManager : IDisposable
    {
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        ~ZipManager()
        { Dispose(); }

        public void Dispose()
        {
            if (semaphore != null)
                semaphore.Dispose();
        }

        public Stream Read(String FileName, String EntryName)
        {
            Task<Stream> readTask = Task.Run(() => ReadAsync(FileName, EntryName));
            readTask.Wait();
            return readTask.Result;
        }

        public async Task<Stream> ReadAsync(String FileName, String EntryName)
        {
            try
            {
                await semaphore.WaitAsync();
                using (FileStream zipFile = new FileStream(FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    using (ZipArchive zipArchive = new ZipArchive(zipFile, ZipArchiveMode.Read))
                    {
                        ZipArchiveEntry entry = zipArchive.GetEntry(EntryName);
                        using (Stream entryStream = entry.Open())
                        {
                            Stream rtn = new MemoryStream();
                            await entryStream.CopyToAsync(rtn);
                            rtn.Seek(0, SeekOrigin.Begin);
                            return rtn;
                        }
                    }
                }
            }
            catch
            { return null; }
            finally
            { semaphore.Release(); }
        }

        public Stream UnsafeRead(String FileName, String EntryName)
        {
            try
            {
                using (FileStream zipFile = new FileStream(FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    using (ZipArchive zipArchive = new ZipArchive(zipFile, ZipArchiveMode.Read))
                    {
                        ZipArchiveEntry entry = zipArchive.GetEntry(EntryName);
                        using (Stream entryStream = entry.Open())
                        {
                            Stream rtn = new MemoryStream();
                            entryStream.CopyTo(rtn);
                            rtn.Seek(0, SeekOrigin.Begin);
                            return rtn;
                        }
                    }
                }
            }
            catch
            { return null; }
        }

        public Boolean Write(String FileName, String EntryName, Stream EntryStream)
        {
            Task<Boolean> writeTask = Task.Run(() => WriteAsync(FileName, EntryName, EntryStream));
            writeTask.Wait();
            return writeTask.Result;
        }

        public async Task<Boolean> WriteAsync(String FileName, String EntryName, Stream EntryStream)
        {
            try
            {
                await semaphore.WaitAsync();
                using (FileStream zipFile = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    using (ZipArchive zipArchive = new ZipArchive(zipFile, ZipArchiveMode.Update))
                    {
                        ZipArchiveEntry entry = zipArchive.GetEntry(EntryName);
                        if (!Equals(entry, null)) entry.Delete(); // Delete the existing entry if it exists.
                        entry = zipArchive.CreateEntry(EntryName, CompressionLevel.Fastest);
                        using (Stream entryStream = entry.Open())
                        {
                            EntryStream.Seek(0, SeekOrigin.Begin);
                            await EntryStream.CopyToAsync(entryStream);
                            return true;
                        }
                    }
                }
            }
            catch
            { throw; }
            finally
            { semaphore.Release(); }
        }

        public Boolean Delete(String FileName, String EntryName)
        {
            Task<Boolean> deleteTask = Task.Run(() => DeleteAsync(FileName, EntryName));
            deleteTask.Wait();
            return deleteTask.Result;
        }

        public async Task<Boolean> DeleteAsync(String FileName, String EntryName)
        {
            try
            {
                await semaphore.WaitAsync();
                using (FileStream zipFile = new FileStream(FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    using (ZipArchive zipArchive = new ZipArchive(zipFile, ZipArchiveMode.Update))
                    {
                        ZipArchiveEntry entry = zipArchive.GetEntry(EntryName);
                        entry.Delete();
                        return true;
                    }
                }
            }
            catch
            { throw; }
            finally
            { semaphore.Release(); }
        }

        public async Task<TResult> Retry<TResult>(Func<Task<TResult>> method, TimeSpan timeout)
        { return await Retry(method: method, timeout: timeout, delay: TimeSpan.FromSeconds(1), delayIncrement: TimeSpan.Zero); }
        public async Task<TResult> Retry<TResult>(Func<Task<TResult>> method, TimeSpan timeout, TimeSpan delay)
        { return await Retry(method: method, timeout: timeout, delay: delay, delayIncrement: TimeSpan.Zero); }
        public async Task<TResult> Retry<TResult>(Func<Task<TResult>> method, TimeSpan timeout, TimeSpan delay, TimeSpan delayIncrement)
        {
            Stopwatch watch = Stopwatch.StartNew();
            do
            {
                try { return await method(); }
                catch (OperationCanceledException ocex)
                { throw ocex; } // Handle OperationCanceledException and throw it.
                catch (Exception ex)
                {
                    // If the timeout has elapsed, throw the Exception.
                    if (watch.Elapsed >= timeout)
                        throw ex;

                    // await for the delay.
                    await Task.Delay(delay);

                    // If there is a delayIncrement and it's greater than 0 add it to the delay.
                    if (delayIncrement > TimeSpan.Zero)
                        delay.Add(delayIncrement);
                }
            }
            while (watch.Elapsed < timeout);
            // A timeout occurred.
            // return the default(TResult).
            return default(TResult);
        }
    }
}

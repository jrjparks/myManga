using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

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

        public IEnumerable<String> GetEntries(String FileName)
        {
            Task<IEnumerable<String>> getEntriesTask = Task.Run(() => GetEntriesAsync(FileName));
            getEntriesTask.Wait();
            return getEntriesTask.Result;
        }

        public async Task<IEnumerable<String>> GetEntriesAsync(String FileName)
        {
            try
            {
                await semaphore.WaitAsync();
                using (ZipArchive zipArchive = new ZipArchive(new FileStream(FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None), ZipArchiveMode.Read))
                { return from Entry in zipArchive.Entries select Entry.FullName; }
            }
            catch
            { return null; }
            finally
            { semaphore.Release(); }
        }

        public IEnumerable<String> UnsafeGetEntries(String FileName)
        {
            try
            {
                using (ZipArchive zipArchive = new ZipArchive(new FileStream(FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None), ZipArchiveMode.Read))
                { return from Entry in zipArchive.Entries select Entry.FullName; }
            }
            catch
            { return null; }
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
                using (ZipArchive zipArchive = new ZipArchive(new FileStream(FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None), ZipArchiveMode.Read))
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
            catch
            { return null; }
            finally
            { semaphore.Release(); }
        }

        public Stream UnsafeRead(String FileName, String EntryName)
        {
            try
            {
                using (ZipArchive zipArchive = new ZipArchive(new FileStream(FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None), ZipArchiveMode.Read))
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
                using (ZipArchive zipArchive = new ZipArchive(new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None), ZipArchiveMode.Update))
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
                using (ZipArchive zipArchive = new ZipArchive(new FileStream(FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None), ZipArchiveMode.Update))
                {
                    ZipArchiveEntry entry = zipArchive.GetEntry(EntryName);
                    entry.Delete();
                    return true;
                }
            }
            catch
            { throw; }
            finally
            { semaphore.Release(); }
        }
    }
}

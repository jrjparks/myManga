using Core.Other.Singleton;
using System;
using System.IO;

namespace Core.IO.Storage.Manager
{
    public class StorageManager<I, T>
        where I : IStorage<T>, new()
        where T : FileStorageInformationObject
    {
        public static StorageManager<I, T> Default { get { return Singleton<StorageManager<I, T>>.Instance; } }

        private readonly IStorage<T> storageInterface;
        protected IStorage<T> StorageInterface { get { return storageInterface; } }
        
        public StorageManager()
        { storageInterface = new I(); }

        public virtual void Write(String filename, Stream stream, params Object[] args)
        {
            StorageInterface.Write(filename, stream, args);
        }
    }
}

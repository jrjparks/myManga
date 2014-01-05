using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.IO.Storage.Manager
{
    public class StorageManager<I> where I : StorageInterface, new()
    {
        private readonly StorageInterface storageInterface;
        protected StorageInterface StorageInterface { get { return storageInterface; } }
        
        

        public StorageManager()
        { storageInterface = new I(); }

        public virtual void Write()
        {
            
        }
    }
}

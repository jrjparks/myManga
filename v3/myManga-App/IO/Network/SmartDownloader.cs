using Amib.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace myManga_App.IO.Network
{
    public class SmartDownloader
    {
        protected readonly SmartThreadPool smartThreadPool;
        protected readonly SynchronizationContext synchronizationContext;
        public Int32 Concurrency
        {
            get { return smartThreadPool.Concurrency; }
        }

        public SmartDownloader() : this(null) { }

        public SmartDownloader(STPStartInfo stpThredPool)
        {
            smartThreadPool = new SmartThreadPool(stpThredPool ?? new STPStartInfo() { MaxWorkerThreads = 5 });
            synchronizationContext = SynchronizationContext.Current;
        }
    }
}

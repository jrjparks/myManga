using Amib.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace myManga_App.IO.Network
{
    public class SmartChapterDownload
    {
        protected readonly SmartThreadPool smartThreadPool;
        protected readonly SynchronizationContext synchronizationContext;

        public SmartChapterDownload()
        {
            smartThreadPool = new SmartThreadPool();
            synchronizationContext = SynchronizationContext.Current;
        }
    }
}

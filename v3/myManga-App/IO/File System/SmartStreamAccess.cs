using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace myManga_App.IO.File_System
{
    public class SmartStreamAccess
    {
        protected readonly SynchronizationContext synchronizationContext;
        protected readonly ReaderWriterLock streamLock;

        public SmartStreamAccess()
            : base()
        {
            synchronizationContext = SynchronizationContext.Current;
            streamLock = new ReaderWriterLock();
        }

        protected virtual void WaitToWrite()
        { streamLock.AcquireWriterLock(-1); }

        protected virtual void DoneWrite()
        { streamLock.ReleaseWriterLock(); }

        protected virtual void WaitToRead()
        { streamLock.AcquireReaderLock(-1); }

        protected virtual void DoneRead()
        { streamLock.ReleaseReaderLock(); }
    }
}

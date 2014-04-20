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
    public class SmartFileStreamAccess : FileStream
    {
        protected readonly EventWaitHandle waitHandle;

        public SmartFileStreamAccess(string path, FileMode mode)
            : base(path, mode)
        {
            waitHandle = new EventWaitHandle(true, EventResetMode.AutoReset, Name);
            waitHandle.WaitOne();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            waitHandle.Set();
        }
    }
}

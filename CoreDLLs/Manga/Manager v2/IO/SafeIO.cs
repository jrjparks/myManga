using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BakaBox.Controls.Threading;
using System.IO;
using System.Threading;
using System.ComponentModel;

namespace Manga.Manager_v2.IO
{
    public class SafeIO
    {
        #region Events
        public delegate void IODelegate(Object Sender, IOData ioData);
        public event EventHandler<IOData> IOResponse;
        protected virtual void OnIOResponse(IOData e)
        {
            if (IOResponse != null)
                IOResponse(this, e);
            if (IOResponse != null)
            {
                if (SyncContext == null)
                    IOResponse(this, e);
                else
                    foreach (EventHandler<IOData> del in IOResponse.GetInvocationList())
                        SyncContext.Post((s) => del(this, (IOData)s), e);
            }
        }
        #endregion

        #region Fields
        private SynchronizationContext SyncContext { get; set; }

        private QueuedBackgroundWorker<IOData> _Worker;
        private QueuedBackgroundWorker<IOData> Worker
        {
            get
            {
                if (_Worker == null)
                    _Worker = new QueuedBackgroundWorker<IOData>();
                return _Worker;
            }
        }
        #endregion

        #region Methods
        #region Public
        public SafeIO()
        {
            SyncContext = SynchronizationContext.Current;
        }
        #endregion
        #region Private
        private void Do_IO(Object sender, DoWorkEventArgs e)
        {
            if (e.Argument is IOData)
            {
                using (IOData ioData = e.Argument as IOData)
                {
                    FileInfo file = new FileInfo(ioData.Path);
                    switch (ioData.Mode)
                    {
                        default:
                        case IOMode.Read:
                            using (Stream io = file.OpenRead())
                            {
                                io.Seek(0, SeekOrigin.Begin);
                                io.CopyTo(ioData.DataStream);
                                io.Close();
                            }
                            break;

                        case IOMode.Write:
                            using (Stream io = file.OpenWrite())
                            {
                                io.Seek(0, SeekOrigin.Begin);
                                ioData.DataStream.CopyTo(io);
                                io.Flush();
                            }
                            break;
                    }
                }
            }
        }
        #endregion
        #endregion
    }

    public class IOData : EventArgs, IDisposable
    {
        public Guid Id
        {
            get;
            set;
        }

        public String Path
        {
            get;
            set;
        }

        public Stream DataStream
        {
            get;
            set;
        }

        public IOMode Mode
        {
            get;
            set;
        }

        public IOData()
        {
            this.Id = Guid.NewGuid();
            this.Mode = IOMode.Read;
        }

        public IOData(Stream DataStream)
        {
            this.Id = Guid.NewGuid();
            this.DataStream = DataStream;
            this.Mode = IOMode.Write;
        }

        public void Dispose()
        {
            if (DataStream != null)
                DataStream.Dispose();
        }
    }

    public enum IOMode
    {
        Read, Write
    }
}

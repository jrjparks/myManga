using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BakaBox.Controls.Threading;
using BakaBox.Tasks;
using System.IO;
using System.Threading;
using System.ComponentModel;

namespace BakaBox.SafeIO
{
    public class SafeFile
    {
        #region Events
        public delegate void FileDelegate(Object Sender, FileData fileData);
        public event EventHandler<FileData> FileResponse;
        protected virtual void OnFileResponse(FileData e)
        {
            if (FileResponse != null)
            {
                if (SyncContext == null)
                    FileResponse(this, e);
                else
                    foreach (EventHandler<FileData> del in FileResponse.GetInvocationList())
                        SyncContext.Post((s) => del(this, (FileData)s), e);
            }
        }
        #endregion

        #region Fields
        private SynchronizationContext SyncContext { get; set; }

        private QueuedBackgroundWorker<FileData> _Worker;
        private QueuedBackgroundWorker<FileData> Worker
        {
            get
            {
                if (_Worker == null)
                    _Worker = new QueuedBackgroundWorker<FileData>();
                return _Worker;
            }
        }
        #endregion

        #region Methods
        #region Public
        public SafeFile()
        {
            SyncContext = SynchronizationContext.Current;
        }

        public Guid SubmitRead(String Path)
        {
            return Worker.AddToQueue(new FileData(Path));
        }

        public Guid SubmitWrite(String Path, Stream Data)
        {
            return Worker.AddToQueue(new FileData(Path, Data));
        }

        public Guid SubmitFile(FileData fileData)
        {
            return Worker.AddToQueue(fileData);
        }
        #endregion

        #region Private
        private void Do_IO(Object sender, DoWorkEventArgs e)
        {
            if (e.Argument is FileData)
            {
                using (FileData fileData = e.Argument as FileData)
                {
                    fileData.State = State.Active;
                    OnFileResponse(fileData);
                    FileInfo file = new FileInfo(fileData.Path);
                    try
                    {
                        switch (fileData.Mode)
                        {
                            default:
                            case BakaBox.Tasks.FileMode.Read:
                                using (Stream io = file.OpenRead())
                                {
                                    io.Seek(0, SeekOrigin.Begin);
                                    fileData.DataStream.Seek(0, SeekOrigin.Begin);
                                    io.CopyTo(fileData.DataStream);
                                    io.Close();
                                }
                                break;

                            case BakaBox.Tasks.FileMode.Write:
                                using (Stream io = file.OpenWrite())
                                {
                                    fileData.DataStream = new MemoryStream();
                                    io.Seek(0, SeekOrigin.Begin);
                                    fileData.DataStream.Seek(0, SeekOrigin.Begin);
                                    fileData.DataStream.CopyTo(io);
                                    io.Flush();
                                    fileData.DataStream.Close();
                                }
                                break;
                        }
                        fileData.State = State.Completed;
                        OnFileResponse(fileData);
                    }
                    catch (Exception ex)
                    {
                        fileData.State = State.CompletedWithError;
                        fileData.Error = ex;
                        if (fileData.DataStream != null)
                            fileData.DataStream.Dispose();
                        OnFileResponse(fileData);
                    }
                }
            }
        }
        #endregion
        #endregion
    }
}

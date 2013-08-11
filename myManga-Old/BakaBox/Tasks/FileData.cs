using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BakaBox.Tasks
{
    public class FileData : EventArgs, IDisposable
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

        public FileMode Mode
        {
            get;
            set;
        }

        public State State
        {
            get;
            set;
        }

        public Exception Error
        {
            get;
            set;
        }

        public FileData()
        {
            this.Id = Guid.NewGuid();
            this.Mode = FileMode.Read;
            this.State = Tasks.State.Pending;
        }

        public FileData(String Path)
            : this()
        {
            this.Path = Path;
        }

        public FileData(String Path, Stream DataStream)
            : this(Path)
        {
            this.Mode = FileMode.Write;
            this.DataStream = DataStream;
        }

        public void Dispose()
        {
            if (DataStream != null)
                DataStream.Dispose();
        }
    }
}

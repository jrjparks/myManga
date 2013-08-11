using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IMangaSite
{
    public class DownloadRequest : EventArgs, IDisposable
    {
        public DownloadRequest()
        {
            Id = Guid.NewGuid();
        }

        public void Dispose()
        {
            
        }

        public Guid Id
        {
            get;
            set;
        }

        public String RemoteURL
        {
            get;
            set;
        }
    }
}

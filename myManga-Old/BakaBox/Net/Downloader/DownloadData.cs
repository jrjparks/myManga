using System;
using System.IO;
using BakaBox.Tasks;
using System.Net;
using System.Text;

namespace BakaBox.Net.Downloader
{
    public class DownloadData : EventArgs
    {
        public DownloadData()
        {
            Id = Guid.NewGuid();
            State = State.Pending;
            WebHeaders = new WebHeaderCollection();
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

        public Stream ResultStream
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

        public WebHeaderCollection WebHeaders
        {
            get;
            set;
        }

        public Encoding WebEncoding
        {
            get;
            set;
        }
    }
}

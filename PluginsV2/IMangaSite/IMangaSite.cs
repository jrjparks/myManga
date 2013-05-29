using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BakaBox.Tasks;
using System.IO;

namespace IMangaSite
{
    public interface IMangaSite
    {
        #region Properties
        Dictionary<Guid, Action<Stream>> RequestCallbackLink;
        #endregion

        #region Events
        // Use Requests to for transactions
        event EventHandler<DownloadRequest> DownloadRequested;
        event EventHandler<FileData> FileIORequested;

        event EventHandler<List<Object>> InfoEvent;

        event EventHandler<List<Object>> ChapterListEvent;
        event EventHandler<List<Object>> ChapterImageListEvent;
        #endregion

        #region Methods
        public Guid RequestInfo();

        public Guid RequestChapterList();
        public Guid RequestChapterImageList();

        void RequestCallbackLink(Guid Request, Action<Stream> Callback);
        #endregion
    }
}

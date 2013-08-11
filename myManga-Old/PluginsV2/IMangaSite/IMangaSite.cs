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
        #endregion

        #region Events
        // Use Requests to for transactions
        event EventHandler<DownloadRequest> DownloadRequested;
        event EventHandler<FileData> FileIORequested;

        event EventHandler InfoEvent;

        event EventHandler ChapterListEvent;
        event EventHandler ChapterImageListEvent;
        #endregion

        #region Methods
        IMangaSiteDataAttribute IMangaSiteData { get; }

        Guid RequestInfo(String InfoURL);

        Guid RequestChapterList();
        Guid RequestChapterImageList();

        Object ParseResponse(Stream Content);
        #endregion
    }
}

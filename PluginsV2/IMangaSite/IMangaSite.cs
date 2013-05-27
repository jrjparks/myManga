using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BakaBox.Tasks;

namespace IMangaSite
{
    public interface IMangaSite
    {
        #region Events
        // Use Requests to for transactions
        event EventHandler<DownloadRequest> DownloadRequested;
        event EventHandler<FileData> FileIORequested;
        #endregion

        #region Methods
        #endregion
    }
}

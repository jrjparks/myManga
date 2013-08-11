using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Manga.Manager_v2.Tasks
{
    public class DownloadImageTask : Task
    {
        private String remoteUrl, localPath, fileName;

        public String RemoteUrl
        {
            get { return remoteUrl; }
            set { remoteUrl = value; }
        }

        public String LocalPath
        {
            get { return localPath; }
            set { localPath = value; }
        }

        public String FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }
    }
}

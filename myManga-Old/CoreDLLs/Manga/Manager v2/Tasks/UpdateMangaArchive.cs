using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Manga.Manager_v2.Tasks
{
    public class UpdateMangaArchive : Task
    {
        private String localPath, mizaPath;

        public String LocalPath
        {
            get { return localPath; }
            set { localPath = value; }
        }

        public String MizaPath
        {
            get { return mizaPath; }
            set { mizaPath = value; }
        }
    }
}

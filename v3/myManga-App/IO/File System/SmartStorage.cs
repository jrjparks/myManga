using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using myMangaSiteExtension.Objects;
using Core.IO;
using System.IO;
using System.Collections.Concurrent;

namespace myManga_App.IO.File_System
{
    public sealed class SmartStorage
    {
        protected readonly SynchronizationContext synchronizationContext;
        protected readonly App App = App.Current as App;
        protected readonly ConcurrentQueue<Core.IO.KeyValuePair<String, String>> writeQuery;

        public SmartStorage()
        {
            synchronizationContext = SynchronizationContext.Current;
            writeQuery = new ConcurrentQueue<Core.IO.KeyValuePair<String, String>>();
        }

        public void Write(Stream stream, String filename)
        {
            String tmpFilename = Path.GetTempFileName();
            using (FileStream file = File.OpenWrite(tmpFilename))
            {
                stream.Seek(0, SeekOrigin.Begin);
                file.CopyTo(file);
                stream.Close();
            }
            Write(tmpFilename, filename);
        }

        public void Write<T>(T Object, String filename) where T : class
        {
            String tmpFilename = Path.GetTempFileName();
            Object.SaveObject(tmpFilename, SaveType.XML);
            Write(tmpFilename, filename);
        }
        public void Write(String path, String filename)
        { writeQuery.Enqueue(new Core.IO.KeyValuePair<String, String>(filename, path)); }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myManga_App.IO.Network
{
    public sealed class SmartPageDownloader : SmartDownloader
    {
        public delegate void PageEventHandler(Object sender, String filename, Stream stream);
        public event PageEventHandler PageObjectComplete;
        private void OnPageObjectComplete(String filename, Stream stream)
        {
            if (PageObjectComplete != null)
            {
                if (synchronizationContext == null)
                    PageObjectComplete(this, filename, stream);
                else
                    foreach (PageEventHandler del in PageObjectComplete.GetInvocationList())
                        synchronizationContext.Post((e) => del(this, filename, stream), null);
            }
        }
    }
}

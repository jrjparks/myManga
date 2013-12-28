using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using myMangaSiteExtension.Objects;

namespace myManga_App.IO.File_System
{
    public sealed class SmartStorage
    {
        protected readonly SynchronizationContext synchronizationContext;
        protected readonly App App = App.Current as App;

        public SmartStorage()
        {
            synchronizationContext = SynchronizationContext.Current;
        }
    }
}

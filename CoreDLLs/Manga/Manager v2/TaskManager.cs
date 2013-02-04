using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BakaBox.Controls.Threading;

namespace Manga.Manager_v2
{
    public class TaskManager
    {
        private readonly QueuedBackgroundWorker<Tasks.Task> taskManager;

        public TaskManager()
        {
            taskManager = new QueuedBackgroundWorker<Tasks.Task>();
        }
    }
}

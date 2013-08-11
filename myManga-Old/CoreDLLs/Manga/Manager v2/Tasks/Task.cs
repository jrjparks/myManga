using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Manga.Manager_v2.Tasks
{
    public class Task
    {
        private readonly DateTime createdDT;
        private DateTime startedDT, completedDT;

        private short progress;
        private String progressStatus, taskName;
        
        public DateTime CreatedDateTime
        {
            get { return createdDT; }
        }

        public DateTime StartedDateTime
        {
            get { return startedDT; }
            set { startedDT = value; }
        }

        public DateTime CompletedDateTime
        {
            get { return completedDT; }
            set { completedDT = value; }
        }

        public short Progress
        {
            get { return progress; }
            set { progress = value; }
        }

        public String ProgressStatus
        {
            get { return progressStatus; }
            set { progressStatus = value; }
        }

        public String TaskName
        {
            get { return taskName; }
            set { taskName = value; }
        }

        public Task()
        {
            createdDT = DateTime.Now;
        }

        /// <summary>
        /// Create a clone of Task<T>
        /// </summary>
        /// <param name="t"></param>
        public Task(Task t)
        {
            createdDT = t.CreatedDateTime;
            startedDT = t.StartedDateTime;
            completedDT = t.CompletedDateTime;
            progress = t.Progress;
            progressStatus = t.ProgressStatus;
            taskName = t.TaskName;
        }
    }
}

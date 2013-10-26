using System.Collections.Generic;
using Amib.Threading;

namespace myManga_App.IO
{
    public class SmartGroupObject
    {
        public IWorkItemsGroup WorkItemsGroup { get; set; }
        protected List<IWorkItemResult> workItemResults;
        public List<IWorkItemResult> WorkItemResults
        {
            get { return workItemResults ?? (workItemResults = new List<IWorkItemResult>()); }
            set { workItemResults = value; }
        }

        public SmartGroupObject() { }
        public SmartGroupObject(IWorkItemsGroup WorkItemsGroup)
        { this.WorkItemsGroup = WorkItemsGroup; }
        public SmartGroupObject(IWorkItemsGroup WorkItemsGroup, List<IWorkItemResult> WorkItemResults)
        {
            this.WorkItemsGroup = WorkItemsGroup;
            this.WorkItemResults = WorkItemResults;
        }
    }

    public class SmartGroupObject<TResult>
    {
        public IWorkItemsGroup WorkItemsGroup { get; set; }
        protected List<IWorkItemResult<TResult>> workItemResults;
        public List<IWorkItemResult<TResult>> WorkItemResults
        {
            get { return workItemResults ?? (workItemResults = new List<IWorkItemResult<TResult>>()); }
            set { workItemResults = value; }
        }

        public SmartGroupObject() { }
        public SmartGroupObject(IWorkItemsGroup WorkItemsGroup)
        { this.WorkItemsGroup = WorkItemsGroup; }
        public SmartGroupObject(IWorkItemsGroup WorkItemsGroup, List<IWorkItemResult<TResult>> WorkItemResults)
        {
            this.WorkItemsGroup = WorkItemsGroup;
            this.WorkItemResults = WorkItemResults;
        }
    }
}

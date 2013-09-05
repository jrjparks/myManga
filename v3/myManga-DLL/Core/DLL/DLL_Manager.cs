using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Core.DLL
{
    [DebuggerStepThrough]
    public class DLL_Manager<T, C> where C : ICollection<T>, new()
    {
        protected SynchronizationContext SyncContext { get; set; }

        protected String appDomainName { get; set; }
        public String AppDomainName
        {
            get
            {
                if (appDomainName == null || appDomainName == String.Empty)
                    appDomainName = String.Format("{0}-AppDomain", typeof(T).Name);
                return appDomainName;
            }
            set
            {
                appDomainName = value;
                if (DLLAppDomain != null)
                {
                    AppDomain.Unload(DLLAppDomain);
                    DLLAppDomain = null;
                }
                if (DLLCollection.Count > 0)
                    DLLCollection.Clear();
                DLLAppDomain = AppDomain.CreateDomain(AppDomainName);
            }
        }

        protected AppDomain dllAppDomain { get; set; }
        public AppDomain DLLAppDomain
        {
            get { return dllAppDomain ?? (dllAppDomain = AppDomain.CreateDomain(AppDomainName)); }
            set { dllAppDomain = value; }
        }
        public C DLLCollection { get; set; }

        public DLL_Manager()
        {
            SyncContext = SynchronizationContext.Current;
            DLLCollection = new C();
        }

        public void LoadDLL(String DLLPath)
        {
            FileAttributes fileAttr = File.GetAttributes(DLLPath);
            if ((fileAttr & FileAttributes.Directory) == FileAttributes.Directory)
                foreach (T Class in DLL_Loader<T>.LoadDirectory(DLLPath))
                    DLLCollection.Add(Class);
            else
                foreach (T Class in DLL_Loader<T>.LoadDLL(DLLPath))
                    DLLCollection.Add(Class);
        }

        public void Unload()
        {
            if (DLLAppDomain != null)
            {
                AppDomain.Unload(DLLAppDomain);
                DLLAppDomain = null;
            }
            if (DLLCollection.Count > 0)
                DLLCollection.Clear();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;

namespace Core.DLL
{
    public class DLL_Manager<T, C> where C : ICollection<T>, new()
    {
        protected SynchronizationContext SyncContext { get; set; }

        protected String _AppDomainName { get; set; }
        public String AppDomainName
        {
            get
            {
                if (_AppDomainName == null || _AppDomainName == String.Empty)
                    _AppDomainName = String.Format("{0}-AppDomain", typeof(T).Name);
                return _AppDomainName;
            }
            set
            {
                _AppDomainName = value;
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

        protected AppDomain _DLLAppDomain { get; set; }
        public AppDomain DLLAppDomain
        {
            get
            {
                if (_DLLAppDomain == null)
                    _DLLAppDomain = AppDomain.CreateDomain(AppDomainName);
                return _DLLAppDomain;
            }
            set { _DLLAppDomain = value; }
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
    }
}

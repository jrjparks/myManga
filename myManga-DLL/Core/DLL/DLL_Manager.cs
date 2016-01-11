using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Core.DLL
{
    /// <summary>
    /// Class loads and manages DLLs
    /// </summary>
    /// <typeparam name="ItemType">Class of DLL Item</typeparam>
    /// <typeparam name="CollectionType">Class of Collection for DLLs</typeparam>
    [DebuggerStepThrough]
    public class DLL_Manager<ItemType, CollectionType> where CollectionType : ICollection<ItemType>, new()
    {
        public String AppDomainName
        { get; private set; }

        public AppDomain DllAppDomain
        { get; protected set; }

        public CollectionType DLLCollection
        { get; set; }

        public DLL_Manager() : this(null) { }

        public DLL_Manager(String AppDomainName)
        {
            if (Equals(AppDomainName, null))
                AppDomainName = String.Format("{0}-AppDomain", typeof(ItemType).Name);
            this.AppDomainName = AppDomainName;
            DllAppDomain = AppDomain.CreateDomain(AppDomainName);
            DLLCollection = new CollectionType();
        }

        public void LoadDLL(String DLLPath, String Filter = "*.dll", SearchOption DirectorySearchOption = SearchOption.TopDirectoryOnly)
        {
            FileAttributes fileAttr = File.GetAttributes(DLLPath);
            if ((fileAttr & FileAttributes.Directory) == FileAttributes.Directory)
                foreach (ItemType Class in DLL_Loader<ItemType>.LoadDirectory(DLLPath, Filter, DirectorySearchOption))
                    DLLCollection.Add(Class);
            else
                foreach (ItemType Class in DLL_Loader<ItemType>.LoadDLL(DLLPath))
                    DLLCollection.Add(Class);
        }

        public void Unload()
        {
            if (DLLCollection.Count > 0)
                DLLCollection.Clear();
            if (DllAppDomain != null)
            {
                AppDomain.Unload(DllAppDomain);
                DllAppDomain = null;
            }
        }
    }
}

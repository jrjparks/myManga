using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace myManga_App.IO.DLL
{
    /// <summary>
    /// Class loads and manages DLLs
    /// </summary>
    /// <typeparam name="ItemType">Class of DLL Item</typeparam>
    /// <typeparam name="CollectionType">Class of Collection for DLLs</typeparam>
    [DebuggerStepThrough]
    public sealed class Manager<ItemType, CollectionType>
        where CollectionType : ICollection<ItemType>, new()
    {
        #region Properties
        public String ManagerAppDomainName
        { get; private set; }

        public AppDomain ManagerAppDomain
        { get; private set; }

        public CollectionType DLLCollection
        { get; set; }
        #endregion

        #region Constructor
        public Manager() : this(null) { }

        public Manager(String AppDomainName)
        {
            if (Equals(AppDomainName, null))
                AppDomainName = String.Format("{0}-AppDomain", typeof(ItemType).Name);
            this.ManagerAppDomainName = AppDomainName;
            ManagerAppDomain = AppDomain.CreateDomain(AppDomainName);
            DLLCollection = new CollectionType();
        }
        #endregion

        #region Methods
        public void Load(String Path, String Filter = "*.dll", SearchOption DirectorySearchOption = SearchOption.TopDirectoryOnly)
        {
            FileAttributes Attributes = File.GetAttributes(Path);
            if (Attributes.HasFlag(FileAttributes.Directory))
                foreach (ItemType Class in Loader<ItemType>.LoadDirectory(Path, Filter, DirectorySearchOption))
                    DLLCollection.Add(Class);
            else
                foreach (ItemType Class in Loader<ItemType>.Load(Path))
                    DLLCollection.Add(Class);
        }

        public void Unload()
        {
            if (DLLCollection.Count > 0) DLLCollection.Clear();
            if (Equals(ManagerAppDomain, null))
            {
                AppDomain.Unload(ManagerAppDomain);
                ManagerAppDomain = null;
            }
        }
        #endregion
    }
}

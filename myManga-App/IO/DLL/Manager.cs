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

        private Action<ICollection<ItemType>, ItemType> AddToCollectionAction
        { get; set; }
        #endregion

        #region Constructor
        public Manager() : this(null, null) { }

        public Manager(String AppDomainName, Action<ICollection<ItemType>, ItemType> AddToCollectionAction)
        {
            if (Equals(AppDomainName, null))
                AppDomainName = String.Format("{0}-AppDomain", typeof(ItemType).Name);

            if (Equals(AddToCollectionAction, null))
                AddToCollectionAction = DefaultAddToCollectionAction;
            this.AddToCollectionAction = AddToCollectionAction;

            ManagerAppDomainName = AppDomainName;
            ManagerAppDomain = AppDomain.CreateDomain(AppDomainName);
            DLLCollection = new CollectionType();
        }
        #endregion

        #region Methods
        public void Load(String Path, String Filter = "*.dll", SearchOption DirectorySearchOption = SearchOption.TopDirectoryOnly)
        {
            FileAttributes Attributes = File.GetAttributes(Path);
            if (Attributes.HasFlag(FileAttributes.Directory))
                foreach (ItemType Item in Loader<ItemType>.LoadDirectory(Path, Filter, DirectorySearchOption))
                    AddToCollectionAction(DLLCollection, Item);
            else
                foreach (ItemType Item in Loader<ItemType>.Load(Path))
                    AddToCollectionAction(DLLCollection, Item);
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

        private void DefaultAddToCollectionAction(ICollection<ItemType> Collection, ItemType Item)
        { Collection.Add(Item); }
        #endregion
    }
}

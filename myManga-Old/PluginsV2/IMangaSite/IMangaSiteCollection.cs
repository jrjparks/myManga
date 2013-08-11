using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections;

namespace IMangaSite
{
    [DebuggerStepThrough]
    public class IMangaSiteCollection : ICollection<IMangaSite>
    {
        #region IEnumerable<MangaArchiveInfoEntry> Members
        public IEnumerator<IMangaSite> GetEnumerator()
        {
            return new IMangaSiteEnumerable(this);
        }
        #endregion

        #region IEnumerable Members
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new IMangaSiteEnumerable(this);
        }
        #endregion

        #region IMangaSiteCollection
        private List<IMangaSite> Items { get; set; }

        public IMangaSiteCollection()
        {
            Items = new List<IMangaSite>();
        }
        public IMangaSiteCollection(Int32 capacity)
        {
            Items = new List<IMangaSite>(capacity);
        }
        public IMangaSiteCollection(IEnumerable<IMangaSite> collection)
        {
            Items = new List<IMangaSite>(collection);
        }
        #endregion

        #region ICollection Members
        public void Add(IMangaSite item)
        {
            Items.Add(item);
        }

        public void AddRange(params IMangaSite[] collection)
        {
            Items.AddRange(collection);
        }

        public void AddRange(IEnumerable<IMangaSite> collection)
        {
            Items.AddRange(collection);
        }

        public void Clear()
        {
            Items.Clear();
        }

        public bool Contains(IMangaSite item)
        {
            return Items.Contains(item);
        }

        public bool Contains(IMangaSite item, IEqualityComparer<IMangaSite> comparer)
        {
            return Items.Contains(item, comparer);
        }

        public void CopyTo(IMangaSite[] array)
        {
            Items.CopyTo(array);
        }

        public void CopyTo(IMangaSite[] array, int arrayIndex)
        {
            Items.CopyTo(array, arrayIndex);
        }

        public void CopyTo(int index, IMangaSite[] array, int arrayIndex, int count)
        {
            Items.CopyTo(index, array, arrayIndex, count);
        }

        public int Count
        {
            get { return Items.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(IMangaSite item)
        {
            return Items.Remove(item);
        }
        #endregion

        #region ICollection Custom Members
        public IMangaSite this[Int32 index]
        {
            get { return Items[index]; }
            set { Items[index] = value; }
        }
        public Int32 this[IMangaSite item]
        {
            get { return Items.IndexOf(item); }
        }
        public IMangaSite this[String name]
        {
            get
            {
                IMangaSite iMangaSite = Items.FirstOrDefault(i => i.IMangaSiteData.Name.Equals(name));
                if (!iMangaSite.Equals(default(IMangaSite)))
                    return iMangaSite;
                return null;
            }
        }
        #endregion
    }

    [DebuggerStepThrough]
    public class IMangaSiteEnumerable : IEnumerator<IMangaSite>
    {
        private IMangaSiteCollection collection;
        private Int32 curIndex;
        private IMangaSite curMangaArchiveInfoEntry;

        public IMangaSiteEnumerable(IMangaSiteCollection Collection)
        {
            collection = Collection;
            curIndex = -1;
            curMangaArchiveInfoEntry = default(IMangaSite);
        }

        public bool MoveNext()
        {
            //Avoids going beyond the end of the collection.
            if (++curIndex >= collection.Count())
            {
                return false;
            }
            else
            {
                // Set current box to next item in collection.
                curMangaArchiveInfoEntry = collection[curIndex];
            }
            return true;
        }

        public void Reset() { curIndex = -1; }

        void IDisposable.Dispose() { }

        public IMangaSite Current
        {
            get { return curMangaArchiveInfoEntry; }
        }


        object IEnumerator.Current
        {
            get { return Current; }
        }
    }
}

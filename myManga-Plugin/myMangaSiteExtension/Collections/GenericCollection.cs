using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace myMangaSiteExtension.Collections
{
    [Serializable, XmlRoot, DebuggerStepThrough]
    public class GenericCollection<T> : ICollection<T>
    {
        protected List<T> innerList;
        protected Boolean is_readonly;

        public GenericCollection()
        {
            innerList = new List<T>();
        }
        public GenericCollection(Int32 capacity)
        {
            innerList = new List<T>(capacity);
        }
        public GenericCollection(IEnumerable<T> collection)
        {
            innerList = new List<T>(collection);
        }

        public virtual T this[int index]
        {
            get { return innerList[index]; }
            set { innerList[index] = value; }
        }

        public virtual Int32 IndexOf(T item)
        {
            return innerList.IndexOf(item);
        }

        public virtual Int32 Count
        {
            get { return innerList.Count; }
        }

        public virtual Boolean IsReadOnly
        {
            get { return is_readonly; }
        }

        public virtual void Add(T item)
        {
            innerList.Add(item);
        }

        public virtual void AddRange(params T[] collection)
        {
            innerList.AddRange(collection);
        }

        public virtual void AddRange(IEnumerable<T> collection)
        {
            innerList.AddRange(collection);
        }

        public virtual Boolean Remove(T item)
        {
            return innerList.Remove(item);
        }

        public virtual Boolean Contains(T item)
        {
            return innerList.Contains(item);
        }

        public virtual void CopyTo(T[] array)
        {
            innerList.CopyTo(array);
        }

        public virtual void CopyTo(T[] array, Int32 index)
        {
            innerList.CopyTo(array, index);
        }

        public virtual void CopyTo(Int32 index, T[] array, Int32 arrayIndex, Int32 count)
        {
            innerList.CopyTo(index, array, arrayIndex, count);
        }

        public virtual void Clear()
        {
            innerList.Clear();
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            return new GenericEnumerator<T>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new GenericEnumerator<T>(this);
        }
    }
}

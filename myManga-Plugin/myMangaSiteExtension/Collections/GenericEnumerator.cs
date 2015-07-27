using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;

namespace myMangaSiteExtension.Collections
{
    [Serializable, XmlRoot, DebuggerStepThrough]
    public class GenericEnumerator<T> : IEnumerator<T>
    {
        protected GenericCollection<T> collection;
        protected Int32 index;
        protected T current;

        public GenericEnumerator()
        {
        }

        public GenericEnumerator(GenericCollection<T> collection)
        {
            this.collection = collection;
            index = -1;
            current = default(T);
        }

        public virtual T Current { get { return current; } }

        object IEnumerator.Current { get { return current; } }

        public virtual void Dispose()
        {
            collection = null;
            current = default(T);
            index = -1;
        }

        public virtual bool MoveNext()
        {
            if (++index >= collection.Count)
                return false;
            else
                current = collection[index];
            return true;
        }

        public virtual void Reset()
        {
            current = default(T);
            index = -1;
        }
    }
}

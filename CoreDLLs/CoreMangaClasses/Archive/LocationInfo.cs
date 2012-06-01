using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace Manga.Archive
{
    [XmlRoot("LocationInfo"), DebuggerStepThrough]
    public class LocationInfo
    {
        [XmlAttribute("File")]
        public String FileName
        { get; set; }
        [XmlAttribute("OnlineFolderPath")]
        public String OnlinePath
        { get; set; }
        [XmlAttribute("AltOnlineFolderPath")]
        public String AltOnlinePath
        { get; set; }

        [XmlIgnore]
        public String FullOnlinePath
        { get { return (OnlinePath != String.Empty) ? Path.Combine(OnlinePath, FileName) : String.Empty; } }
        [XmlIgnore]
        public String FullAltOnlinePath
        { get { return (AltOnlinePath != String.Empty) ? Path.Combine(AltOnlinePath ?? OnlinePath, FileName) : String.Empty; } }
    }

    [XmlRoot("LocationInfos"), DebuggerStepThrough]
    public class LocationInfoCollection : ICollection<LocationInfo>
    {
        private List<LocationInfo> Items { get; set; }

        public void ForEach(Action<LocationInfo> action)
        {
            Items.ForEach(action);
        }

        public LocationInfoCollection()
        {
            Items = new List<LocationInfo>();
        }

        public void TrimExcess()
        {
            Items.TrimExcess();
        }

        public Boolean TrueForAll(Predicate<LocationInfo> match)
        {
            return Items.TrueForAll(match);
        }

        #region ICollection<MangaArchiveInfoEntry> Members

        public LocationInfo this[Int32 index]
        {
            get
            {
                if (index >= Items.Count)
                    return Items[Items.Count - 1];
                else if (index < 0)
                    return Items[0];
                return Items[index];
            }
            set { Items[index] = value; }
        }

        public Int32 this[LocationInfo LocationInfo]
        {
            get { return Items.IndexOf(LocationInfo); }
        }

        public Int32 IndexOf(LocationInfo LocationInfo)
        { return Items.IndexOf(LocationInfo); }

        public void Add(LocationInfo item)
        {
            Items.Add(item);
        }

        public void Clear()
        {
            Items.Clear();
        }

        public bool Contains(LocationInfo item)
        {
            return Items.Contains(item);
        }

        public void CopyTo(LocationInfo[] array, int arrayIndex)
        {
            Items.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return Items.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(LocationInfo item)
        {
            return Items.Remove(item);
        }

        #endregion

        #region IEnumerable<MangaArchiveInfoEntry> Members

        public IEnumerator<LocationInfo> GetEnumerator()
        {
            return new LocationInfoEntryEnumerable(this);
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new LocationInfoEntryEnumerable(this);
        }

        #endregion
    }

    [DebuggerStepThrough]
    public class LocationInfoEntryEnumerable : IEnumerator<LocationInfo>
    {
        private LocationInfoCollection _collection;
        private Int32 curIndex;
        private LocationInfo curMangaArchiveInfoEntry;

        public LocationInfoEntryEnumerable(LocationInfoCollection Collection)
        {
            _collection = Collection;
            curIndex = -1;
            curMangaArchiveInfoEntry = default(LocationInfo);
        }

        public bool MoveNext()
        {
            //Avoids going beyond the end of the collection.
            if (++curIndex >= _collection.Count)
            {
                return false;
            }
            else
            {
                // Set current box to next item in collection.
                curMangaArchiveInfoEntry = _collection[curIndex];
            }
            return true;
        }

        public void Reset() { curIndex = -1; }

        void IDisposable.Dispose() { }

        public LocationInfo Current
        {
            get { return curMangaArchiveInfoEntry; }
        }


        object IEnumerator.Current
        {
            get { return Current; }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;

namespace Manga.Archive
{
    [XmlRoot("Entry"), DebuggerStepThrough]
    public class PageEntry
    {
        [XmlElement("LocationInfo")]
        public LocationInfo LocationInfo { get; set; }

        [XmlAttribute("Page")]
        public UInt32 PageNumber { get; set; }
        [XmlAttribute("Downloaded")]
        public Boolean Downloaded { get; set; }
        [XmlAttribute("FileSize")]
        public UInt32 FileSize { get; set; }
        
        public PageEntry()
        { LocationInfo = new LocationInfo(); Downloaded = false; }

        public override String ToString()
        {
            return String.Format("p{0}", PageNumber);
        }
    }

    [XmlRoot("Pages"), DebuggerStepThrough]
    public class PageEntryCollection : ICollection<PageEntry>
    {
        private List<PageEntry> Items { get; set; }

        public void ForEach(Action<PageEntry> action)
        {
            Items.ForEach(action);
        }

        public PageEntryCollection()
        {
            Items = new List<PageEntry>();
        }

        public void TrimExcess()
        {
            Items.TrimExcess();
        }

        public Boolean TrueForAll(Predicate<PageEntry> match)
        {
            return Items.TrueForAll(match);
        }

        #region PageEntry Members
        public UInt32 NumberOfUnDownloadedItems { get { return (UInt32)AllUnDownloadedItems.Count; } }
        public PageEntryCollection AllUnDownloadedItems
        {
            get
            {
                PageEntryCollection UnDownloadedItems = new PageEntryCollection();
                this.ForEach(
                    delegate(PageEntry Item)
                    {
                        if (!Item.Downloaded)
                            UnDownloadedItems.Add(Item);
                    });
                return UnDownloadedItems;
            }
        }
        public UInt32 NumberOfDownloadedItems { get { return (UInt32)AllDownloadedItems.Count; } }
        public PageEntryCollection AllDownloadedItems
        {
            get
            {
                PageEntryCollection DownloadedItems = new PageEntryCollection();
                this.ForEach(
                    delegate(PageEntry Item)
                    {
                        if (Item.Downloaded)
                            DownloadedItems.Add(Item);
                    });
                return DownloadedItems;
            }
        }

        public LocationInfoCollection DownloadLocations
        {
            get
            {
                LocationInfoCollection DownloadLocations = new LocationInfoCollection();
                this.ForEach(
                    delegate(PageEntry Item)
                    { DownloadLocations.Add(Item.LocationInfo); });
                return DownloadLocations;
            }
        }
        #endregion

        #region ICollection<MangaArchiveInfoEntry> Members

        public PageEntry this[Int32 index]
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

        public Int32 this[PageEntry MAIE]
        {
            get { return Items.IndexOf(MAIE); }
        }

        public Int32 IndexOfPage(UInt32 PageNumber)
        {
            Int32 _Index = 0;
            foreach (PageEntry _Page in Items)
                if (_Page.PageNumber.Equals(PageNumber))
                    break;
                else ++_Index;
            return _Index;
        }

        public PageEntry GetPageByNumber(UInt32 PageNumber)
        {
            foreach (PageEntry MAIE in Items)
            {
                if (MAIE.PageNumber.Equals(PageNumber))
                    return MAIE;
            }
            return null;
        }

        public PageEntry GetPageByFileName(String FileName)
        {
            foreach (PageEntry Page in Items)
            {
                if (Page.LocationInfo.FileName.Equals(FileName))
                    return Page;
            }
            return null;
        }

        public void Add(PageEntry item)
        {
            Items.Add(item);
        }

        public void Clear()
        {
            Items.Clear();
        }

        public bool Contains(PageEntry item)
        {
            return Items.Contains(item);
        }

        public bool Contains(UInt32 PageNumber)
        {
            foreach (PageEntry MAIE in Items)
            {
                if (MAIE.PageNumber.Equals(PageNumber))
                    return true;
            }
            return false;
        }

        public void CopyTo(PageEntry[] array, int arrayIndex)
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

        public bool Remove(PageEntry item)
        {
            return Items.Remove(item);
        }

        #endregion

        #region IEnumerable<MangaArchiveInfoEntry> Members

        public IEnumerator<PageEntry> GetEnumerator()
        {
            return new PageEntryEntryEnumerable(this);
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new PageEntryEntryEnumerable(this);
        }

        #endregion
    }

    [DebuggerStepThrough]
    public class PageEntryEntryEnumerable : IEnumerator<PageEntry>
    {
        private PageEntryCollection _collection;
        private Int32 curIndex;
        private PageEntry curMangaArchiveInfoEntry;

        public PageEntryEntryEnumerable(PageEntryCollection Collection)
        {
            _collection = Collection;
            curIndex = -1;
            curMangaArchiveInfoEntry = default(PageEntry);
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

        public PageEntry Current
        {
            get { return curMangaArchiveInfoEntry; }
        }


        object IEnumerator.Current
        {
            get { return Current; }
        }
    }
}

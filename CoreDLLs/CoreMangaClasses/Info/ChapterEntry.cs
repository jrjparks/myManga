using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;

namespace Manga.Info
{
    [XmlRoot("Entry"), DebuggerStepThrough]
    public class ChapterEntry
    {
        [XmlAttribute("Volume")]
        public UInt32 Volume { get; set; }
        [XmlAttribute("Chapter")]
        public UInt32 Chapter { get; set; }
        [XmlAttribute("SubChapter")]
        public UInt32 SubChapter { get; set; }
        [XmlAttribute("UrlLink")]
        public String UrlLink { get; set; }
        [XmlAttribute("Name")]
        public String Name { get; set; }

        public ChapterEntry()
        {
            Name = UrlLink = String.Empty;
            Volume = Chapter = SubChapter = UInt32.MinValue;
        }
        public ChapterEntry(ChapterEntry ChapterEntry)
        {
            Name = ChapterEntry.Name;
            UrlLink = ChapterEntry.UrlLink;
            Volume = ChapterEntry.Volume;
            Chapter = ChapterEntry.Chapter;
            SubChapter = ChapterEntry.SubChapter;
        }

        [XmlIgnore]
        public Boolean NameSpecified { get { return !Name.Equals(String.Empty); } }
        [XmlIgnore]
        public Boolean UrlLinkSpecified { get { return !UrlLink.Equals(String.Empty); } }
        [XmlIgnore]
        public Boolean VolumeSpecified { get { return !Volume.Equals(UInt32.MinValue); } }
        [XmlIgnore]
        public Boolean ChapterSpecified { get { return !Chapter.Equals(UInt32.MinValue); } }
        [XmlIgnore]
        public Boolean SubChapterSpecified { get { return !SubChapter.Equals(UInt32.MinValue); } }
    }

    [XmlRoot("Chapters"), DebuggerStepThrough]
    public class ChapterEntryCollection : ICollection<ChapterEntry>
    {
        private List<ChapterEntry> Items { get; set; }

        public void ForEach(Action<ChapterEntry> action)
        {
            Items.ForEach(action);
        }

        public ChapterEntryCollection()
        { Items = new List<ChapterEntry>(); }
        public ChapterEntryCollection(Int32 capacity)
        { Items = new List<ChapterEntry>(capacity); }
        public ChapterEntryCollection(IEnumerable<ChapterEntry> collection)
        { Items = new List<ChapterEntry>(collection); }

        public void TrimExcess()
        {
            Items.TrimExcess();
        }

        public Boolean TrueForAll(Predicate<ChapterEntry> match)
        {
            return Items.TrueForAll(match);
        }

        public void Reverse()
        { Items.Reverse(); }
        public void Reverse(Int32 index, Int32 count)
        { Items.Reverse(index, count); }

        public Int32 IndexOf(ChapterEntry ChapterEntry)
        { return Items.IndexOf(ChapterEntry); }
        public Int32 IndexOf(ChapterEntry ChapterEntry, Int32 Index)
        { return Items.IndexOf(ChapterEntry, Index); }
        public Int32 IndexOf(ChapterEntry ChapterEntry, Int32 Index, Int32 Count)
        { return Items.IndexOf(ChapterEntry, Index, Count); }

        #region ChapterEntry
        public ChapterEntry PreviousChapter(ChapterEntry Chapter)
        {
            Int32 _IndexOfChapter = Items.IndexOf(Chapter) - 1;
            if (_IndexOfChapter >= 0)
                return this[_IndexOfChapter];
            return Chapter;
        }
        public ChapterEntry NextChapter(ChapterEntry Chapter)
        {
            Int32 _IndexOfChapter = Items.IndexOf(Chapter) + 1;
            if (_IndexOfChapter < Items.Count)
                return this[_IndexOfChapter];
            return Chapter;
        }
        #endregion

        #region ICollection<MangaArchiveInfoEntry> Members

        public ChapterEntry this[Int32 index]
        {
            get { return Items[index]; }
            set { Items[index] = value; }
        }

        public Int32 this[ChapterEntry CE]
        {
            get { return Items.IndexOf(CE); }
        }

        public ChapterEntry this[UInt32 Volume, UInt32 Chapter, UInt32 SubChapter]
        {
            get { return GetChapterByNumber(Volume, Chapter, SubChapter); }
            set { Items[Items.IndexOf(GetChapterByNumber(Volume, Chapter, SubChapter))] = value; }
        }

        public ChapterEntry GetChapterByNumber(UInt32 Volume, UInt32 Chapter, UInt32 SubChapter)
        {
            foreach (ChapterEntry CE in Items)
            {
                if (CE.Volume.Equals(Volume) &&
                    CE.Chapter.Equals(Chapter) &&
                    CE.SubChapter.Equals(SubChapter))
                    return CE;
            }
            return null;
        }

        public void Add(ChapterEntry item)
        {
            Items.Add(item);
        }

        public void Clear()
        {
            Items.Clear();
        }

        public bool Contains(ChapterEntry item)
        {
            return Items.Contains(item);
        }

        public bool Contains(UInt32 Chapter)
        {
            foreach (ChapterEntry CE in Items)
            {
                if (CE.Chapter.Equals(Chapter))
                    return true;
            }
            return false;
        }

        public void CopyTo(ChapterEntry[] array, int arrayIndex)
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

        public bool Remove(ChapterEntry item)
        {
            return Items.Remove(item);
        }

        #endregion

        #region IEnumerable<MangaArchiveInfoEntry> Members

        public IEnumerator<ChapterEntry> GetEnumerator()
        {
            return new ChapterEntryEnumerable(this);
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ChapterEntryEnumerable(this);
        }

        #endregion
    }

    [DebuggerStepThrough]
    public class ChapterEntryEnumerable : IEnumerator<ChapterEntry>
    {
        private ChapterEntryCollection _collection;
        private Int32 curIndex;
        private ChapterEntry curChapterEntry;

        public ChapterEntryEnumerable(ChapterEntryCollection Collection)
        {
            _collection = Collection;
            curIndex = -1;
            curChapterEntry = default(ChapterEntry);
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
                curChapterEntry = _collection[curIndex];
            }
            return true;
        }

        public void Reset()
        { curIndex = -1; }

        void IDisposable.Dispose() { }

        public ChapterEntry Current
        {
            get { return curChapterEntry; }
        }


        object IEnumerator.Current
        {
            get { return Current; }
        }
    }
}

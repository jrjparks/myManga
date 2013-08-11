using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;

namespace Manga.Core
{
    [DebuggerStepThrough]
    public class SearchInfo : NotifyPropChangeBase
    {
        protected String _Title { get; set; }
        protected String _CoverLocation { get; set; }
        protected String _Artist { get; set; }
        protected String _InformationLocation { get; set; }
        protected UInt32 _ID { get; set; }

        public String Title
        {
            get { return _Title; }
            set { _Title = value; OnPropertyChanged("Title"); }
        }
        public String CoverLocation
        {
            get { return _CoverLocation; }
            set { _CoverLocation = value; OnPropertyChanged("CoverLocation"); }
        }
        public String Artist
        {
            get { return _Artist; }
            set { _Artist = value; OnPropertyChanged("Artist"); }
        }
        public String InformationLocation
        {
            get { return _InformationLocation; }
            set { _InformationLocation = value; OnPropertyChanged("InformationLocation"); }
        }
        public UInt32 ID
        {
            get { return _ID; }
            set { _ID = value; OnPropertyChanged("ID"); }
        }

        public SearchInfo() :
            this(String.Empty, String.Empty, String.Empty, String.Empty, 0)
        { }
        public SearchInfo(
            String title,
            String coverLocation,
            String artist,
            String informationLocation,
            UInt32 iD)
        {
            Title = title;
            CoverLocation = coverLocation;
            Artist = artist;
            InformationLocation = informationLocation;
            ID = iD;
        }
    }

    [DebuggerStepThrough]
    public class SearchInfoCollection : ICollection<SearchInfo>
    {
        private List<SearchInfo> Items { get; set; }

        public void ForEach(Action<SearchInfo> action)
        {
            Items.ForEach(action);
        }

        public SearchInfoCollection()
        {
            Items = new List<SearchInfo>();
        }

        public void TrimExcess()
        {
            Items.TrimExcess();
        }

        public Boolean TrueForAll(Predicate<SearchInfo> match)
        {
            return Items.TrueForAll(match);
        }

        #region ICollection<MangaArchiveInfoEntry> Members

        public SearchInfo this[Int32 index]
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

        public Int32 this[SearchInfo SearchInfo]
        {
            get { return Items.IndexOf(SearchInfo); }
        }

        public Int32 IndexOfSearchInfo(SearchInfo SearchInfo)
        {
            Int32 _Index = 0;
            foreach (SearchInfo _SearchInfo in Items)
                if (_SearchInfo.Equals(SearchInfo))
                    break;
                else ++_Index;
            return _Index;
        }

        public SearchInfo GetSearchInfoByID(UInt32 ID)
        {
            foreach (SearchInfo _SearchInfo in Items)
            {
                if (_SearchInfo.ID.Equals(ID))
                    return _SearchInfo;
            }
            return null;
        }

        public SearchInfo GetSearchInfoByTitle(String Title)
        {
            foreach (SearchInfo _SearchInfo in Items)
            {
                if (_SearchInfo.Title.Equals(Title))
                    return _SearchInfo;
            }
            return null;
        }

        public void Add(SearchInfo item)
        {
            Items.Add(item);
        }

        public void Clear()
        {
            Items.Clear();
        }

        public bool Contains(SearchInfo item)
        {
            return Items.Contains(item);
        }

        public bool Contains(UInt32 ID)
        {
            foreach (SearchInfo _SearchInfo in Items)
            {
                if (_SearchInfo.ID.Equals(ID))
                    return true;
            }
            return false;
        }

        public void CopyTo(SearchInfo[] array, int arrayIndex)
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

        public bool Remove(SearchInfo item)
        {
            return Items.Remove(item);
        }

        #endregion

        #region IEnumerable<MangaArchiveInfoEntry> Members

        public IEnumerator<SearchInfo> GetEnumerator()
        {
            return new SearchInfoEnumerable(this);
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new SearchInfoEnumerable(this);
        }

        #endregion
    }

    [DebuggerStepThrough]
    public class SearchInfoEnumerable : IEnumerator<SearchInfo>
    {
        private SearchInfoCollection _collection;
        private Int32 curIndex;
        private SearchInfo curMangaArchiveInfoEntry;

        public SearchInfoEnumerable(SearchInfoCollection Collection)
        {
            _collection = Collection;
            curIndex = -1;
            curMangaArchiveInfoEntry = default(SearchInfo);
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

        public SearchInfo Current
        {
            get { return curMangaArchiveInfoEntry; }
        }


        object IEnumerator.Current
        {
            get { return Current; }
        }
    }
}

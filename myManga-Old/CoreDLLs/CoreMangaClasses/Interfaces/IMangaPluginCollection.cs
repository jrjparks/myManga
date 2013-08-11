using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Manga.Plugin
{
    [DebuggerStepThrough]
    public class IMangaPluginCollection : ICollection<IMangaPlugin>
    {        
        private List<IMangaPlugin> Items { get; set; }

        public void ForEach(Action<IMangaPlugin> action)
        {
            Items.ForEach(action);
        }

        public IMangaPlugin PluginToUse_SiteUrl(String URLPath)
        {
            foreach (IMangaPlugin Plugin in Items)
                if (System.Text.RegularExpressions.Regex.IsMatch(URLPath, Plugin.SiteURLFormat))
                    return Plugin;
            return null;
        }
        public IMangaPlugin PluginToUse_SiteName(String SiteName)
        {
            foreach (IMangaPlugin Plugin in Items)
                if (Plugin.SiteName.Equals(SiteName))
                    return Plugin;
            return null;
        }

        public IMangaPlugin this[String URLPath]
        {
            get
            {
                foreach (IMangaPlugin Plugin in Items)
                    if (Regex.IsMatch(URLPath, Plugin.SiteURLFormat))
                        return Plugin;
                return null;
            }
        }

        public Boolean RemoveAt(Int32 Index)
        {
            Boolean Success = false;
            if (Success = (Items.Count > Index))
                Items.RemoveAt(Index);
            return Success;
        }
        public Int32 RemoveAll(Predicate<IMangaPlugin> PluginsMatch)
        {
            return Items.RemoveAll(PluginsMatch);
        }
        public void RemoveRange(Int32 Start, Int32 Count)
        {
            Items.RemoveRange(Start, Count);
        }

        public IMangaPluginCollection()
        {
            Items = new List<IMangaPlugin>();
        }
        public IMangaPluginCollection(Int32 capacity)
        {
            Items = new List<IMangaPlugin>(capacity);
        }
        public IMangaPluginCollection(IEnumerable<IMangaPlugin> collection)
        {
            Items = new List<IMangaPlugin>(collection);
        }

        #region ICollection<MangaArchiveInfoEntry> Members

        public IMangaPlugin this[Int32 index]
        {
            get { return Items[index]; }
            set { Items[index] = value; }
        }

        public Int32 this[IMangaPlugin IMP]
        {
            get { return Items.IndexOf(IMP); }
        }

        public void Add(IMangaPlugin item)
        {
            if (!Contains(item.SiteName))
                Items.Add(item);
        }

        public void AddRange(params IMangaPlugin[] collection)
        {
            Items.AddRange(collection);
        }

        public void AddRange(IEnumerable<IMangaPlugin> collection)
        {
            Items.AddRange(collection);
        }

        public void Clear()
        {
            Items.Clear();
        }

        public bool Contains(IMangaPlugin item)
        {
            return Items.Contains(item);
        }

        public bool Contains(String SiteName)
        {
            foreach (IMangaPlugin IManga in Items)
                if (IManga.SiteName.Equals(SiteName))
                    return true;
            return false;
        }

        public void CopyTo(IMangaPlugin[] array, int arrayIndex)
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

        public bool Remove(IMangaPlugin item)
        {
            return Items.Remove(item);
        }

        public void TrimExcess()
        {
            Items.TrimExcess();
        }
        #endregion

        #region IEnumerable<MangaArchiveInfoEntry> Members

        public IEnumerator<IMangaPlugin> GetEnumerator()
        {
            return new IMangaPluginEnumerable(this);
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new IMangaPluginEnumerable(this);
        }

        #endregion

        public IMangaPlugin[] ToArray()
        { return Items.ToArray(); }
    }

    [DebuggerStepThrough]
    public class IMangaPluginEnumerable : IEnumerator<IMangaPlugin>
    {
        private IMangaPluginCollection _collection;
        private Int32 curIndex;
        private IMangaPlugin curMangaArchiveInfoEntry;

        public IMangaPluginEnumerable(IMangaPluginCollection Collection)
        {
            _collection = Collection;
            curIndex = -1;
            curMangaArchiveInfoEntry = default(IMangaPlugin);
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

        public IMangaPlugin Current
        {
            get { return curMangaArchiveInfoEntry; }
        }


        object IEnumerator.Current
        {
            get { return Current; }
        }
    }
}

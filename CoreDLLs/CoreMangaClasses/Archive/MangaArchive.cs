using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Manga.Core;
using System.Diagnostics;
using BakaBox.IO;

namespace Manga.Archive
{
    [DebuggerStepThrough]
    public class MangaArchiveData
    {
        public static String InfoFileName { get { return "Info.ma"; } }
    }

    /// <summary>
    /// This class stores information about a manga chapter
    /// </summary>
    [XmlRoot("MangaArchiveInfo"), DebuggerStepThrough]
    public class MangaArchiveInfo : MangaData
    {
        #region Variables
        [XmlIgnore]
        public PageEntryCollection _PageEntries { get; set; }
        [XmlArrayItem(Type = typeof(PageEntry))]
        public PageEntryCollection PageEntries
        {
            get { return _PageEntries; }
            set
            {
                _PageEntries = value;
                OnPropertyChanged("PageEntries");
            }
        }
        #endregion

        #region Constructors
        public MangaArchiveInfo(MangaArchiveInfo MangaArchiveInfo)
            : base(MangaArchiveInfo as MangaData)
        {
            PageEntries = MangaArchiveInfo.PageEntries;
        }
        public MangaArchiveInfo(MangaData MangaData)
            : base(MangaData)
        {
            PageEntries = new PageEntryCollection();
        }
        public MangaArchiveInfo()
            : base()
        {
            PageEntries = new PageEntryCollection();
        }
        #endregion
    }
}

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
        public static String TmpFolder { get { return Path.Combine(Path.GetTempPath(), "MangaArchives"); } }
    }

    /// <summary>
    /// This class stores information about a manga chapter
    /// </summary>
    [XmlRoot("MangaArchiveInfo"), DebuggerStepThrough]
    public class MangaArchiveInfo : MangaData
    {
        #region Variables
        [XmlIgnore]
        public String TmpFolderLocation { get { return Path.Combine(TmpVolumeFolderLocation, this.Chapter.ToString()).SafeFolder(); } }
        [XmlIgnore]
        public String TmpVolumeFolderLocation { get { return Path.Combine(TmpMangaFolderLocation, this.Volume.ToString()).SafeFolder(); } }
        [XmlIgnore]
        public String TmpMangaFolderLocation { get { return Path.Combine(MangaArchiveData.TmpFolder, this.Name).SafeFolder(); } }
        [XmlIgnore]
        public String MAISaveName
        {
            get { return (base.MemberwiseClone() as MangaArchiveInfo).MangaDataName(); }
        }

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

        #region Methods
        public String GetPageName(UInt32 Page)
        {
            try
            {
                if (PageEntries.Contains(Page))
                    return PageEntries.GetPageByNumber(Page).LocationInfo.FileName;
            }
            catch { }
            return String.Empty;
        }

        public String GetOnlinePagePath(UInt32 Page)
        {
            try
            {
                if (PageEntries.Contains(Page))
                    return PageEntries.GetPageByNumber(Page).LocationInfo.FullOnlinePath;
            }
            catch { }
            return String.Empty;
        }
        public String GetAltOnlinePagePath(UInt32 Page)
        {
            try
            {
                if (PageEntries.Contains(Page))
                    return PageEntries.GetPageByNumber(Page).LocationInfo.FullAltOnlinePath;
            }
            catch { }
            return String.Empty;
        }

        public String GetTmpPagePath(UInt32 Page)
        {
            try
            {
                if (PageEntries.Contains(Page))
                    return Path.Combine(TmpFolderLocation, GetPageName(Page));
            }
            catch { }
            return String.Empty;
        }
        #endregion
    }
}

using System;
using System.Diagnostics;
using System.Xml.Serialization;
using Manga.Core;
using Manga.Archive;

namespace Manga.Info
{
    [DebuggerStepThrough]
    public class MangaInfoData
    {
        public static String InfoFileName { get { return "Info.mi"; } }
        public static String CoverName { get { return "Cover.jpg"; } }
    }

    /// <summary>
    /// This class stores detailed information about manga.
    /// </summary>
    [XmlRoot("MangaInfo"), DebuggerStepThrough]
    public class MangaInfo : MangaData
    {
        #region Private
        [XmlIgnore]
        protected UInt32 _Page { get; set; }

        [XmlIgnore]
        protected String _InfoPage { get; set; }
        [XmlIgnore]
        protected ReadDirection _ReadDirection { get; set; }

        [XmlIgnore]
        protected MangaStatus _Status { get; set; }

        [XmlIgnore]
        protected String _AltTitle { get; set; }
        [XmlIgnore]
        protected String _Released { get; set; }
        [XmlIgnore]
        protected String _Author { get; set; }
        [XmlIgnore]
        protected String _Artist { get; set; }
        [XmlIgnore]
        protected String _Genre { get; set; }

        [XmlIgnore]
        protected ChapterEntryCollection _ChapterEntries { get; set; }
        #endregion

        #region Attributes
        [XmlAttribute("Page")]
        public UInt32 Page
        {
            get { return _Page; }
            set
            {
                _Page = value;
                OnPropertyChanged("Page");
            }
        }

        [XmlAttribute("InfoPage")]
        public String InfoPage
        {
            get { return _InfoPage; }
            set
            {
                _InfoPage = value;
                OnPropertyChanged("InfoPage");
            }
        }
        [XmlAttribute("ReadDirection")]
        public ReadDirection ReadDirection
        {
            get { return _ReadDirection; }
            set
            {
                _ReadDirection = value;
                OnPropertyChanged("ReadDirection");
            }
        }
        [XmlAttribute("Status")]
        public MangaStatus Status
        {
            get { return _Status; }
            set
            {
                _Status = value;
                OnPropertyChanged("Status");
            }
        }
        #endregion

        #region Elements
        [XmlAttribute("AltTitle")]
        public String AltTitle { get; set; }
        [XmlAttribute("Released")]
        public String Released { get; set; }
        [XmlAttribute("Author")]
        public String Author { get; set; }
        [XmlAttribute("Artist")]
        public String Artist { get; set; }
        [XmlAttribute("Genre")]
        public String Genre { get; set; }
        #endregion

        #region Arrays
        [XmlArrayItem(Type = typeof(ChapterEntry))]
        public ChapterEntryCollection ChapterEntries
        {
            get { return _ChapterEntries; }
            set
            {
                _ChapterEntries = value;
                OnPropertyChanged("ChapterEntries");
            }
        }
        #endregion

        #region Ignores
        [XmlIgnore]
        public ChapterEntry LastReadChapterEntry
        {
            get { return ChapterEntries.GetChapterByNumber(Volume, Chapter, SubChapter); }
        }
        #endregion

        #region Constructors
        public MangaInfo(MangaInfo MangaInfo)
            : base(MangaInfo)
        {
            AltTitle = MangaInfo.AltTitle;
            Released = MangaInfo.Released;
            Artist = MangaInfo.Artist;
            Author = MangaInfo.Author;
            Genre = MangaInfo.Genre;
            Page = MangaInfo.Page;
            InfoPage = MangaInfo.InfoPage;
            ReadDirection = MangaInfo.ReadDirection;
            Status = MangaInfo.Status;
            ChapterEntries = MangaInfo.ChapterEntries;
        }
        public MangaInfo(MangaData MangaData)
            : base(MangaData) 
        { MMI_Init(); }
        public MangaInfo()
            : base() 
        { MMI_Init(); }

        private void MMI_Init()
        {
            AltTitle = Released = Artist = Author = Genre = InfoPage = String.Empty;
            Page = UInt32.MinValue;
            Status = MangaStatus.Ongoing;
            ReadDirection = ReadDirection.FromRight;
            ChapterEntries = new ChapterEntryCollection();
        }
        #endregion
    }

    public enum ReadDirection
    {
        [XmlEnum("Left")]
        FromLeft,
        [XmlEnum("Right")]
        FromRight
    }

    public enum MangaStatus
    {
        [XmlEnum("Ongoing")]
        Ongoing,
        [XmlEnum("Complete")]
        Complete
    }
}

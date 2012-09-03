using System;
using System.Diagnostics;
using System.Xml.Serialization;
using Manga.Core;
using Manga.Archive;
using System.ComponentModel;

namespace Manga.Info
{
    [DebuggerStepThrough]
    public class MangaInfoConst
    {
        public const String InfoFileName = "Info.mi";
        public const String CoverName = "Cover.jpg";
    }

    

    /// <summary>
    /// This class stores detailed information about manga.
    /// </summary>
    [XmlRoot("MangaInfo"), DebuggerStepThrough]
    public class MangaInfo : MangaData
    {
        #region Private
        [XmlIgnore]
        protected UInt32 _TotalPage;
        [XmlIgnore]
        protected Boolean _Licensed;
        [XmlIgnore]
        protected Boolean _KeepChapters;

        [XmlIgnore]
        protected String _InfoPage;
        [XmlIgnore]
        protected ReadDirection _ReadDirection;

        [XmlIgnore]
        protected MangaStatus _Status;

        [XmlIgnore]
        protected String _AltTitle;
        [XmlIgnore]
        protected String _Released;
        [XmlIgnore]
        protected String _Author;
        [XmlIgnore]
        protected String _Artist;
        [XmlIgnore]
        protected String _Genre;

        [XmlIgnore]
        protected ChapterEntryCollection _ChapterEntries;
        #endregion

        #region Attributes
        [XmlAttribute("TotalPage")]
        public UInt32 TotalPage
        {
            get { return _TotalPage; }
            set
            {
                _TotalPage = value;
                OnPropertyChanged("TotalPage");
            }
        }
        [XmlAttribute("Licensed")]
        public Boolean Licensed
        {
            get { return _Licensed; }
            set
            {
                _Licensed = value;
                OnPropertyChanged("Licensed");
            }
        }
        [XmlAttribute("KeepChapters")]
        public Boolean KeepChapters
        {
            get { return _KeepChapters; }
            set
            {
                _KeepChapters = value;
                OnPropertyChanged("KeepChapters");
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
        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public Boolean LicensedSpecified { get { return Licensed; } }
        #endregion

        #region Constructors
        public MangaInfo(MangaInfo MangaInfo)
            : base()
        {
            base.Init(MangaInfo);
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
            KeepChapters = MangaInfo.KeepChapters;
        }
        public MangaInfo(MangaData MangaData)
            : base()
        {
            base.Init(MangaData);
            MMI_Init();
        }
        public MangaInfo()
            : base()
        {
            base.Init();
            MMI_Init();
        }

        private void MMI_Init()
        {
            AltTitle = Released = Artist = Author = Genre = InfoPage = String.Empty;
            Page = UInt32.MinValue;
            Status = MangaStatus.Ongoing;
            ReadDirection = ReadDirection.FromRight;
            ChapterEntries = new ChapterEntryCollection();
            KeepChapters = false;
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

using System;
using System.Xml.Serialization;
using System.Diagnostics;

namespace Manga.Core
{
    /// <summary>
    /// This class is used to store data about manga.
    /// </summary>
    [XmlRoot("MangaData"), DebuggerStepThrough]
    public class MangaData : NotifyPropChangeBase
    {
        #region Private Vars
        [XmlIgnore]
        protected String _Site { get; set; }
        [XmlIgnore]
        protected String _Name { get; set; }
        [XmlIgnore]
        protected UInt32 _ID { get; set; }
        [XmlIgnore]
        protected UInt32 _Volume { get; set; }
        [XmlIgnore]
        protected UInt32 _Chapter { get; set; }
        [XmlIgnore]
        protected UInt32 _SubChapter { get; set; }
        #endregion

        #region Public Vars
        [XmlAttribute("Site")]
        public String Site
        {
            get { return _Site; }
            set
            {
                _Site = value;
                OnPropertyChanged("Site");
            }
        }
        [XmlAttribute("Name")]
        public String Name
        {
            get { return _Name; }
            set
            {
                _Name = value;
                OnPropertyChanged("Name");
            }
        }
        [XmlAttribute("ID")]
        public UInt32 ID
        {
            get { return _ID; }
            set
            {
                _ID = value;
                OnPropertyChanged("ID");
            }
        }
        [XmlAttribute("Volume")]
        public UInt32 Volume
        {
            get { return _Volume; }
            set
            {
                _Volume = value;
                OnPropertyChanged("Volume");
            }
        }
        [XmlAttribute("Chapter")]
        public UInt32 Chapter
        {
            get { return _Chapter; }
            set
            {
                _Chapter = value;
                OnPropertyChanged("Chapter");
            }
        }
        [XmlAttribute("SubChapter")]
        public UInt32 SubChapter
        {
            get { return _SubChapter; }
            set
            {
                _SubChapter = value;
                OnPropertyChanged("SubChapter");
            }
        }
        #endregion

        #region XML Specifiers
        [XmlIgnore]
        public Boolean SiteSpecified { get { return !Site.Equals(String.Empty); } }
        [XmlIgnore]
        public Boolean NameSpecified { get { return !Name.Equals(String.Empty); } }
        [XmlIgnore]
        public Boolean IDSpecified { get { return !ID.Equals(UInt32.MinValue); } }
        [XmlIgnore]
        public Boolean VolumeSpecified { get { return !Volume.Equals(UInt32.MinValue); } }
        [XmlIgnore]
        public Boolean ChapterSpecified { get { return !Chapter.Equals(UInt32.MinValue); } }
        [XmlIgnore]
        public Boolean SubChapterSpecified { get { return !SubChapter.Equals(UInt32.MinValue); } }
        #endregion

        public MangaData()
        {
            Name = Site = String.Empty;
            Volume = Chapter = SubChapter = ID = UInt32.MinValue;
        }
        public MangaData(MangaData MangaData)
        {
            Name = MangaData.Name;
            Site = MangaData.Site;
            Volume = MangaData.Volume;
            Chapter = MangaData.Chapter;
            SubChapter = MangaData.SubChapter;
            ID = MangaData.ID;
        }
    }
}

using System;
using System.Xml.Serialization;
using System.Diagnostics;
using BakaBox.MVVM;
using System.ComponentModel;

namespace Manga.Core
{
    /// <summary>
    /// This class is used to store data about manga.
    /// </summary>
    [XmlRoot("MangaData"), DebuggerStepThrough]
    public abstract class MangaData : ModelBase
    {
        #region Private Vars
        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected String _Site;
        [XmlIgnore]
        protected String _Name;
        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected UInt32 _ID;
        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected UInt32 _Volume;
        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected UInt32 _Chapter;
        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected UInt32 _SubChapter;
        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        protected UInt32 _Page;
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
        #endregion

        #region XML Specifiers
        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public Boolean SiteSpecified { get { return !Site.Equals(String.Empty); } }
        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public Boolean NameSpecified { get { return !Name.Equals(String.Empty); } }
        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public Boolean IDSpecified { get { return !ID.Equals(UInt32.MinValue); } }
        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public Boolean VolumeSpecified { get { return !Volume.Equals(UInt32.MinValue); } }
        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public Boolean ChapterSpecified { get { return !Chapter.Equals(UInt32.MinValue); } }
        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public Boolean SubChapterSpecified { get { return !SubChapter.Equals(UInt32.MinValue); } }
        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public Boolean PageSpecified { get { return !Page.Equals(UInt32.MinValue); } }
        #endregion

        public void Init()
        {
            Name = Site = String.Empty;
            Volume = Chapter = SubChapter = ID = UInt32.MinValue;
        }
        public void Init(MangaData MangaData)
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

using System;
using System.Xml.Serialization;

namespace Manga.Core
{
    public class mData
    {
        public enum FileType { MZA, MIZA, Name }
        public static String MDFileName(MangaData MD, FileType FileType)
        {
            String FileName = MD.Name, Extention = String.Empty;

            if (!FileType.Equals(FileType.MIZA))
            {
                if (MD.Volume > 0)
                    FileName += String.Format(" v{0}", MD.Volume.ToString());
                if (MD.Chapter > 0)
                    FileName += String.Format(" c{0}", MD.Chapter.ToString());
                if (MD.SubChapter > 0)
                    FileName += String.Format(".{0}", MD.SubChapter.ToString());
            }
            switch (FileType)
            {
                case mData.FileType.MZA:
                    Extention = ".mza";
                    break;

                case mData.FileType.MIZA:
                    Extention = ".miza";
                    break;

                default:
                case mData.FileType.Name:
                    break;
            }
            return String.Format("{0}{1}", FileName, Extention);
        }
    }

    [XmlRoot("MangaData")]
    public class MangaData
    {
        [XmlAttribute("Site")]
        public String Site { get; set; }

        [XmlAttribute("Name")]
        public String Name { get; set; }
        [XmlAttribute("ID")]
        public UInt32 ID { get; set; }
        [XmlAttribute("Volume")]
        public UInt32 Volume { get; set; }
        [XmlAttribute("Chapter")]
        public UInt32 Chapter { get; set; }
        [XmlAttribute("SubChapter")]
        public UInt32 SubChapter { get; set; }

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

        public MangaData()
        {
            Name = String.Empty;
            Volume = Chapter = SubChapter = UInt32.MinValue;
        }
    }
}

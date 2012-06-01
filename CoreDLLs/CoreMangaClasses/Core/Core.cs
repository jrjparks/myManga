using System;
using System.IO;
using Manga.Info;
using System.Diagnostics;

namespace Manga
{
    [DebuggerStepThrough]
    public class CoverData : NotifyPropChangeBase
    {
        protected Stream _CoverStream { get; set; }
        protected String _CoverName { get; set; }

        public Stream CoverStream
        {
            get { return _CoverStream; }
            set
            {
                _CoverStream = value;
                OnPropertyChanged("CoverStream");
            }
        }
        public String CoverName
        {
            get { return _CoverName; }
            set
            {
                _CoverName = value;
                OnPropertyChanged("CoverName");
            }
        }

        public CoverData()
        {
            CoverName = String.Empty;
            CoverStream = Stream.Null;
        }
        public CoverData(String Name, Stream Data)
        {
            CoverName = Name;
            CoverStream = Data;
        }
    }

    [DebuggerStepThrough]
    public class MangaInfoCoverData
    {
        public CoverData CoverData { get; set; }
        public MangaInfo MangaInfo { get; set; }

        public MangaInfoCoverData(MangaInfo MangaInfo, CoverData CoverData)
        {
            this.MangaInfo = MangaInfo;
            this.CoverData = CoverData;
        }
    }
}

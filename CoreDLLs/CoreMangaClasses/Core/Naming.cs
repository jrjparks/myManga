using System;
using System.Diagnostics;
using Manga.Archive;
using Manga.Core;
using Manga.Info;
using BakaBox.IO;

namespace Manga
{
    [DebuggerStepThrough]
    public static class NamingExtensions
    {
        public static String MangaDataName(this MangaData Data)
        { return Data.MangaDataName(true); }
        public static String MangaDataName(this MangaData Data, Boolean WithExtention)
        {
            String FileName = Data.Name, Extention = String.Empty;
            if (Data is MangaInfo) 
                Extention = ".miza";
            else if (Data is MangaArchiveInfo)
            {
                Extention = ".mza";
                if (Data.Volume > 0)
                    FileName += String.Format(" v{0}", Data.Volume);
                if (Data.Chapter > 0)
                    FileName += String.Format(" c{0}", Data.Chapter);
                if (Data.SubChapter > 0)
                    FileName += String.Format(".{0}", Data.SubChapter);
            }
            return String.Format("{0}{1}", FileName, WithExtention ? Extention : String.Empty).SafeFileName();
        }
        
        public static String ChapterName(this ChapterEntry ChapterEntry, MangaData Data)
        { return ChapterEntry.ChapterName(Data, true); }
        public static String ChapterName(this ChapterEntry ChapterEntry, String MangaName)
        { return ChapterEntry.ChapterName(MangaName, true); }
        public static String ChapterName(this ChapterEntry ChapterEntry, MangaData Data, Boolean WithExtention)
        { return ChapterEntry.ChapterName(Data.Name, WithExtention); }
        public static String ChapterName(this ChapterEntry ChapterEntry, String MangaName, Boolean WithExtention)
        {
            String FileName = MangaName, Extention = ".mza";
            if (ChapterEntry.Volume > 0)
                FileName += String.Format(" v{0}", ChapterEntry.Volume);
            if (ChapterEntry.Chapter > 0)
                FileName += String.Format(" c{0}", ChapterEntry.Chapter);
            if (ChapterEntry.SubChapter > 0)
                FileName += String.Format(".{0}", ChapterEntry.SubChapter);
            return String.Format("{0}{1}", FileName, WithExtention ? Extention : String.Empty).SafeFileName();
        }
    }
}

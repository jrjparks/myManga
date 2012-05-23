using System;
using Manga;
using Manga.Archive;
using Manga.Core;
using Manga.Info;
using Manga.Plugin;

namespace MangaPlugin
{
    public class MangaPlugin : PluginProgressChanged, IMangaPlugin
    {
        #region IMangaPlugin Vars
        // Site Name, Plain Text
        public string SiteName
        {
            get { throw new NotImplementedException(); }
        }

        // This is a Regular Expresion
        public string SiteURLFormat
        {
            get { throw new NotImplementedException(); }
        }

        // Methods the site supporte.
        public SupportedMethods SupportedMethods
        {
            get { throw new NotImplementedException(); }
        }
        #endregion

        #region IMangaPlugin Methods
        // Download the cover image.
        public CoverData GetCoverImage(MangaInfo MangaInfo)
        {
            throw new NotImplementedException();
        }

        // Download chapter information.
        public MangaArchiveInfo LoadChapterInformation(string ChapterPath)
        {
            throw new NotImplementedException();
        }

        // Download manga information.
        public MangaInfo LoadMangaInformation(string InfoPage)
        {
            throw new NotImplementedException();
        }

        // Download convert url to/from Info and Chapter formats.
        public String ManipulateMangaData(MangaData MangaData, DataType ManipulationType)
        {
            throw new NotImplementedException();
        }

        // Search the website.
        public SearchInfoCollection Search(string Text, int Limit)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}

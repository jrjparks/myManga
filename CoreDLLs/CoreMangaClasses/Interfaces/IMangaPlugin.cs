using System;
using Manga.Archive;
using Manga.Core;
using Manga.Info;
using System.IO;
using System.Diagnostics;

namespace Manga.Plugin
{
    [Flags]
    public enum SupportedMethods
    {
        None = 0x01,
        ChapterInfo = 0x02,
        MangaInfo = 0x04,
        Search = 0x08,
        CoverImage = 0x16,

        All = ChapterInfo | MangaInfo | Search | CoverImage
    }

    [Flags]
    public enum DataType
    {
        None = 0x01,
        MangaInformation = 0x02,
        ChapterInformation = 0x04
    }

    public interface IMangaPlugin
    {
        #region Events
        event PluginProgressChanged.ProgressChange ProgressChanged;
        #endregion

        #region IMangaPlugin Properties
        /// <summary>
        /// Define the supported members.
        /// </summary>
        SupportedMethods SupportedMethods { get; }

        /// <summary>
        /// Please use static name for this property.
        /// </summary>
        String SiteName { get; }
        
        /// <summary>
        /// Regex string for detecting url.
        /// </summary>
        String SiteURLFormat { get; }

        /// <summary>
        /// The Referer Header for the site.
        /// </summary>
        String SiteRefererHeader { get; }
        #endregion

        #region IMangaPlugin Members
        /// <summary>
        /// Method for downloading chapter.
        /// </summary>
        /// <param name="ChapterPath">The URL where the manga chapter can be downloaded from.</param>
        /// <returns>MangaArchiveInfo about manga chapter.</returns>
        MangaArchiveInfo LoadChapterInformation(String ChapterPath);

        /// <summary>
        /// Method for downloading manga information and chapters.
        /// </summary>
        /// <param name="InfoPage">The URL where the manga information can be downloaded from.</param>
        /// <returns>MangaInfo about manga.</returns>
        MangaInfo LoadMangaInformation(String InfoPage);

        /// <summary>
        /// Manipulate URLs to and from the different information locations.
        /// </summary>
        /// <param name="MangaData">MangaInfo</param>
        /// <param name="To">What to change the information to.</param>
        /// <returns>URL of information location.</returns>
        String ManipulateMangaData(MangaData MangaData, DataType ManipulationType);

        /// <summary>
        /// Download the cover for the manga.
        /// </summary>
        /// <param name="MangaInfo">The MangaInfo of the cover to download.</param>
        /// <returns></returns>
        CoverData GetCoverImage(MangaInfo MangaInfo);

        /// <summary>
        /// Search the manga site to find/list manga.
        /// </summary>
        /// <param name="Text">Text to search for.</param>
        /// <param name="Limit">Maximum number of items to return.</param>
        /// <returns>Collection of SearchInfo.</returns>
        SearchInfoCollection Search(String Text, Int32 Limit);
        #endregion
    }

    [DebuggerStepThrough]
    public class WebMangaWorkerTransfer
    {
        public Object Data { get; set; }
        public DataType RequestType { get; set; }
    }

    [DebuggerStepThrough]
    public class PluginProgressChanged
    {
        public delegate void ProgressChange(Object Sender, Int32 Progress, Object Data);
        public event ProgressChange ProgressChanged;
        protected virtual void OnProgressChanged(Int32 Progress)
        {
            if (ProgressChanged != null)
                ProgressChanged(this, Progress, default(MangaData));
        }
        protected virtual void OnProgressChanged(Int32 Progress, Object Data)
        {
            if (ProgressChanged != null)
                ProgressChanged(this, Progress, Data);
        }
    }
}

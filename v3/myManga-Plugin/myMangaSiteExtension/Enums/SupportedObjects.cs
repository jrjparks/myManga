using System;
namespace myMangaSiteExtension
{
    [Flags]
    public enum SupportedObjects
    {
        None = 0x00,
        Manga = 0x01,
        Chapter = 0x02,
        Page = 0x04,
        Search = 0x08,
        Covers = 0x16,

        All = Manga | Chapter | Page | Search | Covers
    }
}
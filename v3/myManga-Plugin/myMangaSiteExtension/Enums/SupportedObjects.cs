using System;
namespace myMangaSiteExtension
{
    [Flags]
    public enum SupportedObjects
    {
        None = 0x01,
        Covers=0x16,

        All = Covers
    }
}
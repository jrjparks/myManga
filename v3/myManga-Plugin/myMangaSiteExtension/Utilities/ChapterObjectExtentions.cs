using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using myMangaSiteExtension.Objects;

namespace myMangaSiteExtension.Utilities
{
    public static class ChapterObjectExtentions
    {
        public static void Merge(this ChapterObject chapterObject, params ChapterObject[] list) { chapterObject.Merge(list.AsEnumerable()); }
        public static void Merge(this ChapterObject chapterObject, IEnumerable<ChapterObject> list)
        {
            if (list.Count() > 0)
            {
                // Volume
                foreach (Int32 Volume in (from ChapterObject obj in list where obj != null select obj.Volume))
                    if (chapterObject.Volume < 0 && Volume >= 0)
                        chapterObject.Volume = Volume;

                // Chapter
                foreach (Int32 Chapter in (from ChapterObject obj in list where obj != null select obj.Chapter))
                    if (chapterObject.Chapter < 0 && Chapter >= 0)
                        chapterObject.Chapter = Chapter;

                // SubChapter
                foreach (Int32 SubChapter in (from ChapterObject obj in list where obj != null select obj.SubChapter))
                    if (chapterObject.SubChapter < 0 && SubChapter >= 0)
                        chapterObject.SubChapter = SubChapter;

                // Locations
                foreach (List<LocationObject> Locations in (from ChapterObject obj in list where obj != null select obj.Locations))
                    foreach (LocationObject Location in Locations)
                        if (!chapterObject.Locations.Any(o => o.ExtensionName == Location.ExtensionName))
                            chapterObject.Locations.Add(Location);
            }
        }
        public static ChapterObject Merge(IEnumerable<ChapterObject> list)
        {
            if (list == null || list.Count() == 0)
                return null;
            ChapterObject chapterObject = list.First();
            if (list.Count() > 1)
            {
                // Locations
                foreach (List<LocationObject> Locations in (from ChapterObject obj in list
                                                            where (obj != null && obj.Volume == chapterObject.Volume && obj.Chapter == chapterObject.Chapter && obj.SubChapter == chapterObject.SubChapter)
                                                            select obj.Locations))
                    foreach (LocationObject Location in Locations)
                        if (!chapterObject.Locations.Any(o => o.ExtensionName == Location.ExtensionName))
                            chapterObject.Locations.Add(Location);
            }
            return chapterObject;
        }
    }
}

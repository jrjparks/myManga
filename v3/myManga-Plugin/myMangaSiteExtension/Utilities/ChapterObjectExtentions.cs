﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
                // Name
                Regex nameRgx = new Regex(@"^vol(ume)?[\d\s]+.+?ch(apter)?[\d\s]+");
                if (String.IsNullOrWhiteSpace(chapterObject.Name) || nameRgx.IsMatch(chapterObject.Name.ToLower()))
                {
                    ChapterObject FoD_Obj = list.FirstOrDefault(cO => !String.IsNullOrWhiteSpace(cO.Name) && !nameRgx.IsMatch(cO.Name.ToLower()));
                    if (FoD_Obj != null)
                        chapterObject.Name = FoD_Obj.Name;
                }

                // Volume
                foreach (Int32 Volume in (from ChapterObject obj in list where obj != null select obj.Volume))
                    if (chapterObject.Volume <= 0 && Volume >= 0)
                        chapterObject.Volume = Volume;

                // Chapter
                foreach (Int32 Chapter in (from ChapterObject obj in list where obj != null select obj.Chapter))
                    if (chapterObject.Chapter <= 0 && Chapter >= 0)
                        chapterObject.Chapter = Chapter;

                // SubChapter
                foreach (Int32 SubChapter in (from ChapterObject obj in list where obj != null select obj.SubChapter))
                    if (chapterObject.SubChapter <= 0 && SubChapter >= 0)
                        chapterObject.SubChapter = SubChapter;

                // Locations
                foreach (List<LocationObject> Locations in (from ChapterObject obj in list where obj != null select obj.Locations))
                    foreach (LocationObject Location in Locations)
                        if (!chapterObject.Locations.Any(o => o.ExtensionName == Location.ExtensionName))
                            chapterObject.Locations.Add(Location);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Other;
using myMangaSiteExtension.Objects;

namespace myMangaSiteExtension.Utilities
{
    public static class MangaObjectExtensions
    {
        public static void Merge(this MangaObject mangaObject, params MangaObject[] list) { mangaObject.Merge(list.AsEnumerable()); }
        public static void Merge(this MangaObject mangaObject, IEnumerable<MangaObject> list)
        {
            if (list.Count() > 0)
            {
                // Locations
                foreach (List<LocationObject> Locations in (from MangaObject obj in list where obj != null select obj.Locations))
                    foreach (LocationObject Location in Locations)
                        if (!mangaObject.Locations.Any(o => o.ExtensionName == Location.ExtensionName))
                            mangaObject.Locations.Add(Location);

                // DatabaseLocations
                foreach (List<LocationObject> DatabaseLocations in (from MangaObject obj in list where obj != null select obj.DatabaseLocations))
                    foreach (LocationObject DatabaseLocation in DatabaseLocations)
                        if (!mangaObject.DatabaseLocations.Any(o => o.ExtensionName == DatabaseLocation.ExtensionName))
                            mangaObject.DatabaseLocations.Add(DatabaseLocation);

                // Artists
                foreach (List<String> Artists in (from MangaObject obj in list where obj != null select obj.Artists))
                    foreach (String Artist in Artists)
                        if (Artist != null && !mangaObject.Artists.Any(o => o.ToLower() == Artist.ToLower()))
                            mangaObject.Artists.Add(Artist);

                // Authors
                foreach (List<String> Authors in (from MangaObject obj in list where obj != null select obj.Authors))
                    foreach (String Author in Authors)
                        if (Author != null && !mangaObject.Authors.Any(o => o.ToLower() == Author.ToLower()))
                            mangaObject.Authors.Add(Author);

                // Covers
                foreach (List<String> Covers in (from MangaObject obj in list where obj != null select obj.Covers))
                    foreach (String Cover in Covers)
                        if (Cover != null && !mangaObject.Covers.Any(o => o == Cover))
                            mangaObject.Covers.Add(Cover);

                // AlternateNames
                foreach (String Name in (from MangaObject obj in list where obj != null select obj.Name))
                    if (!mangaObject.AlternateNames.Any(o => o.ToLower() == Name.ToLower()) && Name != null)
                        mangaObject.AlternateNames.Add(Name);
                foreach (List<String> AlternateNames in (from MangaObject obj in list where obj != null select obj.AlternateNames))
                    foreach (String AlternateName in AlternateNames)
                        if (AlternateName != null && !mangaObject.AlternateNames.Any(o => o.ToLower() == AlternateName.ToLower()))
                            mangaObject.AlternateNames.Add(AlternateName);

                // Genres
                foreach (List<String> Genres in (from MangaObject obj in list where obj != null select obj.Genres))
                    foreach (String Genre in Genres)
                        if (Genre != null && !mangaObject.Genres.Any(o => o.ToLower() == Genre.ToLower()))
                            mangaObject.Genres.Add(Genre);

                // Chapters
                foreach (List<ChapterObject> Chapters in (from MangaObject obj in list where obj != null select obj.Chapters))
                    foreach (ChapterObject Chapter in Chapters)
                        if (Chapter != null)
                            if (!mangaObject.Chapters.Any(o => o.Chapter == Chapter.Chapter && o.SubChapter == Chapter.SubChapter))
                                mangaObject.Chapters.Add(Chapter);
                            else
                                mangaObject.Chapters.Find(o => o.Chapter == Chapter.Chapter && o.SubChapter == Chapter.SubChapter).Merge(Chapter);
            }
        }
        public static MangaObject Merge(IEnumerable<MangaObject> list)
        {
            if (list == null || list.Count() == 0)
                return null;
            MangaObject mangaObject = list.First();
            if (list.Count() > 1)
            {
                // Locations
                foreach (List<LocationObject> Locations in (from MangaObject obj in list.Skip(1) where obj != null select obj.Locations))
                    foreach (LocationObject Location in Locations)
                        if (!mangaObject.Locations.Any(o => o.ExtensionName == Location.ExtensionName))
                            mangaObject.Locations.Add(Location);

                // DatabaseLocations
                foreach (List<LocationObject> DatabaseLocations in (from MangaObject obj in list.Skip(1) where obj != null select obj.DatabaseLocations))
                    foreach (LocationObject DatabaseLocation in DatabaseLocations)
                        if (!mangaObject.DatabaseLocations.Any(o => o.ExtensionName == DatabaseLocation.ExtensionName))
                            mangaObject.DatabaseLocations.Add(DatabaseLocation);

                // Artists
                foreach (List<String> Artists in (from MangaObject obj in list.Skip(1) where obj != null select obj.Artists))
                    foreach (String Artist in Artists)
                        if (Artist != null && !mangaObject.Artists.Any(o => o.ToLower() == Artist.ToLower()))
                            mangaObject.Artists.Add(Artist);

                // Authors
                foreach (List<String> Authors in (from MangaObject obj in list.Skip(1) where obj != null select obj.Authors))
                    foreach (String Author in Authors)
                        if (Author != null && !mangaObject.Authors.Any(o => o.ToLower() == Author.ToLower()))
                            mangaObject.Authors.Add(Author);

                // Covers
                foreach (List<String> Covers in (from MangaObject obj in list.Skip(1) where obj != null select obj.Covers))
                    foreach (String Cover in Covers)
                        if (Cover != null && !mangaObject.Covers.Any(o => o == Cover))
                            mangaObject.Covers.Add(Cover);

                // AlternateNames
                foreach (String Name in (from MangaObject obj in list.Skip(1) where obj != null select obj.Name))
                    if (!mangaObject.AlternateNames.Any(o => o.ToLower() == Name.ToLower()) && Name != null)
                        mangaObject.AlternateNames.Add(Name);
                foreach (List<String> AlternateNames in (from MangaObject obj in list.Skip(1) where obj != null select obj.AlternateNames))
                    foreach (String AlternateName in AlternateNames)
                        if (AlternateName != null && !mangaObject.AlternateNames.Any(o => o.ToLower() == AlternateName.ToLower()))
                            mangaObject.AlternateNames.Add(AlternateName);

                // Genres
                foreach (List<String> Genres in (from MangaObject obj in list.Skip(1) where obj != null select obj.Genres))
                    foreach (String Genre in Genres)
                        if (Genre != null && !mangaObject.Genres.Any(o => o.ToLower() == Genre.ToLower()))
                            mangaObject.Genres.Add(Genre);
            }
            return mangaObject;
        }

        public static void AttachDatabase(this MangaObject value, DatabaseObject databaseObject, Boolean databaseAsMaster = false)
        {
            value.DatabaseLocations = databaseObject.Locations;
            if (databaseAsMaster)
            {
                if (value.Name != null && !value.AlternateNames.Any(o => o.ToLower() == value.Name.ToLower()))
                    value.AlternateNames.Add(value.Name);
                value.Name = databaseObject.Name;
            }
            else if (databaseObject.Name != null && !value.AlternateNames.Any(o => o.ToLower() == databaseObject.Name.ToLower()))
                value.AlternateNames.Add(databaseObject.Name);
            // AlternateNames
            foreach (String AlternateName in databaseObject.AlternateNames)
                if (AlternateName != null && !value.AlternateNames.Any(o => o.ToLower() == AlternateName.ToLower()))
                    value.AlternateNames.Add(AlternateName);

            if (databaseAsMaster || value.Description == null || value.Description.Equals(String.Empty))
                value.Description = databaseObject.Description;

            // Genres
            foreach (String Genre in databaseObject.Genres)
                if (Genre != null && !value.Genres.Any(o => o.ToLower() == Genre.ToLower()))
                    value.Genres.Add(Genre);

            // DatabaseLocations
            foreach (LocationObject DatabaseLocation in databaseObject.Locations)
                if (!value.DatabaseLocations.Any(o => o.ExtensionName == DatabaseLocation.ExtensionName))
                    value.DatabaseLocations.Add(DatabaseLocation);

            // Covers
            foreach (String Cover in databaseObject.Covers)
                if (Cover != null && !value.Covers.Any(o => o == Cover))
                    value.Covers.Add(Cover);
        }
    }
}

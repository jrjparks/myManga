using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using myMangaSiteExtension.Objects;

namespace myMangaSiteExtension.Utilities
{
    public static class DatabaseObjectExtensions
    {
        public static DatabaseObject Merge(IEnumerable<DatabaseObject> list)
        {
            if (list == null || list.Count() == 0)
                return null;
            DatabaseObject databaseObject = list.First();
            if (list.Count() > 1)
            {
                // Locations
                foreach (List<LocationObject> Locations in (from DatabaseObject obj in list.Skip(1) where obj != null select obj.Locations))
                    foreach (LocationObject Location in Locations)
                        if (!databaseObject.Locations.Any(o => o.ExtensionName == Location.ExtensionName))
                            databaseObject.Locations.Add(Location);

                // Covers
                foreach (List<String> Covers in (from DatabaseObject obj in list.Skip(1) where obj != null select obj.Covers))
                    foreach (String Cover in Covers)
                        if (!databaseObject.Covers.Any(o => o == Cover))
                            databaseObject.Covers.Add(Cover);

                // AlternateNames
                foreach (String Name in (from DatabaseObject obj in list.Skip(1) where obj != null select obj.Name))
                    if (!databaseObject.AlternateNames.Any(o => o.ToLower() == Name.ToLower()))
                        databaseObject.AlternateNames.Add(Name);
                foreach (List<String> AlternateNames in (from DatabaseObject obj in list.Skip(1) select obj.AlternateNames))
                    foreach (String AlternateName in AlternateNames)
                        if (!databaseObject.AlternateNames.Any(o => o.ToLower() == AlternateName.ToLower()))
                            databaseObject.AlternateNames.Add(AlternateName);

                // Staff
                foreach (List<String> Staff in (from DatabaseObject obj in list.Skip(1) where obj != null select obj.Staff))
                    foreach (String Person in Staff)
                        if (!databaseObject.Staff.Any(o => o.ToLower() == Person.ToLower()))
                            databaseObject.Staff.Add(Person);

                // Genres
                foreach (List<String> Genres in (from DatabaseObject obj in list.Skip(1) where obj != null select obj.Genres))
                    foreach (String Genre in Genres)
                        if (!databaseObject.Genres.Any(o => o.ToLower() == Genre.ToLower()))
                            databaseObject.Genres.Add(Genre);
            }
            return databaseObject;
        }
    }
}

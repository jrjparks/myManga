using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using myMangaSiteExtension.Objects;

namespace myMangaSiteExtension.Interfaces
{
    public interface IDatabaseExtension
    {
        String GetSearchUri(String searchTerm);

        DatabaseObject ParseDatabaseObject(String content);
        List<DatabaseObject> ParseSearch(String content);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Attributes;

namespace myMangaSiteExtension.Interfaces
{
    public interface IDatabaseExtension
    {
        IDatabaseExtensionDescriptionAttribute DatabaseExtensionDescriptionAttribute { get; }

        SearchRequestObject GetSearchRequestObject(String searchTerm);

        DatabaseObject ParseDatabaseObject(String content);
        List<DatabaseObject> ParseSearch(String content);
    }
}

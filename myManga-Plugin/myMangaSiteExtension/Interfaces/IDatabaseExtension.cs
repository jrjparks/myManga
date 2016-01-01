using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Objects;
using System;
using System.Collections.Generic;

namespace myMangaSiteExtension.Interfaces
{
    public interface IDatabaseExtension : IExtension
    {
        IDatabaseExtensionDescriptionAttribute DatabaseExtensionDescriptionAttribute { get; }

        SearchRequestObject GetSearchRequestObject(String searchTerm);

        DatabaseObject ParseDatabaseObject(String content);
        List<DatabaseObject> ParseSearch(String content);
    }
}

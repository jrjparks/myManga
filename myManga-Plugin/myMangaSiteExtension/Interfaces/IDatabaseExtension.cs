using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Objects;
using System;
using System.Collections.Generic;

namespace myMangaSiteExtension.Interfaces
{
    public interface IDatabaseExtension : IExtension
    {
        DatabaseObject ParseDatabaseObject(String content);

        /// <summary>
        /// Use extension to parse search page.
        /// </summary>
        /// <param name="SearchTerm">Search term to send.</param>
        /// <returns>List of SearchResultObjects</returns>
        new List<DatabaseObject> ParseSearch(String Content);
    }
}

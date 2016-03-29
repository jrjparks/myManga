using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace MangaEden
{
    [DataContract]
    public class SearchResponse
    {
        [DataMember(Name = "label")]
        public String Label { get; set; }
        [DataMember(Name = "url")]
        public String URL { get; set; }
        [DataMember(Name = "value")]
        public String Value { get; set; }
    }

    [DataContract]
    public class MangaList
    {
        [DataMember(Name = "end")]
        public Int32 End { get; set; }
        [DataMember(Name = "start")]
        public Int32 Start { get; set; }
        [DataMember(Name = "page")]
        public Int32 Page { get; set; }
        [DataMember(Name = "length")]
        public Int32 Length { get; set; }

        [DataMember(Name = "manga")]
        public MangaItem[] Manga { get; set; }
    }

    [DataContract]
    public class MangaItem
    {
        [DataMember(Name = "a")]
        public String Alias { get; set; }
        [DataMember(Name = "c")]
        public String[] Category { get; set; }
        [DataMember(Name = "h")]
        public Int32 Hits { get; set; }
        [DataMember(Name = "i")]
        public String ID { get; set; }
        [DataMember(Name = "im")]
        public String MangaImageUrl { get; set; }
        [DataMember(Name = "ld")]
        public Int32 LastChapterDate { get; set; }
        [DataMember(Name = "s")]
        public String s { get; set; }
        [DataMember(Name = "t")]
        public String Title { get; set; }
    }
}

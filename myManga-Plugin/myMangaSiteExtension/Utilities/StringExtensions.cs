using System;

namespace myMangaSiteExtension.Utilities
{
    public static class StringExtensions
    {
        /// <summary>
        /// Get the string slice between the two indexes.
        /// Inclusive for start index, exclusive for end index.
        /// </summary>
        public static String Slice(this String source, Int32 start, Int32 end)
        {
            if (start < 0) // Keep this for negative end support
                start = source.Length + start;
            if (end < 0) // Keep this for negative end support
                end = source.Length + end;
            Int32 len = end - start;               // Calculate length
            return source.Substring(start, len); // Return Substring of length
        }
    }
}

using System.Collections.Generic;

namespace Unity.Labs.EditorXR
{
    /// <summary>
    /// Implementors receive a filtered list of types found
    /// </summary>
    public interface IFilterUI
    {
        /// <summary>
        /// The filter list provided
        /// </summary>
        List<string> filterList { set; }

        /// <summary>
        /// The search query to be performed
        /// </summary>
        string searchQuery { get; }
    }

    /// <summary>
    /// Extension methods for IFilterUI
    /// </summary>
    public static class FilterUIMethods
    {
        /// <summary>
        /// Check if the given string matches the filter
        /// </summary>
        /// <param name="filterUI">The filterUI providing the filtering</param>
        /// <param name="type">The type string to check</param>
        /// <returns>Whether the string matches the filter</returns>
        public static bool MatchesFilter(this IFilterUI filterUI, string type)
        {
            var searchQuery = filterUI.searchQuery;
            if (string.IsNullOrEmpty(searchQuery))
                return true;

            return searchQuery.StartsWith(type);
        }
    }
}

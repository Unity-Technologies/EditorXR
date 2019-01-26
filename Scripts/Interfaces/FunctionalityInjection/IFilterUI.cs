using System.Collections.Generic;

namespace UnityEditor.Experimental.EditorVR
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

    public static class IFilterUIMethods
    {
        public static bool MatchesFilter(this IFilterUI filterUI, string type)
        {
            var searchQuery = filterUI.searchQuery;
            if (string.IsNullOrEmpty(searchQuery))
                return true;

            return searchQuery.StartsWith(type);
        }
    }
}

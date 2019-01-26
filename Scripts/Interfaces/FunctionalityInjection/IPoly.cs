using UnityEditor.Experimental.EditorVR;

#if INCLUDE_POLY_TOOLKIT
using System.Collections.Generic;
using PolyToolkit;
using UnityEditor.Experimental.EditorVR.Workspaces;
#endif

[assembly: OptionalDependency("PolyToolkit.PolyApi", "INCLUDE_POLY_TOOLKIT")]

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Provides access to the Poly Module
    /// </summary>
    public interface IPoly
    {
    }

#if INCLUDE_POLY_TOOLKIT
    public static class IPolyMethods
    {
        internal delegate void GetFeaturedModelsDelegate(PolyOrderBy orderBy, PolyMaxComplexityFilter complexity,
            PolyFormatFilter? format, PolyCategory category, int requestSize, List<PolyGridAsset> assets,
            Action<string> listCallback, string nextPageToken = null);

        internal static GetFeaturedModelsDelegate getAssetList;

        /// <summary>
        /// Get a list of assets
        /// </summary>
        /// <param name="orderBy">The sorting order of the results</param>
        /// <param name="complexity">The max complexity of assets to request</param>
        /// <param name="format">The format of assets to request</param>
        /// <param name="category">The category of assets to request</param>
        /// <param name="requestSize">The number of assets to request</param>
        /// <param name="assets">The list of poly assets to add the results to</param>
        /// <param name="listCallback">A method which is called when the list is returned</param>
        /// <param name="nextPageToken">(optional) The next page token to pick up on an existing list</param>
        public static void GetAssetList(this IPoly obj, PolyOrderBy orderBy, PolyMaxComplexityFilter complexity,
            PolyFormatFilter? format, PolyCategory category, int requestSize, List<PolyGridAsset>
            assets, Action<string> listCallback, string nextPageToken = null)
        {
            getAssetList(orderBy, complexity, format, category, requestSize, assets, listCallback, nextPageToken);
        }
    }
#endif
}

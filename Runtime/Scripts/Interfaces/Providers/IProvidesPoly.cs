using Unity.Labs.EditorXR.Workspaces;
using Unity.Labs.ModuleLoader;

#if INCLUDE_POLY_TOOLKIT
using System;
using System.Collections.Generic;
using PolyToolkit;
#endif

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to the spatial hash
    /// </summary>
    interface IProvidesPoly : IFunctionalityProvider
    {
#if INCLUDE_POLY_TOOLKIT
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
        void GetAssetList(PolyOrderBy orderBy, PolyMaxComplexityFilter complexity,
            PolyFormatFilter? format, PolyCategory category, int requestSize, List<PolyGridAsset>
                assets, Action<string> listCallback, string nextPageToken = null);
#endif
    }
}

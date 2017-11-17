#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Workspaces;
#if INCLUDE_POLY_TOOLKIT
using PolyToolkit;

#endif

[assembly: OptionalDependency("PolyToolkit.PolyApi", "INCLUDE_POLY_TOOLKIT")]

#if INCLUDE_POLY_TOOLKIT
namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Provides access to the Poly Module
    /// </summary>
    public interface IPoly
    {
    }

    public static class IPolyMethods
    {
        internal delegate void GetFeaturedModelsDelegate(PolyOrderBy orderBy, PolyMaxComplexityFilter complexity,
            PolyFormatFilter? format, PolyCategory category, List<PolyGridAsset> assets, Action<string> listCallback,
            string nextPageToken = null);

        internal static GetFeaturedModelsDelegate getFeaturedModels;

        public static void GetFeaturedModels(this IPoly obj, PolyOrderBy orderBy, PolyMaxComplexityFilter complexity,
            PolyFormatFilter? format, PolyCategory category, List<PolyGridAsset> assets, Action<string> listCallback,
            string nextPageToken = null)
        {
            getFeaturedModels(orderBy, complexity, format, category, assets, listCallback, nextPageToken);
        }
    }
}
#endif
#endif

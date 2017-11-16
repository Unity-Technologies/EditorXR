#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Workspaces;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Provides access to the Web Module
    /// </summary>
    public interface IPoly
    {
    }

    public static class IPolyMethods
    {
        internal static Action<List<PolyAsset>, Action<string>, string> getFeaturedModels;

        public static void GetFeaturedModels(this IPoly obj, List<PolyAsset> assets, Action<string> listCallback, string nextPageToken = null)
        {
            getFeaturedModels(assets, listCallback, nextPageToken);
        }
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Workspaces;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Provides access to the Web Module
    /// </summary>
    public interface IBlocks
    {
    }

    public static class IBlocksMethods
    {
        internal static Action<List<BlocksAsset>, Action<string>, string> getFeaturedModels;

        public static void GetFeaturedModels(this IBlocks obj, List<BlocksAsset> assets, Action<string> listCallback, string nextPageToken = null)
        {
            getFeaturedModels(assets, listCallback, nextPageToken);
        }
    }
}
#endif

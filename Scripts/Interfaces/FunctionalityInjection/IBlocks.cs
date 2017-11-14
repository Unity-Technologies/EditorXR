#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Provides access to the Web Module
    /// </summary>
    public interface IBlocks { }

    public static class IBlocksMethods
    {
        internal static Action<List<BlocksAsset>> getFeaturedModels;

        public static void GetFeaturedModels(this IBlocks obj, List<BlocksAsset> assets)
        {
            getFeaturedModels(assets);
        }
    }
}
#endif

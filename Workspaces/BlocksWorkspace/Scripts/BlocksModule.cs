#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR;
using UnityEngine;

#if INCLUDE_POLY_TOOLKIT
using PolyToolkit;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEditor.Experimental.EditorVR.Workspaces;
#endif

[assembly: OptionalDependency("PolyToolkit.PolyApi", "INCLUDE_POLY_TOOLKIT")]

#if INCLUDE_POLY_TOOLKIT
namespace UnityEditor.Experimental.EditorVR.Modules
{
    public class BlocksModule : MonoBehaviour, IWeb
    {
        class RequestHandler
        {
            List<BlocksAsset> m_Assets;
            Transform m_Container;
            Action<string> m_ListCallback;

            public RequestHandler(List<BlocksAsset> assets, Transform container, Action<string> listCallback, string nextPageToken = null)
            {
                m_Assets = assets;
                m_Container = container;
                m_ListCallback = listCallback;
                var request = PolyListAssetsRequest.Featured();

                request.pageToken = nextPageToken;
                request.pageSize = 50;
                PolyApi.ListAssets(request, ListAssetsCallback);
            }

            // Callback invoked when the featured assets results are returned.
            void ListAssetsCallback(PolyStatusOr<PolyListAssetsResult> result)
            {
                if (!result.Ok)
                {
                    Debug.LogError("Failed to get featured assets. :( Reason: " + result.Status);
                    return;
                }

                if (m_ListCallback != null)
                    m_ListCallback(result.Value.nextPageToken);

                foreach (var asset in result.Value.assets)
                {
                    BlocksAsset blocksAsset;
                    var name = asset.name;
                    if (!k_AssetCache.TryGetValue(name, out blocksAsset))
                    {
                        blocksAsset = new BlocksAsset(asset, m_Container);
                        k_AssetCache[name] = blocksAsset;
                    }

                    m_Assets.Add(blocksAsset);
                }
            }
        }

        static readonly Dictionary<string, BlocksAsset> k_AssetCache = new Dictionary<string, BlocksAsset>();

        Transform m_Container;

        void Awake()
        {
            PolyApi.Init();
            m_Container = ObjectUtils.CreateEmptyGameObject("Poly Prefabs", transform).transform;
        }

        void OnDestroy()
        {
            k_AssetCache.Clear();
            PolyApi.Shutdown();
        }

        public void GetFeaturedModels(List<BlocksAsset> assets, Action<string> listCallback, string nextPageToken = null)
        {
            new RequestHandler(assets, m_Container, listCallback, nextPageToken);
        }
    }
}
#endif
#endif

#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Workspaces;
using UnityEngine;
#if INCLUDE_POLY_TOOLKIT
using PolyToolkit;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Utilities;
#endif

[assembly: OptionalDependency("PolyToolkit.PolyApi", "INCLUDE_POLY_TOOLKIT")]

#if INCLUDE_POLY_TOOLKIT
namespace UnityEditor.Experimental.EditorVR.Modules
{
    public class PolyModule : MonoBehaviour, IWeb
    {
        public const int RequestSize = 100;

        class RequestHandler
        {
            List<PolyGridAsset> m_Assets;
            Transform m_Container;
            Action<string> m_ListCallback;

            public RequestHandler(PolyOrderBy orderBy, PolyMaxComplexityFilter complexity, PolyFormatFilter? format,
                PolyCategory category, List<PolyGridAsset> assets, Transform container, Action<string> listCallback,
                string nextPageToken = null)
            {
                m_Assets = assets;
                m_Container = container;
                m_ListCallback = listCallback;

                var request = new PolyListAssetsRequest
                {
                    orderBy = orderBy,
                    maxComplexity = complexity,
                    formatFilter = format,
                    category = category
                };

                request.pageToken = nextPageToken;
                request.pageSize = RequestSize;
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
                    PolyGridAsset polyGridAsset;
                    var name = asset.name;
                    if (!k_AssetCache.TryGetValue(name, out polyGridAsset))
                    {
                        polyGridAsset = new PolyGridAsset(asset, m_Container);
                        k_AssetCache[name] = polyGridAsset;
                    }

                    m_Assets.Add(polyGridAsset);
                }
            }
        }

        static readonly Dictionary<string, PolyGridAsset> k_AssetCache = new Dictionary<string, PolyGridAsset>();

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

        public void GetFeaturedModels(PolyOrderBy orderBy, PolyMaxComplexityFilter complexity, PolyFormatFilter? format,
            PolyCategory category, List<PolyGridAsset> assets, Action<string> listCallback, string nextPageToken = null)
        {
            new RequestHandler(orderBy, complexity, format,category, assets, m_Container, listCallback, nextPageToken);
        }
    }
}
#endif
#endif

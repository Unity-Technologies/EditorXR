#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR;
using UnityEngine;
using PolyAsset = UnityEditor.Experimental.EditorVR.Workspaces.PolyAsset;

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
            List<PolyAsset> m_Assets;
            Transform m_Container;
            Action<string> m_ListCallback;

            public RequestHandler(PolyOrderBy orderBy, PolyMaxComplexityFilter complexity, PolyFormatFilter? format,
                PolyCategory category, List<PolyAsset> assets, Transform container, Action<string> listCallback,
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
                    PolyAsset polyAsset;
                    var name = asset.name;
                    if (!k_AssetCache.TryGetValue(name, out polyAsset))
                    {
                        polyAsset = new PolyAsset(asset, m_Container);
                        k_AssetCache[name] = polyAsset;
                    }

                    m_Assets.Add(polyAsset);
                }
            }
        }

        static readonly Dictionary<string, PolyAsset> k_AssetCache = new Dictionary<string, PolyAsset>();

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
            PolyCategory category, List<PolyAsset> assets, Action<string> listCallback, string nextPageToken = null)
        {
            new RequestHandler(orderBy, complexity, format,category, assets, m_Container, listCallback, nextPageToken);
        }
    }
}
#endif
#endif

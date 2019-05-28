using System;
using System.Text;
using Unity.Labs.ModuleLoader;
using UnityEditor.Experimental.EditorVR.Workspaces;
using UnityEngine;

#if INCLUDE_POLY_TOOLKIT
using PolyToolkit;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Utilities;
#endif

#if UNITY_EDITOR
using Unity.Labs.Utils;

[assembly: OptionalDependency("PolyToolkit.PolyApi", "INCLUDE_POLY_TOOLKIT")]
#endif

#if INCLUDE_POLY_TOOLKIT
namespace UnityEditor.Experimental.EditorVR.Modules
{
    public class PolyModule : MonoBehaviour, IModule, IWeb
    {
        class RequestHandler
        {
            List<PolyGridAsset> m_Assets;
            Transform m_Container;
            Action<string> m_ListCallback;

            public RequestHandler(PolyOrderBy orderBy, PolyMaxComplexityFilter complexity, PolyFormatFilter? format,
                PolyCategory category, int requestSize, List<PolyGridAsset> assets, Transform container,
                Action<string> listCallback, string nextPageToken = null)
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
                request.pageSize = requestSize;
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

        const string k_APIKey = "QUl6YVN5QUZvMEp6ZVZZRFNDSURFa3hlWmdMNjg0OUM0MThoWlYw";

        static readonly Dictionary<string, PolyGridAsset> k_AssetCache = new Dictionary<string, PolyGridAsset>();

        Transform m_Container;

        public void LoadModule()
        {
            PolyApi.Init(new PolyAuthConfig(Encoding.UTF8.GetString(Convert.FromBase64String(k_APIKey)), "", ""));
            m_Container = EditorXRUtils.CreateEmptyGameObject("Poly Prefabs", transform).transform;
            IPolyMethods.getAssetList = GetAssetList;
        }

        public void UnloadModule()
        {
            k_AssetCache.Clear();
            PolyApi.Shutdown();
        }

        public void GetAssetList(PolyOrderBy orderBy, PolyMaxComplexityFilter complexity, PolyFormatFilter? format,
            PolyCategory category, int requestSize, List<PolyGridAsset> assets, Action<string> listCallback,
            string nextPageToken = null)
        {
            new RequestHandler(orderBy, complexity, format,category, requestSize, assets, m_Container, listCallback, nextPageToken);
        }
    }
}
#endif

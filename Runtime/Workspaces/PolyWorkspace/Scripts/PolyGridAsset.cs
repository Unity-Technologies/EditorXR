using System;
using Unity.Labs.EditorXR.Interfaces;
using UnityEngine;
using Unity.Labs.ListView;

using Unity.Labs.ModuleLoader;
#if INCLUDE_POLY_TOOLKIT
using PolyToolkit;
using UnityEngine.Networking;
#endif

namespace Unity.Labs.EditorXR.Workspaces
{
    class PolyGridAsset : IListViewItemData<string>, IUsesWeb
    {
        const int k_MaxPreviewComplexity = 2500;
        static readonly string k_TemplateName = "PolyGridItem";

#if INCLUDE_POLY_TOOLKIT
        static PolyImportOptions s_Options;
#else
#pragma warning disable 649
#endif

        readonly PolyAsset m_Asset;
        readonly Transform m_Container; // Parent object under which to store imported prefabs--should be cleared on reset
        readonly long m_Complexity; // Cached to avoid loop lookup

        GameObject m_Prefab;
        Texture2D m_Thumbnail;
        bool m_Initialized; // Whether the download/import process has started
        bool m_Importing;

        public string template { get { return k_TemplateName; } }

        public string index { get; private set; }
        public PolyAsset asset { get { return m_Asset; } }
        public GameObject prefab { get { return m_Prefab; } }
        public Texture2D thumbnail { get { return m_Thumbnail; } }
        public bool initialized { get { return m_Initialized; } }
        public long complexity { get { return m_Complexity; } }

#if !FI_AUTOFILL
        IProvidesWeb IFunctionalitySubscriber<IProvidesWeb>.provider { get; set; }
#endif

#if INCLUDE_POLY_TOOLKIT
        public event Action<PolyGridAsset, GameObject> modelImportCompleted;
        public event Action<PolyGridAsset, Texture2D> thumbnailImportCompleted;

        static PolyGridAsset()
        {
            s_Options = PolyImportOptions.Default();
            s_Options.rescalingMode = PolyImportOptions.RescalingMode.FIT;
            s_Options.desiredSize = 1.0f;
            s_Options.recenter = true;
        }
#endif

        public PolyGridAsset(PolyAsset asset, Transform container)
        {
#if INCLUDE_POLY_TOOLKIT
            m_Asset = asset;
            index = asset.name; // PolyAsset.name is the GUID
            m_Container = container;
            m_Complexity = 0L;
            foreach (var format in asset.formats)
            {
                m_Complexity = Math.Max(m_Complexity, format.formatComplexity.triangleCount);
            }
#endif
        }

        public void Initialize()
        {
#if INCLUDE_POLY_TOOLKIT
            m_Initialized = true;

            GetThumbnail();

            if (m_Complexity < k_MaxPreviewComplexity)
                ImportModel();
#endif
        }

        public void ImportModel()
        {
#if INCLUDE_POLY_TOOLKIT
            if (m_Prefab == null && !m_Importing)
                PolyApi.Import(asset, s_Options, ImportAssetCallback);

            m_Importing = true;
#endif
        }

#if INCLUDE_POLY_TOOLKIT
        // Callback invoked when an asset has just been imported.
        void ImportAssetCallback(PolyAsset asset, PolyStatusOr<PolyImportResult> result)
        {
            m_Importing = false;
            if (!result.Ok)
            {
                Debug.LogError("Failed to import asset. :( Reason: " + result.Status);
                return;
            }

            var importedAsset = result.Value.gameObject;
            importedAsset.transform.parent = m_Container;
            importedAsset.SetActive(false);
            m_Prefab = importedAsset;
            m_Prefab.name = asset.displayName;

            if (modelImportCompleted != null)
                modelImportCompleted(this, m_Prefab);
        }

        void GetThumbnail()
        {
            var thumbnail = m_Asset.thumbnail;
            if (thumbnail == null)
                return;

            this.Download<DownloadHandlerTexture>(thumbnail.url, request =>
            {
                var handler = (DownloadHandlerTexture)request.downloadHandler;
                m_Thumbnail = handler.texture;
                if (m_Thumbnail == null)
                    return;

                m_Thumbnail.wrapMode = TextureWrapMode.Clamp;

                if (thumbnailImportCompleted != null)
                    thumbnailImportCompleted(this, m_Thumbnail);
            });
        }
#endif
    }

#if !INCLUDE_POLY_TOOLKIT
    // Stub classes to avoid too many #ifs
    class PolyAsset
    {
        public string name;
        public string displayName;
        public PolyThumbnail thumbnail;
    }

    class PolyThumbnail
    {
        public string url;
    }
    #pragma warning restore 649
#endif
}

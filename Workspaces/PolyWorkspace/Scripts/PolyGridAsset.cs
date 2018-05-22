
using System;
using UnityEditor.Experimental.EditorVR;
using UnityEngine;
using ListView;

#if INCLUDE_POLY_TOOLKIT
using PolyToolkit;
#endif

[assembly: OptionalDependency("PolyToolkit.PolyApi", "INCLUDE_POLY_TOOLKIT")]

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    public class PolyGridAsset : ListViewItemData<string>, IWeb
    {
        const int k_MaxPreviewComplexity = 2500;

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

        public PolyAsset asset { get { return m_Asset; } }
        public GameObject prefab { get { return m_Prefab; } }
        public Texture2D thumbnail { get { return m_Thumbnail; } }
        public bool initialized { get { return m_Initialized; } }
        public long complexity { get { return m_Complexity; } }
        public override string index { get { return m_Asset.name; } } // PolyAsset.name is the GUID

        public event Action<PolyGridAsset, GameObject> modelImportCompleted;
        public event Action<PolyGridAsset, Texture2D> thumbnailImportCompleted;

#if INCLUDE_POLY_TOOLKIT
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
            m_Container = container;
            m_Complexity = 0L;
            foreach (var format in asset.formats)
            {
                m_Complexity = Math.Max(m_Complexity, format.formatComplexity.triangleCount);
            }
#endif

            template = "PolyGridItem";
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

            this.DownloadTexture(thumbnail.url, handler =>
            {
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
    public class PolyAsset
    {
        public string name;
        public string displayName;
        public PolyThumbnail thumbnail;
    }

    public class PolyThumbnail
    {
        public string url;
    }
#else
    #pragma warning restore 618
#endif
}


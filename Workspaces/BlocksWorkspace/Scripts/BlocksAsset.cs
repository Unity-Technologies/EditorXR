#if UNITY_EDITOR

using ListView;
using PolyToolkit;
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    public class BlocksAsset : ListViewItemData<string>, IWeb
    {
        const int k_MaxPreviewComplexity = 2500;

        static PolyImportOptions s_Options;

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
        public bool importing { get { return m_Importing; } }
        public long complexity { get { return m_Complexity; } }
        public override string index { get { return m_Asset.name; } }

        public event Action<BlocksAsset, GameObject> modelImportCompleted;
        public event Action<BlocksAsset, Texture2D> thumbnailImportCompleted;

        static BlocksAsset()
        {
            s_Options = PolyImportOptions.Default();
            s_Options.rescalingMode = PolyImportOptions.RescalingMode.FIT;
            s_Options.desiredSize = 1.0f;
            s_Options.recenter = true;
        }

        public BlocksAsset(PolyAsset asset, Transform container)
        {
            m_Asset = asset;
            m_Container = container;
            m_Complexity = 0L;
            foreach (var format in asset.formats)
            {
                m_Complexity = Math.Max(m_Complexity, format.formatComplexity.triangleCount);
            }

            template = "BlocksGridItem";
        }

        public void Initialize()
        {
            m_Initialized = true;

            GetThumbnail(asset);

            if (m_Complexity < k_MaxPreviewComplexity)
                ImportModel();
        }

        public void ImportModel()
        {
            if (m_Prefab == null && !m_Importing)
                PolyApi.Import(asset, s_Options, ImportAssetCallback);

            m_Importing = true;
        }

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

        void GetThumbnail(PolyAsset asset)
        {
            this.DownloadTexture(asset.thumbnail.url, handler =>
            {
                m_Thumbnail = handler.texture;
                m_Thumbnail.wrapMode = TextureWrapMode.Clamp;

                if (thumbnailImportCompleted != null)
                    thumbnailImportCompleted(this, m_Thumbnail);
            });
        }
    }
}
#endif

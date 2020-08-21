using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.ListViewFramework;
using UnityEditor;
using UnityEngine;

namespace Unity.EditorXR.Data
{
    sealed class FolderData : NestedListViewItemData<FolderData, int>
    {
        const string k_TemplateName = "FolderListItem";

        // Maximum time (in ms) before yielding in CreateFolderData: should be target frame time
        const float k_MaxFrameTime = 0.01f;

        // Minimum time to spend loading the project folder before yielding
        const float k_MinFrameTime = 0.005f;
        static float s_ProjectFolderLoadStartTime;
        static float s_ProjectFolderLoadYieldTime;

        List<AssetData> m_Assets;
        readonly int m_Index;
        int m_Depth;

        public string name { get; private set; }
        public List<AssetData> assets { get { return m_Assets; } }
        public override int index { get { return m_Index; } }
        public override string template { get { return k_TemplateName; } }

        public FolderData(string name, int guid, int depth)
        {
            this.name = name;
            m_Index = guid;
            m_Depth = depth;
        }

#if UNITY_EDITOR
        public static IEnumerator CreateRootFolderData(HashSet<string> assetTypes, Action<FolderData> callback)
        {
            var hp = new HierarchyProperty(HierarchyType.Assets);
            hp.SetSearchFilter("t:object", 0);

            var folderStack = new Stack<FolderData>();
            var folder = new FolderData(hp.name, hp.guid.GetHashCode(), hp.depth);
            while (hp.Next(null))
            {
                while (hp.depth <= folder.m_Depth)
                    folder = folderStack.Pop();

                if (hp.isFolder)
                {
                    var folderList = folder.m_Children;
                    if (folderList == null)
                    {
                        folderList = new List<FolderData>();
                        folder.m_Children = folderList;
                    }

                    folderStack.Push(folder);
                    folder = new FolderData(hp.name, hp.guid.GetHashCode(), hp.depth);
                    folderList.Add(folder);
                }
                else if (hp.isMainRepresentation) // Ignore sub-assets (mixer children, terrain splats, etc.)
                {
                    var assetList = folder.m_Assets;
                    if (assetList == null)
                    {
                        assetList = new List<AssetData>();
                        folder.m_Assets = assetList;
                    }

                    assetList.Add(CreateAssetData(hp, assetTypes));
                }

                // Spend a minimum amount of time in this function, and if we have extra time in the frame, use it
                var time = Time.realtimeSinceStartup;
                if (time - s_ProjectFolderLoadYieldTime > k_MaxFrameTime
                    && time - s_ProjectFolderLoadStartTime > k_MinFrameTime)
                {
                    s_ProjectFolderLoadYieldTime = time;
                    yield return null;
                    s_ProjectFolderLoadStartTime = time;
                }
            }

            while (folderStack.Count > 0)
                folder = folderStack.Pop();

            callback(folder);
        }

        static AssetData CreateAssetData(HierarchyProperty hp, HashSet<string> assetTypes = null)
        {
            var typeName = string.Empty;
            if (assetTypes != null)
            {
                var path = AssetDatabase.GUIDToAssetPath(hp.guid);
                if (Path.GetExtension(path) == ".asset") // Some .assets cause a hitch when getting their type
                {
                    typeName = "Asset";
                }
                else
                {
                    var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                    if (type != null)
                    {
                        typeName = type.Name;
                        switch (typeName)
                        {
                            case "MonoScript":
                                typeName = "Script";
                                break;
                            case "SceneAsset":
                                typeName = "Scene";
                                break;
                            case "AudioMixerController":
                                typeName = "AudioMixer";
                                break;
                        }
                    }
                }

                assetTypes.Add(typeName);
            }

            return new AssetData(hp.name, hp.guid, typeName);
        }
#else
        public void SetAssetList(List<AssetData> list)
        {
            m_Assets = list;
        }
#endif
    }
}

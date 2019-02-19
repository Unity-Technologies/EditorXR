#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    sealed class ProjectFolderModule : MonoBehaviour, ISystemModule
    {
        // Maximum time (in ms) before yielding in CreateFolderData: should be target frame time
        const float k_MaxFrameTime = 0.01f;

        // Minimum time to spend loading the project folder before yielding
        const float k_MinProjectFolderLoadTime = 0.005f;

        readonly List<IFilterUI> m_FilterUIs = new List<IFilterUI>();

        readonly List<IUsesProjectFolderData> m_ProjectFolderLists = new List<IUsesProjectFolderData>();
        List<FolderData> m_FolderData;
        readonly HashSet<string> m_AssetTypes = new HashSet<string>();
        float m_ProjectFolderLoadStartTime;
        float m_ProjectFolderLoadYieldTime;

        void OnEnable()
        {
#if UNITY_2018_1_OR_NEWER
            EditorApplication.projectChanged += UpdateProjectFolders;
#else
            EditorApplication.projectWindowChanged += UpdateProjectFolders;
#endif
            UpdateProjectFolders();
        }

        void OnDisable()
        {
#if UNITY_2018_1_OR_NEWER
            EditorApplication.projectChanged -= UpdateProjectFolders;
#else
            EditorApplication.projectWindowChanged -= UpdateProjectFolders;
#endif
        }

        public void AddConsumer(IUsesProjectFolderData consumer)
        {
            consumer.folderData = GetFolderData();
            m_ProjectFolderLists.Add(consumer);
        }

        public void RemoveConsumer(IUsesProjectFolderData consumer)
        {
            m_ProjectFolderLists.Remove(consumer);
        }

        public void AddConsumer(IFilterUI consumer)
        {
            consumer.filterList = GetFilterList();
            m_FilterUIs.Add(consumer);
        }

        public void RemoveConsumer(IFilterUI consumer)
        {
            m_FilterUIs.Remove(consumer);
        }

        List<string> GetFilterList()
        {
            return m_AssetTypes.ToList();
        }

        List<FolderData> GetFolderData()
        {
            if (m_FolderData == null)
                m_FolderData = new List<FolderData>();

            return m_FolderData;
        }

        void UpdateProjectFolders()
        {
            m_AssetTypes.Clear();

            StartCoroutine(CreateFolderData((folderData, hasNext) =>
            {
                m_FolderData = new List<FolderData> { folderData };

                // Send new data to existing folderLists
                foreach (var list in m_ProjectFolderLists)
                {
                    list.folderData = GetFolderData();
                }

                // Send new data to existing filterUIs
                foreach (var filterUI in m_FilterUIs)
                {
                    filterUI.filterList = GetFilterList();
                }
            }, m_AssetTypes));
        }

        IEnumerator CreateFolderData(Action<FolderData, bool> callback, HashSet<string> assetTypes, bool hasNext = true, HierarchyProperty hp = null)
        {
            if (hp == null)
            {
                hp = new HierarchyProperty(HierarchyType.Assets);
                hp.SetSearchFilter("t:object", 0);
            }
            var name = hp.name;
            var guid = hp.guid;
            var depth = hp.depth;
            var folderList = new List<FolderData>();
            var assetList = new List<AssetData>();
            if (hasNext)
            {
                hasNext = hp.Next(null);
                while (hasNext && hp.depth > depth)
                {
                    if (hp.isFolder)
                    {
                        yield return StartCoroutine(CreateFolderData((data, next) =>
                        {
                            folderList.Add(data);
                            hasNext = next;
                        }, assetTypes, hasNext, hp));
                    }
                    else if (hp.isMainRepresentation) // Ignore sub-assets (mixer children, terrain splats, etc.)
                    {
                        assetList.Add(CreateAssetData(hp, assetTypes));
                    }

                    if (hasNext)
                        hasNext = hp.Next(null);

                    // Spend a minimum amount of time in this function, and if we have extra time in the frame, use it
                    if (Time.realtimeSinceStartup - m_ProjectFolderLoadYieldTime > k_MaxFrameTime
                        && Time.realtimeSinceStartup - m_ProjectFolderLoadStartTime > k_MinProjectFolderLoadTime)
                    {
                        m_ProjectFolderLoadYieldTime = Time.realtimeSinceStartup;
                        yield return null;
                        m_ProjectFolderLoadStartTime = Time.realtimeSinceStartup;
                    }
                }

                if (hasNext)
                    hp.Previous(null);
            }

            callback(new FolderData(name, folderList.Count > 0 ? folderList : null, assetList, guid), hasNext);
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
    }
}
#endif

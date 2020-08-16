using System.Collections.Generic;
using System.Linq;
using Unity.EditorXR.Core;
using Unity.EditorXR.Data;
using Unity.XRTools.ModuleLoader;
using UnityEditor;
using UnityEngine;

namespace Unity.EditorXR.Modules
{
#if UNITY_EDITOR
    sealed class ProjectFolderModule : MonoBehaviour, IDelayedInitializationModule, IInterfaceConnector
    {
        readonly List<IFilterUI> m_FilterUIs = new List<IFilterUI>();

        readonly List<IUsesProjectFolderData> m_ProjectFolderLists = new List<IUsesProjectFolderData>();
        List<FolderData> m_FolderData;
        readonly HashSet<string> m_AssetTypes = new HashSet<string>();
        float m_ProjectFolderLoadStartTime;
        float m_ProjectFolderLoadYieldTime;
        IModule m_ModuleImplementation;

        public int initializationOrder { get { return 0; } }
        public int shutdownOrder { get { return 0; } }
        public int connectInterfaceOrder { get { return 0; } }

        public void Initialize()
        {
            EditorApplication.projectChanged += UpdateProjectFolders;
            UpdateProjectFolders();
        }

        public void Shutdown()
        {
            EditorApplication.projectChanged -= UpdateProjectFolders;
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
            StartCoroutine(FolderData.CreateRootFolderData(m_AssetTypes, SetupFolderData));
        }

        void SetupFolderData(FolderData folderData)
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
        }

        public void LoadModule() { }

        public void UnloadModule() { }

        public void ConnectInterface(object target, object userData = null)
        {
            var usesProjectFolderData = target as IUsesProjectFolderData;
            if (usesProjectFolderData != null)
            {
                AddConsumer(usesProjectFolderData);

                var filterUI = target as IFilterUI;
                if (filterUI != null)
                    AddConsumer(filterUI);
            }
        }

        public void DisconnectInterface(object target, object userData = null)
        {
            var usesProjectFolderData = target as IUsesProjectFolderData;
            if (usesProjectFolderData != null)
            {
                RemoveConsumer(usesProjectFolderData);

                var filterUI = target as IFilterUI;
                if (filterUI != null)
                    RemoveConsumer(filterUI);
            }
        }
    }
#else
    sealed class ProjectFolderModule : MonoBehaviour
    {
    }
#endif
    }

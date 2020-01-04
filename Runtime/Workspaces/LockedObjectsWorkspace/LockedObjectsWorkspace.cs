#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.EditorXR.Utilities;
using Unity.Labs.ModuleLoader;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Labs.EditorXR.Workspaces
{
    [MainMenuItem("Locked Objects", "Workspaces", "View all locked objects in your scene(s)")]
    [SpatialMenuItem("Locked Objects", "Workspaces", "View all locked objects in your scene(s)")]
    class LockedObjectsWorkspace : HierarchyWorkspace, IUsesGameObjectLocking
    {
#pragma warning disable 649
        [SerializeField]
        GameObject m_UnlockAllPrefab;
#pragma warning restore 649

        string m_BaseSearchQuery;
        string m_CachedSearchQuery;

#if !FI_AUTOFILL
        IProvidesGameObjectLocking IFunctionalitySubscriber<IProvidesGameObjectLocking>.provider { get; set; }
#endif

        public override string searchQuery
        {
            get
            {
                var query = base.searchQuery;
                if (m_BaseSearchQuery != query)
                {
                    m_BaseSearchQuery = query;
                    m_CachedSearchQuery = string.Format("{0} {1}", m_BaseSearchQuery, k_Locked);
                }

                return m_CachedSearchQuery;
            }
        }

        public override List<string> filterList
        {
            set
            {
                m_FilterList = value;
                m_FilterList.Sort();

                if (m_FilterUI)
                    m_FilterUI.filterList = value;
            }
        }

        public override void Setup()
        {
            base.Setup();

            if (m_UnlockAllPrefab)
            {
                var unlockAllUI = EditorXRUtils.Instantiate(m_UnlockAllPrefab, m_WorkspaceUI.frontPanel, false);
                foreach (var mb in unlockAllUI.GetComponentsInChildren<MonoBehaviour>())
                {
                    this.ConnectInterfaces(mb);
                    this.InjectFunctionalitySingle(mb);
                }

                unlockAllUI.GetComponentInChildren<Button>(true).onClick.AddListener(UnlockAll);
            }
        }

        void UnlockAll()
        {
            UnlockAll(m_HierarchyData);
        }

        void UnlockAll(List<HierarchyData> hierarchyData)
        {
            if (hierarchyData == null || hierarchyData.Count == 0)
                return;

            if (!hierarchyData[0].gameObject)
                hierarchyData = hierarchyData[0].children;

            foreach (var hd in hierarchyData)
            {
                this.SetLocked(hd.gameObject, false);

                UnlockAll(hd.children);
            }
        }
    }
}
#endif

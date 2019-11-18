using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    sealed class SelectionModule : ScriptableSettings<SelectionModule>, IDelayedInitializationModule,
        IUsesGameObjectLocking, IUsesControlHaptics, IRayToNode, IUsesContainsVRPlayerCompletely,
        IProvidesGrouping, IProvidesSelectObject, IInterfaceConnector
    {
#pragma warning disable 649
        [SerializeField]
        HapticPulse m_HoverPulse;
#pragma warning restore 649

        GameObject m_CurrentGroupRoot;
        readonly Dictionary<GameObject, GameObject> m_GroupMap = new Dictionary<GameObject, GameObject>(); // Maps objects to their group parent
        readonly List<GameObject> m_StandardIgnoreList = new List<GameObject>();
        public List<GameObject> standardIgnoreList { get { return m_StandardIgnoreList; } }

        event Action selectionChanged;

        public Func<GameObject, bool> overrideSelectObject { private get; set; }

        public event Action<Transform> selected;

        public int initializationOrder { get { return 0; } }
        public int shutdownOrder { get { return 0; } }
        public int connectInterfaceOrder { get { return 0; } }

#if !FI_AUTOFILL
        IProvidesGameObjectLocking IFunctionalitySubscriber<IProvidesGameObjectLocking>.provider { get; set; }
        IProvidesControlHaptics IFunctionalitySubscriber<IProvidesControlHaptics>.provider { get; set; }
        IProvidesContainsVRPlayerCompletely IFunctionalitySubscriber<IProvidesContainsVRPlayerCompletely>.provider { get; set; }
#endif

        // Local method use only -- created here to reduce garbage collection
        static readonly HashSet<Object> k_SelectedObjects = new HashSet<Object>();
        static readonly List<GameObject> k_SingleObjectList = new List<GameObject>();
        readonly List<Transform> m_Transforms = new List<Transform>();

        public GameObject GetSelectionCandidate(GameObject hoveredObject, bool useGrouping = false)
        {
            // If we can't even select the object we're starting with, then skip any further logic
            if (!CanSelectObject(hoveredObject, false))
                return null;

            // By default the selection candidate would be the same object passed in
            if (!useGrouping)
                return hoveredObject;

            // Only offer up the group root as the selection on first selection; Subsequent selections would allow children from the group
            var groupRoot = GetGroupRoot(hoveredObject);
            if (groupRoot && groupRoot != m_CurrentGroupRoot && CanSelectObject(groupRoot, false))
                return groupRoot;

            return hoveredObject;
        }

        bool CanSelectObject(GameObject hoveredObject, bool useGrouping)
        {
            if (this.IsLocked(hoveredObject))
                return false;

            if (hoveredObject != null)
            {
                if (!hoveredObject.activeInHierarchy)
                    return false;

                if (this.ContainsVRPlayerCompletely(hoveredObject))
                    return false;

                if (useGrouping)
                    return CanSelectObject(GetSelectionCandidate(hoveredObject, true), false);
            }

            return true;
        }

        public void SelectObject(GameObject hoveredObject, Transform rayOrigin, bool multiSelect, bool useGrouping = false)
        {
            k_SingleObjectList.Clear();
            k_SingleObjectList.Add(hoveredObject);
            SelectObjects(k_SingleObjectList, rayOrigin, multiSelect, useGrouping);
        }

        public void SelectObjects(List<GameObject> hoveredObjects, Transform rayOrigin, bool multiSelect, bool useGrouping = false)
        {
            if (hoveredObjects == null || hoveredObjects.Count == 0)
            {
                if (!multiSelect)
                    Selection.activeObject = null;

                return;
            }

            k_SelectedObjects.Clear();

            if (multiSelect)
                k_SelectedObjects.UnionWith(Selection.objects);

            Selection.activeGameObject = hoveredObjects[0];

            if (Selection.activeGameObject)
                this.Pulse(this.RequestNodeFromRayOrigin(rayOrigin), m_HoverPulse);

            foreach (var hoveredObject in hoveredObjects)
            {
                if (overrideSelectObject(hoveredObject))
                    continue;

                var selection = GetSelectionCandidate(hoveredObject, useGrouping);

                var groupRoot = GetGroupRoot(hoveredObject);
                if (useGrouping && groupRoot != m_CurrentGroupRoot)
                    m_CurrentGroupRoot = groupRoot;

                if (multiSelect)
                {
                    // Re-selecting an object removes it from selection, otherwise add it
                    if (!k_SelectedObjects.Remove(selection))
                        k_SelectedObjects.Add(selection);
                }
                else
                {
                    k_SelectedObjects.Add(selection);
                }
            }

            Selection.objects = k_SelectedObjects.ToArray();
            if (selected != null)
                selected(rayOrigin);
        }

        GameObject GetGroupRoot(GameObject hoveredObject)
        {
            if (!hoveredObject)
                return null;

            GameObject groupParent;
            if (m_GroupMap.TryGetValue(hoveredObject, out groupParent))
                return groupParent;

#if UNITY_EDITOR
            var groupRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(hoveredObject);

            if (groupRoot)
                return groupRoot;
#endif

            return null;
        }

        public void MakeGroup(GameObject parent)
        {
            parent.GetComponentsInChildren(m_Transforms);
            foreach (var child in m_Transforms)
            {
                m_GroupMap[child.gameObject] = parent;
            }
        }

        public void LoadModule() { }

        public void UnloadModule() { }

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var groupingSubscriber = obj as IFunctionalitySubscriber<IProvidesGrouping>;
            if (groupingSubscriber != null)
                groupingSubscriber.provider = this;

            var selectObjectSubscriber = obj as IFunctionalitySubscriber<IProvidesSelectObject>;
            if (selectObjectSubscriber != null)
                selectObjectSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }

        public void ConnectInterface(object target, object userData = null)
        {
            var selectionChanged = target as ISelectionChanged;
            if (selectionChanged != null)
                this.selectionChanged += selectionChanged.OnSelectionChanged;

            var standardIgnoreList = target as IStandardIgnoreList;
            if (standardIgnoreList != null)
                standardIgnoreList.ignoreList = this.standardIgnoreList;
        }

        public void DisconnectInterface(object target, object userData = null)
        {
            var selectionChanged = target as ISelectionChanged;
            if (selectionChanged != null)
                this.selectionChanged -= selectionChanged.OnSelectionChanged;
        }

        void OnSelectionChanged()
        {
            // Selection can change outside of this module, so stay in sync
            if (Selection.objects.Length == 0)
                m_CurrentGroupRoot = null;

            if (selectionChanged != null)
                selectionChanged();
        }

        public void Initialize()
        {
            Selection.selectionChanged += OnSelectionChanged;

            // In case we have anything selected at start, set up manipulators, inspector, etc.
            EditorApplication.delayCall += OnSelectionChanged;

            m_StandardIgnoreList.Clear();
        }

        public void Shutdown()
        {
            Selection.selectionChanged -= OnSelectionChanged;

            // Suppress MissingReferenceException in tests
            EditorApplication.delayCall -= OnSelectionChanged;
        }
    }
}

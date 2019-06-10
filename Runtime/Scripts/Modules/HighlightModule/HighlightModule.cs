using System.Collections;
using System.Collections.Generic;
using Unity.Labs.EditorXR.Interfaces;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    [ModuleBehaviorCallbackOrder(ModuleOrders.HighlightModuleBehaviorOrder)]
    sealed class HighlightModule : ScriptableSettings<HighlightModule>, IModuleBehaviorCallbacks, IUsesGameObjectLocking,
        IProvidesCustomHighlight, IProvidesSetHighlight
    {
        struct HighlightData
        {
            public float startTime;
            public float duration;
        }

        const string k_SelectionOutlinePrefsKey = "Scene/Selected Outline";

        static readonly Dictionary<SkinnedMeshRenderer, Mesh> k_BakedMeshes = new Dictionary<SkinnedMeshRenderer, Mesh>();

#pragma warning disable 649
        [SerializeField]
        Material m_DefaultHighlightMaterial;

        [SerializeField]
        Material m_RayHighlightMaterial;
#pragma warning restore 649

        readonly Dictionary<Material, Dictionary<GameObject, HighlightData>> m_Highlights = new Dictionary<Material, Dictionary<GameObject, HighlightData>>();
        readonly Dictionary<Node, HashSet<Transform>> m_NodeMap = new Dictionary<Node, HashSet<Transform>>();

        Dictionary<int, IEnumerator> m_Blinking = new Dictionary<int, IEnumerator>();               // instanceID-keyed
        Dictionary<GameObject, float> m_LastBlinkStartTimes = new Dictionary<GameObject, float>();

        Material m_RayHighlightMaterialCopy;
        Transform m_ModuleParent;

#if !FI_AUTOFILL
        IProvidesGameObjectLocking IFunctionalitySubscriber<IProvidesGameObjectLocking>.provider { get; set; }
#endif

        // Local method use only -- created here to reduce garbage collection
        static readonly List<KeyValuePair<Material, GameObject>> k_HighlightsToRemove = new List<KeyValuePair<Material, GameObject>>();
        static readonly List<MeshFilter> k_MeshFilters = new List<MeshFilter>();
        static readonly List<SkinnedMeshRenderer> k_SkinnedMeshRenderers = new List<SkinnedMeshRenderer>();

        readonly List<OnHighlightMethod> m_CustomHighlightMethods = new List<OnHighlightMethod>();

        public Color highlightColor
        {
            get { return m_RayHighlightMaterialCopy.GetVector("_Color"); }
            set { m_RayHighlightMaterialCopy.color = value; }
        }

        public void LoadModule()
        {
            m_RayHighlightMaterialCopy = Instantiate(m_RayHighlightMaterial);
#if UNITY_EDITOR
            if (EditorPrefs.HasKey(k_SelectionOutlinePrefsKey))
            {
                var selectionColor = EditorMaterialUtils.PrefToColor(EditorPrefs.GetString(k_SelectionOutlinePrefsKey));
                selectionColor.a = 1;
                m_RayHighlightMaterialCopy.color = PlayerSettings.colorSpace == ColorSpace.Gamma ? selectionColor : selectionColor.gamma;
            }
#endif

            m_ModuleParent = ModuleLoaderCore.instance.GetModuleParent().transform;
        }

        public void UnloadModule() { }

        public void OnBehaviorUpdate()
        {
            k_HighlightsToRemove.Clear();
            foreach (var highlight in m_Highlights)
            {
                var material = highlight.Key;
                var highlights = highlight.Value;
                foreach (var kvp in highlights)
                {
                    var go = kvp.Key;
                    if (go == null)
                        continue;

                    var highlightData = kvp.Value;
                    if (highlightData.duration > 0)
                    {
                        var visibleTime = Time.time - highlightData.startTime;
                        if (visibleTime > highlightData.duration)
                            k_HighlightsToRemove.Add(new KeyValuePair<Material, GameObject>(material, go));
                    }

                    var shouldHighlight = true;
                    for (int i = 0; i < m_CustomHighlightMethods.Count; i++)
                    {
                        var func = m_CustomHighlightMethods[i];
                        if (func(go, material))
                            shouldHighlight = false;
                    }

                    if (shouldHighlight)
                        HighlightObject(go, material);
                }
            }

            foreach (var obj in Selection.gameObjects)
            {
                if (!obj)
                    continue;

                if (obj.transform.IsChildOf(m_ModuleParent))
                    continue;

                HighlightObject(obj, m_RayHighlightMaterialCopy);
            }

            foreach (var kvp in k_HighlightsToRemove)
            {
                var highlights = m_Highlights[kvp.Key];
                if (highlights.Remove(kvp.Value) && highlights.Count == 0)
                    m_Highlights.Remove(kvp.Key);
            }

            foreach (var kvp in m_Blinking)
            {
                kvp.Value.MoveNext();
            }
        }

        static void HighlightObject(GameObject go, Material material)
        {
            go.GetComponentsInChildren(k_MeshFilters);
            foreach (var meshFilter in k_MeshFilters)
            {
                var mesh = meshFilter.sharedMesh;
                if (meshFilter.sharedMesh == null)
                    continue;

                var localToWorldMatrix = meshFilter.transform.localToWorldMatrix;
                var layer = meshFilter.gameObject.layer;
                for (var i = 0; i < meshFilter.sharedMesh.subMeshCount; i++)
                {
                    Graphics.DrawMesh(mesh, localToWorldMatrix, material, layer, null, i);
                }
            }

            go.GetComponentsInChildren(k_SkinnedMeshRenderers);
            foreach (var skinnedMeshRenderer in k_SkinnedMeshRenderers)
            {
                if (skinnedMeshRenderer.sharedMesh == null)
                    continue;

                Mesh bakedMesh;
                if (!k_BakedMeshes.TryGetValue(skinnedMeshRenderer, out bakedMesh))
                {
                    bakedMesh = new Mesh();
                    k_BakedMeshes[skinnedMeshRenderer] = bakedMesh;
                }

                skinnedMeshRenderer.BakeMesh(bakedMesh);

                var localToWorldMatrix = skinnedMeshRenderer.transform.localToWorldMatrix * Matrix4x4.Scale(skinnedMeshRenderer.transform.lossyScale.Inverse());
                var layer = skinnedMeshRenderer.gameObject.layer;
                for (var i = 0; i < bakedMesh.subMeshCount; i++)
                {
                    Graphics.DrawMesh(bakedMesh, localToWorldMatrix, material, layer, null, i);
                }
            }
        }

        public void AddRayOriginForNode(Node node, Transform rayOrigin)
        {
            HashSet<Transform> set;
            if (!m_NodeMap.TryGetValue(node, out set))
            {
                set = new HashSet<Transform>();
                m_NodeMap[node] = set;
            }

            set.Add(rayOrigin);
        }

        public void SetHighlight(GameObject go, bool active, Transform rayOrigin = null, Material material = null, bool force = false, float duration = 0f)
        {
            if (go == null)
                return;

            if (go.transform.IsChildOf(m_ModuleParent))
                return;

            if (!force && active && this.IsLocked(go))
                return;

            if (material == null)
            {
                material = rayOrigin ? m_RayHighlightMaterialCopy : m_DefaultHighlightMaterial;
            }

            if (active) // Highlight
            {
                Dictionary<GameObject, HighlightData> highlights;
                if (!m_Highlights.TryGetValue(material, out highlights))
                {
                    highlights = new Dictionary<GameObject, HighlightData>();
                    m_Highlights[material] = highlights;
                }

                highlights[go] = new HighlightData { startTime = Time.time, duration = duration };
            }
            else // Unhighlight
            {
                if (force)
                {
                    // A force removal removes the GameObject regardless of how it was highlighted (e.g. with a specific hand)
                    foreach (var gameObjects in m_Highlights.Values)
                    {
                        gameObjects.Remove(go);
                    }
                }
                else
                {
                    Dictionary<GameObject, HighlightData> highlights;
                    if (m_Highlights.TryGetValue(material, out highlights))
                        highlights.Remove(go);
                }

                var skinnedMeshRenderer = ComponentUtils<SkinnedMeshRenderer>.GetComponent(go);
                if (skinnedMeshRenderer)
                    k_BakedMeshes.Remove(skinnedMeshRenderer);
            }
        }

        public IEnumerator SetBlinkingHighlight(GameObject go, bool active, Transform rayOrigin,
            Material material, bool force, float dutyPercent, float cycleLength)
        {
            if (!active)
            {
                SetHighlight(go, active, rayOrigin, null, true);
                m_Blinking.Clear();
                return null;
            }

            m_LastBlinkStartTimes[go] = Time.time;
            var onDuration = Mathf.Clamp01(dutyPercent) * cycleLength;

            SetHighlight(go, true, rayOrigin, material, false, onDuration);

            var blinker = BlinkHighlight(go, true, rayOrigin, material, false, onDuration, cycleLength);
            m_Blinking.Add(go.GetInstanceID(), blinker);
            return blinker;
        }

        IEnumerator BlinkHighlight(GameObject go, bool active, Transform rayOrigin, Material material,
            bool force, float onTime, float cycleLength)
        {
            while (true)
            {
                float lastBlinkTime;
                m_LastBlinkStartTimes.TryGetValue(go, out lastBlinkTime);

                var now = Time.time;
                if (now - lastBlinkTime >= cycleLength)
                {
                    m_LastBlinkStartTimes[go] = now;
                    SetHighlight(go, true, rayOrigin, material, false, onTime);
                }

                yield return null;
            }
        }

        public void OnBehaviorAwake() { }

        public void OnBehaviorEnable() { }

        public void OnBehaviorStart() { }

        public void OnBehaviorDisable() { }

        public void OnBehaviorDestroy() { }

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var customHighlightSubscriber = obj as IFunctionalitySubscriber<IProvidesCustomHighlight>;
            if (customHighlightSubscriber != null)
                customHighlightSubscriber.provider = this;

            var setHighlightSubscriber = obj as IFunctionalitySubscriber<IProvidesSetHighlight>;
            if (setHighlightSubscriber != null)
                setHighlightSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }

        public void SubscribeToOnHighlight(OnHighlightMethod highlightMethod) { m_CustomHighlightMethods.Add(highlightMethod); }

        public void UnsubscribeFromOnHighlight(OnHighlightMethod highlightMethod) { m_CustomHighlightMethods.Remove(highlightMethod); }
    }
}

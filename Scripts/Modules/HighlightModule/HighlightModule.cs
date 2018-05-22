
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    sealed class HighlightModule : MonoBehaviour, ISystemModule, IUsesGameObjectLocking
    {
        struct HighlightData
        {
            public float startTime;
            public float duration;
        }

        const string k_SelectionOutlinePrefsKey = "Scene/Selected Outline";

        static readonly Dictionary<SkinnedMeshRenderer, Mesh> k_BakedMeshes = new Dictionary<SkinnedMeshRenderer, Mesh>();

        [SerializeField]
        Material m_DefaultHighlightMaterial;

        [SerializeField]
        Material m_RayHighlightMaterial;

        readonly Dictionary<Material, Dictionary<GameObject, HighlightData>> m_Highlights = new Dictionary<Material, Dictionary<GameObject, HighlightData>>();
        readonly Dictionary<Node, HashSet<Transform>> m_NodeMap = new Dictionary<Node, HashSet<Transform>>();

        Dictionary<int, IEnumerator> m_Blinking = new Dictionary<int, IEnumerator>();               // instanceID-keyed
        Dictionary<GameObject, float> m_LastBlinkStartTimes = new Dictionary<GameObject, float>();

        // Local method use only -- created here to reduce garbage collection
        static readonly List<KeyValuePair<Material, GameObject>> k_HighlightsToRemove = new List<KeyValuePair<Material, GameObject>>();
        static readonly List<MeshFilter> k_MeshFilters = new List<MeshFilter>();
        static readonly List<SkinnedMeshRenderer> k_SkinnedMeshRenderers = new List<SkinnedMeshRenderer>();

        public event Func<GameObject, Material, bool> customHighlight
        {
            add { m_CustomHighlightFuncs.Add(value); }
            remove { m_CustomHighlightFuncs.Remove(value); }
        }

        readonly List<Func<GameObject, Material, bool>> m_CustomHighlightFuncs = new List<Func<GameObject, Material, bool>>();

        public Color highlightColor
        {
            get { return m_RayHighlightMaterial.GetVector("_Color"); }
            set { m_RayHighlightMaterial.color = value; }
        }

        void OnEnable()
        {
            m_RayHighlightMaterial = Instantiate(m_RayHighlightMaterial);
            if (EditorPrefs.HasKey(k_SelectionOutlinePrefsKey))
            {
                var selectionColor = MaterialUtils.PrefToColor(EditorPrefs.GetString(k_SelectionOutlinePrefsKey));
                selectionColor.a = 1;
#if UNITY_EDITOR
                m_RayHighlightMaterial.color = PlayerSettings.colorSpace == ColorSpace.Gamma ? selectionColor : selectionColor.gamma;
#else
                m_RayHighlightMaterial.color = selectionColor;
#endif
            }
        }

        void LateUpdate()
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
                    for (int i = 0; i < m_CustomHighlightFuncs.Count; i++)
                    {
                        var func = m_CustomHighlightFuncs[i];
                        if (func(go, material))
                            shouldHighlight = false;
                    }

                    if (shouldHighlight)
                        HighlightObject(go, material);
                }
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
            k_MeshFilters.Clear();
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

            k_SkinnedMeshRenderers.Clear();
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

            if (!force && active && this.IsLocked(go))
                return;

            if (material == null)
            {
                material = rayOrigin ? m_RayHighlightMaterial : m_DefaultHighlightMaterial;
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
                // using StopAll assumes that we're only allowing one simultaneous blinking highlight
                StopAllCoroutines();
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
            while (enabled)
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

    }
}


#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class HighlightModule : MonoBehaviour, IUsesGameObjectLocking
	{
		class HighlightData
		{
			public float startTime;
			public float duration;
		}

		const string k_SelectionOutlinePrefsKey = "Scene/Selected Outline";

		static readonly Dictionary<SkinnedMeshRenderer, Mesh> m_BakedMeshes = new Dictionary<SkinnedMeshRenderer, Mesh>();

		[SerializeField]
		Material m_DefaultHighlightMaterial;

		[SerializeField]
		Material m_RayHighlightMaterial;

		readonly Dictionary<Material, Dictionary<GameObject, HighlightData>> m_Highlights = new Dictionary<Material, Dictionary<GameObject, HighlightData>>();
		readonly Dictionary<Node, HashSet<Transform>> m_NodeMap = new Dictionary<Node, HashSet<Transform>>();

		// Local method use only -- created here to reduce garbage collection
		readonly List<KeyValuePair<Material, GameObject>> m_HighlightsToRemove = new List<KeyValuePair<Material, GameObject>>();

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

		void Awake()
		{
			m_RayHighlightMaterial = Instantiate(m_RayHighlightMaterial);
			if (EditorPrefs.HasKey(k_SelectionOutlinePrefsKey))
			{
				var selectionColor = MaterialUtils.PrefToColor(EditorPrefs.GetString(k_SelectionOutlinePrefsKey));
				selectionColor.a = 1;
				m_RayHighlightMaterial.color = PlayerSettings.colorSpace == ColorSpace.Gamma ? selectionColor : selectionColor.gamma;
			}
		}

		void LateUpdate()
		{
			m_HighlightsToRemove.Clear();
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
							m_HighlightsToRemove.Add(new KeyValuePair<Material, GameObject>(material, go));
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

			foreach (var kvp in m_HighlightsToRemove)
			{
				var highlights = m_Highlights[kvp.Key];
				if (highlights.Remove(kvp.Value) && highlights.Count == 0)
					m_Highlights.Remove(kvp.Key);
			}
		}

		static void HighlightObject(GameObject go, Material material)
		{
			foreach (var meshFilter in go.GetComponentsInChildren<MeshFilter>())
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

			foreach (var skinnedMeshRenderer in go.GetComponentsInChildren<SkinnedMeshRenderer>())
			{
				if (skinnedMeshRenderer.sharedMesh == null)
					continue;

				Mesh bakedMesh;
				if (!m_BakedMeshes.TryGetValue(skinnedMeshRenderer, out bakedMesh))
				{
					bakedMesh = new Mesh();
					m_BakedMeshes[skinnedMeshRenderer] = bakedMesh;
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

				var skinnedMeshRenderer = go.GetComponent<SkinnedMeshRenderer>();
				if (skinnedMeshRenderer)
					m_BakedMeshes.Remove(skinnedMeshRenderer);
			}
		}
	}
}
#endif

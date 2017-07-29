#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
	sealed class HighlightModule : MonoBehaviour, IUsesGameObjectLocking
	{
		const string k_SelectionOutlinePrefsKey = "Scene/Selected Outline";

		[SerializeField]
		Material m_DefaultHighlightMaterial;

		[SerializeField]
		Material m_RayHighlightMaterial;

		readonly Dictionary<Material, HashSet<GameObject>> m_Highlights = new Dictionary<Material, HashSet<GameObject>>();
		readonly Dictionary<Node, HashSet<Transform>> m_NodeMap = new Dictionary<Node, HashSet<Transform>>();

		static Mesh s_BakedMesh;

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
			s_BakedMesh = new Mesh();

			if (EditorPrefs.HasKey(k_SelectionOutlinePrefsKey))
			{
				var selectionColor = MaterialUtils.PrefToColor(EditorPrefs.GetString(k_SelectionOutlinePrefsKey));
				selectionColor.a = 1;

				m_RayHighlightMaterial = Instantiate(m_RayHighlightMaterial);
				m_RayHighlightMaterial.color = selectionColor.gamma;
			}
		}

		void LateUpdate()
		{
			foreach (var highlight in m_Highlights)
			{
				var material = highlight.Key;
				var highlights = highlight.Value;
				foreach (var go in highlights)
				{
					if (go == null)
						continue;

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

			foreach (var obj in Selection.gameObjects)
			{
				if (!obj)
					continue;

				HighlightObject(obj, m_RayHighlightMaterial);
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

				skinnedMeshRenderer.BakeMesh(s_BakedMesh);

				var localToWorldMatrix = skinnedMeshRenderer.transform.localToWorldMatrix;
				var layer = skinnedMeshRenderer.gameObject.layer;
				for (var i = 0; i < s_BakedMesh.subMeshCount; i++)
				{
					Graphics.DrawMesh(s_BakedMesh, localToWorldMatrix, material, layer, null, i);
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

		public void SetHighlight(GameObject go, bool active, Transform rayOrigin = null, Material material = null, bool force = false)
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
				HashSet<GameObject> gameObjects;
				if (!m_Highlights.TryGetValue(material, out gameObjects))
				{
					gameObjects = new HashSet<GameObject>();
					m_Highlights[material] = gameObjects;
				}
				gameObjects.Add(go);
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
					HashSet<GameObject> gameObjects;
					if (m_Highlights.TryGetValue(material, out gameObjects))
					{
						gameObjects.Remove(go);
					}
				}
			}
		}
	}
}
#endif

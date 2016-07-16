using UnityEngine;
using System.Collections;
using UnityEngine.VR.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.EventSystems;

public class SelectionTool : MonoBehaviour, ITool, IRay, IRaycaster, IStandardActionMap
{
	[SerializeField]
	private Material m_HighlightMaterial;

	private GameObject m_CurrentOver;

	private static readonly Dictionary<Renderer, Material[]> s_SavedMaterials = new Dictionary<Renderer,Material[]>();
	private static readonly Dictionary<GameObject, int> s_CurrentPointers = new Dictionary<GameObject, int>();

	public Func<Transform, GameObject> GetGameObjectOver { private get; set; }

	public Transform RayOrigin { get; set; }

	public Standard StandardInput { get; set; }

	void Update()
	{
		var newOver = GetGameObjectOver(RayOrigin);
		if (newOver != m_CurrentOver)
		{
			if(m_CurrentOver != null)
				OnHoverExit(m_CurrentOver);

			if(newOver != null)
				OnHoverEnter(newOver);

			m_CurrentOver = newOver;
		}

		if (StandardInput.action.wasJustPressed)
			Selection.activeGameObject = m_CurrentOver;
	}

	void OnDestroy()
	{
		foreach (var kvp in s_SavedMaterials)
			kvp.Key.sharedMaterials = kvp.Value;
	}

	private void OnHoverEnter(GameObject go)
	{
		if (!s_CurrentPointers.ContainsKey(go))
			s_CurrentPointers.Add(go, 1);
		else
			s_CurrentPointers[go]++;

		if (s_CurrentPointers[go] == 1)
		{
			var renderers = go.GetComponentsInChildren<Renderer>();
			foreach (var ren in renderers)
			{
				if (!s_SavedMaterials.ContainsKey(ren))
					s_SavedMaterials.Add(ren, ren.sharedMaterials);
				ren.sharedMaterial = m_HighlightMaterial; // TODO Should change all materials not just the first
			}
		}
	}

	private void OnHoverExit(GameObject go)
	{
		if (!s_CurrentPointers.ContainsKey(go))
		{
			Debug.Log("Selection tool hover exiting go that was never entered.");
			return;
		}
		else
			s_CurrentPointers[go]--;

		if (s_CurrentPointers[go] == 0)
		{
			var renderers = go.GetComponentsInChildren<Renderer>();
			foreach (var ren in renderers)
			{
				ren.sharedMaterials = s_SavedMaterials[ren];
				s_SavedMaterials.Remove(ren);
			}
		}
	}
}

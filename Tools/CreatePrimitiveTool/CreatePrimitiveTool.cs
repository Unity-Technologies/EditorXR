using System;
using UnityEngine;
using UnityEditor.VR;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;
using UnityEngine.InputNew;

[MainMenuItem("Primitive", "Primitive", "Create primitives in the scene")]
public class CreatePrimitiveTool : MonoBehaviour, ITool, IStandardActionMap, IRay, IInstantiateUI
{
	[SerializeField]
	private Canvas CanvasPrefab;
	private Canvas m_ToolCanvas;

	public Standard standardInput
	{
		get; set;
	}

	public Transform rayOrigin
	{
		get; set;
	}

	public Func<GameObject, GameObject> instantiateUI
	{
		private get; set;
	}

	void Update()
	{
		if (standardInput.action.wasJustPressed)
		{
			if (m_ToolCanvas == null)
			{
				var go = instantiateUI(CanvasPrefab.gameObject);
				m_ToolCanvas = go.GetComponent<Canvas>();
			}
			m_ToolCanvas.transform.position = rayOrigin.position + rayOrigin.forward*5f;
			m_ToolCanvas.transform.rotation = Quaternion.LookRotation(m_ToolCanvas.transform.position - VRView.viewerCamera.transform.position);
		}
	}
	void OnDestroy()
	{
		if (m_ToolCanvas != null)
			U.Object.Destroy(m_ToolCanvas.gameObject);
	}
}

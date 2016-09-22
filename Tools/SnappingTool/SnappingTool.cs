using UnityEngine;
using System.Collections;
using UnityEngine.VR.Utilities;
using UnityEngine.VR.Tools;
using System;
using UnityEngine.InputNew;
using UnityEditor.VR;

[UnityEngine.VR.Tools.MainMenuItem("Snapping", "Transform", "Select snapping modes")]
public class SnappingTool : MonoBehaviour, ITool, IRay, IRaycaster, ICustomActionMap, IInstantiateUI
{

	[SerializeField]
	private Canvas ToolCanvasPrefab;
	private Canvas m_ToolCanvas;

	public ActionMap actionMap
	{
		get { return m_ActionMap; }
	}
	[SerializeField]
	private ActionMap m_ActionMap;

	public ActionMapInput actionMapInput
	{
		get { return m_ActionMapInput; }
		set { m_ActionMapInput = (SnappingInput)value; }
	}
	private SnappingInput m_ActionMapInput;

	public Func<Transform, GameObject> getFirstGameObject { private get; set; }

	public Transform rayOrigin { private get; set; }

	public Func<GameObject, GameObject> instantiateUI { private get; set; }

	void Update()
	{
		if (rayOrigin == null)
			return;

		if (m_ToolCanvas == null)
		{
			var go = instantiateUI(ToolCanvasPrefab.gameObject);
			m_ToolCanvas = go.GetComponent<Canvas>();
			m_ToolCanvas.transform.SetParent(rayOrigin, false);
		}

		if (m_ActionMapInput.trigger.wasJustPressed)
		{
			print("Go team!");
		}
		if (m_ActionMapInput.trigger.wasJustReleased)
		{
			print("Yaay us!");
		}
	}

	void OnDestroy()
	{
		if (m_ToolCanvas)
			U.Object.Destroy(m_ToolCanvas.gameObject);
	}

}

using System;
using UnityEngine;
using UnityEngine.VR;
using UnityEditor.VR;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;
using UnityEngine.InputNew;

[MainMenuItem("Primitive", "Primitive", "Create primitives in the scene")]
public class CreatePrimitiveTool : MonoBehaviour, ITool, IStandardActionMap, IRay, IInstantiateMenuUI, ICustomRay, IHighlight, IRaycaster
{
	public static PrimitiveType s_SelectedPrimitiveType = PrimitiveType.Cube;
	public static bool s_Freeform = false;

	private GameObject m_CurrentGameObject = null;

	private Vector3 m_PointA = Vector3.zero;
	private Vector3 m_PointB = Vector3.zero;

	private const float kDrawDistance = 0.08f;
	private const float kWaitTime = 0.2f;

	private PrimitiveCreationStates m_State = PrimitiveCreationStates.PointA;

	[SerializeField]
	private Canvas CanvasPrefab;
	private Canvas m_ToolCanvas;
	private bool m_ToolCanvasSpawned = false;
	
	public Standard standardInput {	get; set; }

	public Transform rayOrigin { get; set; }

	public Func<Node,MenuOrigin,GameObject,GameObject> instantiateMenuUI { private get; set; }

	public Action hideDefaultRay { private get; set; }

	public Action showDefaultRay { private get; set; }

	

	private enum PrimitiveCreationStates
	{
		PointA,
		PointB,
		Freeform,
	}

	public Node node { private get; set; }

	public Action<GameObject,bool> setHighlight
	{
		private get; set;
	}
	public Func<Transform,GameObject> getFirstGameObject
	{
		private get; set;
	}

	private GameObject m_HoverGameObject;

	void Update()
	{
		if(!m_ToolCanvasSpawned)
		{
			if(m_ToolCanvas == null)
			{
				var go = instantiateMenuUI(node, MenuOrigin.Main,CanvasPrefab.gameObject);
				m_ToolCanvas = go.GetComponent<Canvas>();
				m_ToolCanvasSpawned = true;
			}
			hideDefaultRay();
			return;
		}

		switch(m_State)
		{
			case PrimitiveCreationStates.PointA:
			{
				if(standardInput.action.wasJustPressed)
				{
					m_CurrentGameObject = GameObject.CreatePrimitive(s_SelectedPrimitiveType);
					m_CurrentGameObject.transform.localScale = new Vector3(0.0025f,0.0025f,0.0025f);

					m_PointA = rayOrigin.position + rayOrigin.forward * kDrawDistance;
					m_CurrentGameObject.transform.position = m_PointA;

					if(s_Freeform)
						m_State = PrimitiveCreationStates.Freeform;
					else
						m_State = PrimitiveCreationStates.PointB;
				}
				break;
			}
			case PrimitiveCreationStates.PointB:
			{
				m_PointB = rayOrigin.position + rayOrigin.forward * kDrawDistance;

				m_CurrentGameObject.transform.position = (m_PointA + m_PointB) * .5f;
				var corner = (m_PointA - m_PointB).magnitude;

				if(s_SelectedPrimitiveType == PrimitiveType.Capsule || s_SelectedPrimitiveType == PrimitiveType.Cylinder)
					m_CurrentGameObject.transform.localScale = Vector3.one * corner * 0.5f;
				else
					m_CurrentGameObject.transform.localScale = Vector3.one * corner;

				if(standardInput.action.wasJustReleased)
					m_State = PrimitiveCreationStates.PointA;

				break;
			}
			case PrimitiveCreationStates.Freeform:
			{
				m_PointB = rayOrigin.position + rayOrigin.forward * kDrawDistance;
				m_CurrentGameObject.transform.position = (m_PointA + m_PointB) * 0.5f;
				Vector3 maxCorner = Vector3.Max(m_PointA,m_PointB);
				Vector3 minCorner = Vector3.Min(m_PointA,m_PointB);
				m_CurrentGameObject.transform.localScale = (maxCorner - minCorner);

				if(standardInput.action.wasJustReleased)
					m_State = PrimitiveCreationStates.PointA;

				break;
			}
		}

		if(rayOrigin == null)
			return;

		var newHoverGameObject = getFirstGameObject(rayOrigin);
		if(newHoverGameObject != m_HoverGameObject)
		{
			if(m_HoverGameObject != null)
				setHighlight(m_HoverGameObject,false);

			if(newHoverGameObject != null)
				setHighlight(newHoverGameObject,true);
		}

		m_HoverGameObject = newHoverGameObject;
	}

	void OnDestroy()
	{
		if (m_ToolCanvas != null)
			U.Object.Destroy(m_ToolCanvas.gameObject);
	}
}
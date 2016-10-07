using System;
using UnityEngine;
using UnityEngine.VR;
using UnityEditor.VR;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;
using UnityEngine.InputNew;
using UnityEngine.VR.Handles;


[MainMenuItem("Primitive", "Primitive", "create primitives")]
public class CreatePrimitiveTool : MonoBehaviour, ITool, IStandardActionMap, IRay, IInstantiateMenuUI, ICustomRay, IHighlight, IRaycaster
{
	public static PrimitiveType s_SelectedPrimitiveType = PrimitiveType.Cube;
	public static bool s_Freeform = false;

	[SerializeField]
	Canvas CanvasPrefab;
	private GameObject m_CurrentGameObject = null;

	private Vector3 m_PointA = Vector3.zero;
	private Vector3 m_PointB = Vector3.zero;

	private const float kDrawDistance = 0.075f;
	private const float kWaitTime = 0.2f;

	private PrimitiveCreationStates m_State = PrimitiveCreationStates.PointA;

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

	private bool m_ToolCanvasSpawned;

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

	BaseHandle m_SphereHandle;
	BaseHandle m_CubeHandle;

	void Awake()
	{
		m_SphereHandle = CanvasPrefab.GetComponent<CreatePrimitiveMenu>().Sphere.GetComponent<BaseHandle>();
		m_CubeHandle = CanvasPrefab.GetComponent<CreatePrimitiveMenu>().Cube.GetComponent<BaseHandle>();

		m_SphereHandle.hoverStarted += OnHandleHoverStarted;
		m_SphereHandle.hoverEnded += OnHandleHoverEnded;
		m_CubeHandle.hoverStarted += OnHandleHoverStarted;
		m_CubeHandle.hoverEnded += OnHandleHoverEnded;
	}

	void Update()
	{
		
		if(!m_ToolCanvasSpawned)
		{
			var go = instantiateMenuUI(node, MenuOrigin.Main,CanvasPrefab.gameObject);
			m_ToolCanvasSpawned = true;
			hideDefaultRay();
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

	void OnHandleHoverStarted(BaseHandle handle,HandleEventData eventData = default(HandleEventData))
	{
		setHighlight(handle.gameObject,true);
	}

	void OnHandleHoverEnded(BaseHandle handle,HandleEventData eventData = default(HandleEventData))
	{
		setHighlight(handle.gameObject,false);
	}
}
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using UnityEngine.VR.Tools;
using UnityEngine.InputNew;
using UnityEngine.VR.Actions;

[MainMenuItem("Primitive", "Primitive", "create primitives")]
public class CreatePrimitiveTool : MonoBehaviour, ITool, IStandardActionMap, IRay, IInstantiateMenuUI, ICustomRay, IToolActions
{
	class PrimitiveToolAction : IAction
	{
		public Action execute;
		public Sprite icon { get; internal set; }
		public bool ExecuteAction()
		{
			execute();
			return true;
		}
	}

	[SerializeField]
	Sprite m_CubeIcon;
	[SerializeField]
	Sprite m_SphereIcon;
	[SerializeField]
	Sprite m_CapsuleIcon;
	[SerializeField]
	Sprite m_PlaneIcon;
	[SerializeField]
	Sprite m_QuadIcon;
	[SerializeField]
	Sprite m_CylinderIcon;

	readonly PrimitiveToolAction m_CreateCubeAction = new PrimitiveToolAction();
	readonly PrimitiveToolAction m_CreateSphereAction = new PrimitiveToolAction();
	readonly PrimitiveToolAction m_CreateCapsuleAction = new PrimitiveToolAction();
	readonly PrimitiveToolAction m_CreatePlaneAction = new PrimitiveToolAction();
	readonly PrimitiveToolAction m_CreateQuadAction = new PrimitiveToolAction();
	readonly PrimitiveToolAction m_CreateCylinderAction = new PrimitiveToolAction();
	readonly PrimitiveToolAction m_FreeformCubeAction = new PrimitiveToolAction();

	private PrimitiveType m_SelectedPrimitiveType = PrimitiveType.Cube;
	private bool m_Freeform = false;

	[SerializeField]
	private Canvas m_CanvasPrefab;
	private bool m_CanvasSpawned;

	private GameObject m_CurrentGameObject = null;

	private const float kDrawDistance = 0.075f;

	private Vector3 m_PointA = Vector3.zero;
	private Vector3 m_PointB = Vector3.zero;

	private PrimitiveCreationStates m_State = PrimitiveCreationStates.PointA;

	public Standard standardInput {	get; set; }

	public Transform rayOrigin { get; set; }

	public Func<Node,MenuOrigin,GameObject,GameObject> instantiateMenuUI { private get; set; }

	public Action hideDefaultRay { private get; set; }

	public Action showDefaultRay { private get; set; }

	public Node selfNode { private get; set; }

	public List<IAction> toolActions { get; private set; }

	public event Action<Node?> startRadialMenu = delegate { };

	private enum PrimitiveCreationStates
	{
		PointA,
		PointB,
		Freeform,
	}

	void Awake()
	{
		m_CreateCubeAction.icon = m_CubeIcon;
		m_CreateCubeAction.execute = SetSelectedToCube;

		m_CreateSphereAction.icon = m_SphereIcon;
		m_CreateCubeAction.execute = SetSelectedToSphere;

		m_CreateCapsuleAction.icon = m_CapsuleIcon;
		m_CreateCapsuleAction.execute = SetSelectedToCapsule;

		m_CreatePlaneAction.icon = m_PlaneIcon;
		m_CreatePlaneAction.execute = SetSelectedToPlane;

		m_CreateQuadAction.icon = m_QuadIcon;
		m_CreateQuadAction.execute = SetSelectedToQuad;

		m_CreateCylinderAction.icon = m_CylinderIcon;
		m_CreateCylinderAction.execute = SetSelectedToCylinder;

		m_FreeformCubeAction.icon = m_CubeIcon;
		m_FreeformCubeAction.execute = SetSelectedToFreeform;

		toolActions = new List<IAction>() { m_CreateCubeAction,
											m_CreateSphereAction,
											m_CreateCapsuleAction,
											m_CreatePlaneAction,
											m_CreateQuadAction,
											m_CreateCylinderAction,
											m_FreeformCubeAction };

	}

	void Update()
	{
		if (!m_CanvasSpawned)
		{
			//SpawnCanvas();
		}

		switch(m_State)
		{
			case PrimitiveCreationStates.PointA:
			{
				HandlePointA();
				break;
			}
			case PrimitiveCreationStates.PointB:
			{
				UpdatePositions();
				SetScalingForObjectType();
				CheckForTriggerRelease();
				break;
			}
			case PrimitiveCreationStates.Freeform:
			{
				UpdatePositions();
				UpdateFreeformScale();
				CheckForTriggerRelease();
				break;
			}
		}
	}

	void SpawnCanvas()
	{
		//hideDefaultRay();
		var go = instantiateMenuUI(selfNode,MenuOrigin.Main,m_CanvasPrefab.gameObject);
		go.GetComponent<CreatePrimitiveMenu>().selectPrimitive += SetSelectedPrimitive;
		m_CanvasSpawned = true;
	}

	void SetSelectedPrimitive(PrimitiveType type,bool isFreeform)
	{
		m_SelectedPrimitiveType = type;
		m_Freeform = isFreeform;
	}

	void HandlePointA()
	{
		if(standardInput.action.wasJustPressed)
		{
			m_CurrentGameObject = GameObject.CreatePrimitive(m_SelectedPrimitiveType);
			m_CurrentGameObject.transform.localScale = new Vector3(0.0025f,0.0025f,0.0025f);
			m_PointA = rayOrigin.position + rayOrigin.forward * kDrawDistance;
			m_CurrentGameObject.transform.position = m_PointA;

			if(m_Freeform)
				m_State = PrimitiveCreationStates.Freeform;
			else
				m_State = PrimitiveCreationStates.PointB;
		}
	}

	void SetScalingForObjectType()
	{
		var corner = (m_PointA - m_PointB).magnitude;

		// it feels better to scale the capsule and cylinder type primitives vertically with the drawpoint
		if(m_SelectedPrimitiveType == PrimitiveType.Capsule || m_SelectedPrimitiveType == PrimitiveType.Cylinder)
			m_CurrentGameObject.transform.localScale = Vector3.one * corner * 0.5f;
		else
			m_CurrentGameObject.transform.localScale = Vector3.one * corner;
	}

	void UpdatePositions()
	{
		m_PointB = rayOrigin.position + rayOrigin.forward * kDrawDistance;
		m_CurrentGameObject.transform.position = (m_PointA + m_PointB) * 0.5f;
	}

	void UpdateFreeformScale()
	{
		Vector3 maxCorner = Vector3.Max(m_PointA,m_PointB);
		Vector3 minCorner = Vector3.Min(m_PointA,m_PointB);
		m_CurrentGameObject.transform.localScale = (maxCorner - minCorner);
	}

	void CheckForTriggerRelease()
	{
		if(standardInput.action.wasJustReleased)
			m_State = PrimitiveCreationStates.PointA;
	}

	void SetSelectedToCube()
	{
		m_SelectedPrimitiveType = PrimitiveType.Cube;
		m_Freeform = false;
	}

	void SetSelectedToSphere()
	{
		m_SelectedPrimitiveType = PrimitiveType.Sphere;
		m_Freeform = false;
	}

	void SetSelectedToCapsule()
	{
		m_SelectedPrimitiveType = PrimitiveType.Capsule;
		m_Freeform = false;
	}

	void SetSelectedToPlane()
	{
		m_SelectedPrimitiveType = PrimitiveType.Plane;
		m_Freeform = false;
	}

	void SetSelectedToQuad()
	{
		m_SelectedPrimitiveType = PrimitiveType.Quad;
		m_Freeform = false;
	}

	void SetSelectedToCylinder()
	{
		m_SelectedPrimitiveType = PrimitiveType.Cylinder;
		m_Freeform = false;
	}

	void SetSelectedToFreeform()
	{
		m_SelectedPrimitiveType = PrimitiveType.Cube;
		m_Freeform = true;
	}
}

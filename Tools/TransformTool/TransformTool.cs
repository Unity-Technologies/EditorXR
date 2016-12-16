#if !UNITY_EDITOR
#pragma warning disable 414, 649
#endif

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.Experimental.EditorVR.Actions;
using UnityEngine.Experimental.EditorVR.Manipulators;
using UnityEngine.Experimental.EditorVR.Modules;
using UnityEngine.Experimental.EditorVR.Tools;
using UnityEngine.Experimental.EditorVR.Utilities;

public class TransformTool : MonoBehaviour, ITool, ITransformer, ISelectionChanged, IActions, IDirectSelection, IGrabObject, ISetHighlight, ICustomRay, IProcessInput, IUsesViewerBody, IDeleteSceneObject
{
	const float kLazyFollowTranslate = 8f;
	const float kLazyFollowRotate = 12f;

	class GrabData
	{
		public Transform grabbedObject;
		public Transform rayOrigin;
		public Vector3 positionOffset;
		public Quaternion rotationOffset;
		public Vector3 initialScale;
		public DirectSelectInput input;

		public GrabData(Transform rayOrigin, Transform grabbedObject, DirectSelectInput input)
		{
			this.rayOrigin = rayOrigin;
			this.grabbedObject = grabbedObject;
			this.input = input;
			Reset();
		}

		public void Reset()
		{
			U.Math.GetTransformOffset(rayOrigin, grabbedObject, out positionOffset, out rotationOffset);
			initialScale = grabbedObject.transform.localScale;
		}

		public void PositionObject()
		{
			U.Math.SetTransformOffset(rayOrigin, grabbedObject, positionOffset, rotationOffset);
		}

		public void ScaleObject(float scaleFactor)
		{
			grabbedObject.position = rayOrigin.position + positionOffset * scaleFactor;
			grabbedObject.localScale = initialScale * scaleFactor;
		}
	}

	class TransformAction : IAction
	{
		internal Func<bool> execute;
		public Sprite icon { get; internal set; }
		public void ExecuteAction()
		{
			if (execute != null)
				execute();
		}
	}

	public List<IAction> actions
	{
		get
		{
			if (m_Actions == null)
			{
				m_Actions = new List<IAction>()
				{
					m_PivotModeToggleAction,
					m_PivotRotationToggleAction,
					m_ManipulatorToggleAction
				};
			}
			return m_Actions;
		}
	}
	List<IAction> m_Actions;

	[SerializeField]
	Sprite m_OriginCenterIcon;
	[SerializeField]
	Sprite m_OriginPivotIcon;
	[SerializeField]
	Sprite m_RotationGlobalIcon;
	[SerializeField]
	Sprite m_RotationLocalIcon;
	[SerializeField]
	Sprite m_StandardManipulatorIcon;
	[SerializeField]
	Sprite m_ScaleManipulatorIcon;

	[SerializeField]
	GameObject m_StandardManipulatorPrefab;
	[SerializeField]
	GameObject m_ScaleManipulatorPrefab;

	BaseManipulator m_CurrentManipulator;

	BaseManipulator m_StandardManipulator;
	BaseManipulator m_ScaleManipulator;

	Bounds m_SelectionBounds;
	Vector3 m_TargetPosition;
	Quaternion m_TargetRotation;
	Vector3 m_TargetScale;
	Quaternion m_PositionOffsetRotation;
	Quaternion m_StartRotation;

	readonly Dictionary<Transform, Vector3> m_PositionOffsets = new Dictionary<Transform, Vector3>();
	readonly Dictionary<Transform, Quaternion> m_RotationOffsets = new Dictionary<Transform, Quaternion>();
	readonly Dictionary<Transform, Vector3> m_ScaleOffsets = new Dictionary<Transform, Vector3>();

#if UNITY_EDITOR
	PivotRotation m_PivotRotation = PivotRotation.Local;
	PivotMode m_PivotMode = PivotMode.Pivot;
#endif

	readonly Dictionary<Node, GrabData> m_GrabData = new Dictionary<Node, GrabData>();
	bool m_DirectSelected;
	float m_ScaleStartDistance;
	Node m_ScaleFirstNode;
	float m_ScaleFactor;
	bool m_WasScaling;

	public DefaultRayVisibilityDelegate showDefaultRay { private get; set; }
	public DefaultRayVisibilityDelegate hideDefaultRay { private get; set; }
	public Func<Transform, object, bool> lockRay { private get; set; }
	public Func<Transform, object, bool> unlockRay { private get; set; }

	readonly TransformAction m_PivotModeToggleAction = new TransformAction();
	readonly TransformAction m_PivotRotationToggleAction = new TransformAction();
	readonly TransformAction m_ManipulatorToggleAction = new TransformAction();

	Dictionary<Transform, DirectSelectionData> m_LastDirectSelection;
	public Func<Dictionary<Transform, DirectSelectionData>> getDirectSelection { private get; set; }

	public Func<DirectSelectionData, Transform, bool> canGrabObject { private get; set; }
	public Func<IGrabObject, DirectSelectionData, Transform, bool> grabObject { private get; set; }
	public Action<IGrabObject, Transform, Transform> dropObject { private get; set; }

	public Action<GameObject, bool> setHighlight { private get; set; }

	public Func<Transform, bool> isOverShoulder { private get; set; }

	public Action<GameObject> deleteSceneObject { private get; set; }

	void Awake()
	{
#if UNITY_EDITOR
		m_PivotModeToggleAction.execute = TogglePivotMode;
		UpdatePivotModeToggleIcon();
		m_PivotRotationToggleAction.execute = TogglePivotRotation;
		UpdatePivotRotationToggleIcon();
		m_ManipulatorToggleAction.execute = ToggleManipulator;
		UpdateManipulatorToggleIcon();
#endif

		// Add standard and scale manipulator prefabs to a list (because you cannot add asset references directly to a serialized list)
		if (m_StandardManipulatorPrefab != null)
			m_StandardManipulator = CreateManipulator(m_StandardManipulatorPrefab);

		if (m_ScaleManipulatorPrefab != null)
			m_ScaleManipulator = CreateManipulator(m_ScaleManipulatorPrefab);

		m_CurrentManipulator = m_StandardManipulator;
	}

	public void OnSelectionChanged()
	{
		// Reset direct selection state in case of a ray selection
		m_DirectSelected = false;

		if (Selection.gameObjects.Length == 0)
			m_CurrentManipulator.gameObject.SetActive(false);
		else
			UpdateCurrentManipulator();
	}

	public void ProcessInput(ActionMapInput input, Action<InputControl> consumeControl)
	{
		var manipulatorGameObject = m_CurrentManipulator.gameObject;

		var directSelection = getDirectSelection();
		var hasObject = false;

		if (m_LastDirectSelection != null)
		{
			foreach (var selection in m_LastDirectSelection.Values)
			{
				setHighlight(selection.gameObject, false);
			}
		}

		foreach(var selection in directSelection.Values)
		{
			setHighlight(selection.gameObject, true);
		}

		m_LastDirectSelection = directSelection;
		if (!m_CurrentManipulator.dragging)
		{
			var hasLeft = m_GrabData.ContainsKey(Node.LeftHand);
			var hasRight = m_GrabData.ContainsKey(Node.RightHand);
			hasObject = directSelection.Count > 0 || hasLeft || hasRight;

			// Disable manipulator on direct hover or drag
			if (manipulatorGameObject.activeSelf && hasObject)
				manipulatorGameObject.SetActive(false);

			foreach (var kvp in directSelection)
			{
				var selection = kvp.Value;
				var rayOrigin = kvp.Key;

#if UNITY_EDITOR
				// If gameObject is within a prefab and not the current prefab, choose prefab root
				var prefabRoot = PrefabUtility.FindPrefabRoot(selection.gameObject);
				if(prefabRoot)
				{
					selection.gameObject = prefabRoot;
				}
#endif

				if (!canGrabObject(selection, rayOrigin))
					continue;

				var directSelectInput = (DirectSelectInput)selection.input;
				if (directSelectInput.select.wasJustPressed)
				{
					if (!grabObject(this, selection, rayOrigin))
						continue;

					consumeControl(directSelectInput.select);

					var grabbedObject = selection.gameObject.transform;

					// Check if the other hand is already grabbing for two-handed scale
					foreach (var grabData in m_GrabData)
					{
						var otherNode = grabData.Key;
						if (otherNode != selection.node)
						{
							m_ScaleStartDistance = (rayOrigin.position - grabData.Value.rayOrigin.position).magnitude;
							m_ScaleFirstNode = otherNode;
							grabData.Value.positionOffset = grabbedObject.position - grabData.Value.rayOrigin.position;
							break;
						}
					}

					m_GrabData[selection.node] = new GrabData(rayOrigin, grabbedObject, directSelectInput);

					Selection.activeGameObject = grabbedObject.gameObject;

					setHighlight(grabbedObject.gameObject, false);

					hideDefaultRay(rayOrigin, true);
					lockRay(rayOrigin, this);

					// Wait a frame since OnSelectionChanged is called after setting m_DirectSelected to true
					EditorApplication.delayCall += () =>
					{
						// A direct selection has been made. Hide the manipulator until the selection changes
						m_DirectSelected = true;
					};
				}
			}

			GrabData leftData;
			hasLeft = m_GrabData.TryGetValue(Node.LeftHand, out leftData);

			GrabData rightData;
			hasRight = m_GrabData.TryGetValue(Node.RightHand, out rightData);

			var leftHeld = leftData != null && leftData.input.select.isHeld;
			var rightHeld = rightData != null && rightData.input.select.isHeld;
			if (hasLeft && hasRight && leftHeld && rightHeld && leftData.grabbedObject == rightData.grabbedObject) // Two-handed scaling
			{
				// Offsets will change while scaling. Whichever hand keeps holding the trigger after scaling is done will need to reset itself
				m_WasScaling = true;

				m_ScaleFactor = (leftData.rayOrigin.position - rightData.rayOrigin.position).magnitude / m_ScaleStartDistance;
				if (m_ScaleFactor > 0 && m_ScaleFactor < Mathf.Infinity)
				{
					if (m_ScaleFirstNode == Node.LeftHand)
						leftData.ScaleObject(m_ScaleFactor);
					else
						rightData.ScaleObject(m_ScaleFactor);
				}
			}
			else
			{
				if (m_WasScaling)
				{
					// Reset initial conditions
					if (hasLeft)
						leftData.Reset();
					if (hasRight)
						rightData.Reset();

					m_WasScaling = false;
				}

				if (hasLeft && leftHeld)
					leftData.PositionObject();

				if (hasRight && rightHeld)
					rightData.PositionObject();
			}

			if (hasLeft && leftData.input.select.wasJustReleased)
			{
				DropObject(Node.LeftHand);
				consumeControl(leftData.input.select);
			}

			if (hasRight && rightData.input.select.wasJustReleased)
			{
				DropObject(Node.RightHand);
				consumeControl(rightData.input.select);
			}
		}

		// Manipulator is disabled while direct manipulation is happening
		if (hasObject || m_DirectSelected)
			return;

		if (Selection.gameObjects.Length > 0)
		{
			if (!m_CurrentManipulator.dragging)
				UpdateCurrentManipulator();
			
			var deltaTime = Time.unscaledDeltaTime;
			var manipulatorTransform = manipulatorGameObject.transform;
			manipulatorTransform.position = Vector3.Lerp(manipulatorTransform.position, m_TargetPosition, kLazyFollowTranslate * deltaTime);

#if UNITY_EDITOR
			if (m_PivotRotation == PivotRotation.Local) // Manipulator does not rotate when in global mode
				manipulatorTransform.rotation = Quaternion.Slerp(manipulatorTransform.rotation, m_TargetRotation, kLazyFollowRotate * deltaTime);
#endif

			foreach (var t in Selection.transforms)
			{
				t.rotation = Quaternion.Slerp(t.rotation, m_TargetRotation * m_RotationOffsets[t], kLazyFollowRotate * deltaTime);

#if UNITY_EDITOR
				if (m_PivotMode == PivotMode.Center) // Rotate the position offset from the manipulator when rotating around center
				{
					m_PositionOffsetRotation = Quaternion.Slerp(m_PositionOffsetRotation, m_TargetRotation * Quaternion.Inverse(m_StartRotation), kLazyFollowRotate * deltaTime);
					t.position = manipulatorTransform.position + m_PositionOffsetRotation * m_PositionOffsets[t];
				}
				else
#endif
				{
					t.position = manipulatorTransform.position + m_PositionOffsets[t];
				}

				t.localScale = Vector3.Lerp(t.localScale, Vector3.Scale(m_TargetScale, m_ScaleOffsets[t]), kLazyFollowTranslate * deltaTime);
			}
		}
	}

	public void DropHeldObject(Transform obj)
	{
		Vector3 position;
		Quaternion rotation;
		DropHeldObject(obj, out position, out rotation);
	}

	public void DropHeldObject(Transform obj, out Vector3 positionOffset, out Quaternion rotationOffset)
	{
		foreach (var kvp in m_GrabData)
		{
			var grabData = kvp.Value;
			if (grabData.grabbedObject == obj)
			{
				positionOffset = grabData.positionOffset;
				rotationOffset = grabData.rotationOffset;
				DropObject(kvp.Key);
				return;
			}
		}

		positionOffset = Vector3.zero;
		rotationOffset = Quaternion.identity;
	}

	public Transform GetHeldObject(Transform rayOrigin)
	{
		foreach (var grabData in m_GrabData.Values)
		{
			if (grabData.rayOrigin == rayOrigin)
				return grabData.grabbedObject;
		}

		return null;
	}

	public void TransferHeldObject(Transform rayOrigin, ActionMapInput input, Transform destRayOrigin, Vector3 deltaOffset)
	{
		foreach (var grabData in m_GrabData.Values)
		{
			if (grabData.rayOrigin == rayOrigin)
			{
				grabData.rayOrigin = destRayOrigin;
				grabData.positionOffset += deltaOffset;
				grabData.input = (DirectSelectInput)input;
				grabData.PositionObject();
				return;
			}
		}
	}

	public void AddHeldObject(Node node, Transform rayOrigin, Transform grabbedObject, ActionMapInput input)
	{
		m_GrabData[node] = new GrabData(rayOrigin, grabbedObject, (DirectSelectInput)input);
	}

	void DropObject(Node inputNode)
	{
		var grabData = m_GrabData[inputNode];
		dropObject(this, grabData.grabbedObject, grabData.rayOrigin);
		m_GrabData.Remove(inputNode);

		unlockRay(grabData.rayOrigin, this);
		showDefaultRay(grabData.rayOrigin, true);
	}

	private void Translate(Vector3 delta)
	{
		m_TargetPosition += delta;
	}

	private void Rotate(Quaternion delta)
	{
		m_TargetRotation = delta * m_TargetRotation;
	}

	private void Scale(Vector3 delta)
	{
		m_TargetScale += delta;
	}

	private void UpdateSelectionBounds()
	{		
		m_SelectionBounds = U.Object.GetBounds(Selection.gameObjects);
	}

	BaseManipulator CreateManipulator(GameObject prefab)
	{
		var go = U.Object.Instantiate(prefab, transform, active: false);
		go.SetActive(false);
		var manipulator = go.GetComponent<BaseManipulator>();
		manipulator.translate = Translate;
		manipulator.rotate = Rotate;
		manipulator.scale = Scale;
		return manipulator;
	}

	private void UpdateCurrentManipulator()
	{
		var selectionTransforms = Selection.transforms;
		if (selectionTransforms.Length <= 0)
			return;

		UpdateSelectionBounds();
		var manipulatorGameObject = m_CurrentManipulator.gameObject;
		manipulatorGameObject.SetActive(true);
		var manipulatorTransform = manipulatorGameObject.transform;
#if UNITY_EDITOR
		var activeTransform = Selection.activeTransform;
		manipulatorTransform.position = m_PivotMode == PivotMode.Pivot ? activeTransform.position : m_SelectionBounds.center;
		manipulatorTransform.rotation = m_PivotRotation == PivotRotation.Global ? Quaternion.identity : activeTransform.rotation;
#endif
		m_TargetPosition = manipulatorTransform.position;
		m_TargetRotation = manipulatorTransform.rotation;
		m_StartRotation = m_TargetRotation;
		m_PositionOffsetRotation = Quaternion.identity;
		m_TargetScale = Vector3.one;

		// Save the initial position, rotation, and scale realtive to the manipulator
		m_PositionOffsets.Clear();
		m_RotationOffsets.Clear();
		m_ScaleOffsets.Clear();

		foreach (var t in selectionTransforms)
		{
			m_PositionOffsets.Add(t, t.position - manipulatorTransform.position);
			m_ScaleOffsets.Add(t, t.localScale);
			m_RotationOffsets.Add(t, Quaternion.Inverse(manipulatorTransform.rotation) * t.rotation);
		}
	}

#if UNITY_EDITOR
	bool TogglePivotMode()
	{
		m_PivotMode = m_PivotMode == PivotMode.Pivot ? PivotMode.Center : PivotMode.Pivot;
		UpdatePivotModeToggleIcon();
		UpdateCurrentManipulator();
		return true;
	}

	void UpdatePivotModeToggleIcon()
	{
		m_PivotModeToggleAction.icon = m_PivotMode == PivotMode.Center ? m_OriginCenterIcon : m_OriginPivotIcon;
	}

	bool TogglePivotRotation()
	{
		m_PivotRotation = m_PivotRotation == PivotRotation.Global ? PivotRotation.Local : PivotRotation.Global;
		UpdatePivotRotationToggleIcon();
		UpdateCurrentManipulator();
		return true;
	}

	void UpdatePivotRotationToggleIcon()
	{
		m_PivotRotationToggleAction.icon = m_PivotRotation == PivotRotation.Global ? m_RotationGlobalIcon : m_RotationLocalIcon;
	}

	bool ToggleManipulator()
	{
		m_CurrentManipulator.gameObject.SetActive(false);

		m_CurrentManipulator = m_CurrentManipulator == m_StandardManipulator ? m_ScaleManipulator : m_StandardManipulator;
		UpdateManipulatorToggleIcon();
		UpdateCurrentManipulator();
		return true;
	}

	void UpdateManipulatorToggleIcon()
	{
		m_ManipulatorToggleAction.icon = m_CurrentManipulator == m_StandardManipulator ? m_ScaleManipulatorIcon : m_StandardManipulatorIcon;
	}
#endif
}

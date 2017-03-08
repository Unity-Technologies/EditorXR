#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Manipulators;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Tools
{
	sealed class TransformTool : MonoBehaviour, ITool, ITransformer, ISelectionChanged, IActions, IUsesDirectSelection, IGrabObjects, ISetHighlight, ICustomRay, IProcessInput, IUsesViewerBody, IDeleteSceneObject, ISelectObject, IManipulatorVisibility
	{
		const float k_LazyFollowTranslate = 8f;
		const float k_LazyFollowRotate = 12f;

		class GrabData
		{
			public Transform rayOrigin;
			public DirectSelectInput input;
			public Vector3[] positionOffsets { get; private set; }
			public Quaternion[] rotationOffsets { get; private set; }
			public Transform[] grabbedObjects;
			Vector3[] initialScales;

			public GrabData(Transform rayOrigin, DirectSelectInput input, Transform[] grabbedObjects)
			{
				this.rayOrigin = rayOrigin;
				this.input = input;
				this.grabbedObjects = grabbedObjects;
				Reset();
			}

			public void Reset()
			{
				var length = grabbedObjects.Length;
				positionOffsets = new Vector3[length];
				rotationOffsets = new Quaternion[length];
				initialScales = new Vector3[length];
				for (int i = 0; i < length; i++)
				{
					var grabbedObject = grabbedObjects[i];
					MathUtilsExt.GetTransformOffset(rayOrigin, grabbedObject, out positionOffsets[i], out rotationOffsets[i]);
					initialScales[i] = grabbedObject.transform.localScale;
				}
			}

			public void UpdatePositions()
			{
				Undo.RecordObjects(grabbedObjects, "Move");

				for (int i = 0; i < grabbedObjects.Length; i++)
				{
					MathUtilsExt.SetTransformOffset(rayOrigin, grabbedObjects[i], positionOffsets[i], rotationOffsets[i]);
				}
			}

			public void ScaleObjects(float scaleFactor)
			{
				Undo.RecordObjects(grabbedObjects, "Move");

				for (int i = 0; i < grabbedObjects.Length; i++)
				{
					var grabbedObject = grabbedObjects[i];
					grabbedObject.position = rayOrigin.position + positionOffsets[i] * scaleFactor;
					grabbedObject.localScale = initialScales[i] * scaleFactor;
				}
			}
		}

		class TransformAction : IAction, ITooltip
		{
			internal Func<bool> execute;
			public string tooltipText { get; internal set; }
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

		PivotRotation m_PivotRotation = PivotRotation.Local;
		PivotMode m_PivotMode = PivotMode.Pivot;

		readonly Dictionary<Node, GrabData> m_GrabData = new Dictionary<Node, GrabData>();
		bool m_DirectSelected;
		float m_ScaleStartDistance;
		Node m_ScaleFirstNode;
		float m_ScaleFactor;
		bool m_WasScaling;

		readonly TransformAction m_PivotModeToggleAction = new TransformAction();
		readonly TransformAction m_PivotRotationToggleAction = new TransformAction();
		readonly TransformAction m_ManipulatorToggleAction = new TransformAction();

		readonly Dictionary<Transform, GameObject> m_HoverObjects = new Dictionary<Transform, GameObject>();

		public Func<Transform, bool> isOverShoulder { private get; set; }
		public Action<GameObject> deleteSceneObject { private get; set; }

		public Func<Transform, object, bool> lockRay { private get; set; }
		public Func<Transform, object, bool> unlockRay { private get; set; }

		public Func<GameObject, Transform, bool> canGrabObject { private get; set; }
		public event Action<GameObject> objectGrabbed;
		public event Action<Transform[], Transform> objectsDropped;

		public Action<GameObject, bool> setHighlight { private get; set; }
		public GetSelectionCandidateDelegate getSelectionCandidate { private get; set; }
		public SelectObjectDelegate selectObject { private get; set; }

		public bool manipulatorVisible { private get; set; }

		void Awake()
		{
			m_PivotModeToggleAction.execute = TogglePivotMode;
			UpdatePivotModeAction();
			m_PivotRotationToggleAction.execute = TogglePivotRotation;
			UpdatePivotRotationAction();
			m_ManipulatorToggleAction.execute = ToggleManipulator;
			UpdateManipulatorAction();

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

		public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
			var hasObject = false;

			var manipulatorGameObject = m_CurrentManipulator.gameObject;
			if (!m_CurrentManipulator.dragging)
			{
				var directSelection = this.GetDirectSelection();

				var hasLeft = m_GrabData.ContainsKey(Node.LeftHand);
				var hasRight = m_GrabData.ContainsKey(Node.RightHand);
				hasObject = directSelection.Count > 0 || hasLeft || hasRight;

				// Disable manipulator on direct hover or drag
				if (manipulatorGameObject.activeSelf && hasObject)
					manipulatorGameObject.SetActive(false);

				foreach (var selection in m_HoverObjects.Values)
				{
					if (selection)
						setHighlight(selection, false);
				}

				m_HoverObjects.Clear();

				foreach (var kvp in directSelection)
				{
					var rayOrigin = kvp.Key;
					var selection = kvp.Value;
					var hoveredObject = selection.gameObject;

					var selectionCandidate = getSelectionCandidate(hoveredObject, true);

					// Can't select this object (it might be locked or static)
					if (hoveredObject && !selectionCandidate)
						return;

					if (selectionCandidate)
						hoveredObject = selectionCandidate;

					if (!canGrabObject(hoveredObject, rayOrigin))
						continue;

					m_HoverObjects[rayOrigin] = hoveredObject; // Store actual hover object to unhighlight next frame

					setHighlight(hoveredObject, true);

					var directSelectInput = (DirectSelectInput)selection.input;
					if (directSelectInput.select.wasJustPressed)
					{
						objectGrabbed(hoveredObject);

						// Only add to selection, don't remove
						if (!Selection.objects.Contains(hoveredObject))
							selectObject(hoveredObject, rayOrigin, directSelectInput.multiSelect.isHeld);

						consumeControl(directSelectInput.select);

						var selectedNode = selection.node;

						// Check if the other hand is already grabbing for two-handed scale
						foreach (var grabData in m_GrabData)
						{
							var otherNode = grabData.Key;
							if (otherNode != selectedNode)
							{
								var otherData = grabData.Value;
								m_ScaleStartDistance = (rayOrigin.position - otherData.rayOrigin.position).magnitude;
								m_ScaleFirstNode = otherNode;
								for (int i = 0; i < otherData.grabbedObjects.Length; i++)
								{
									otherData.positionOffsets[i] = otherData.grabbedObjects[i].position - otherData.rayOrigin.position;
								}
								break;
							}
						}

						m_GrabData[selectedNode] = new GrabData(rayOrigin, directSelectInput, Selection.transforms);

						setHighlight(hoveredObject, false);

						this.HideDefaultRay(rayOrigin, true);
						lockRay(rayOrigin, this);

						// Wait a frame since OnSelectionChanged is called after setting m_DirectSelected to true
						EditorApplication.delayCall += () =>
						{
							// A direct selection has been made. Hide the manipulator until the selection changes
							m_DirectSelected = true;
						};

						Undo.IncrementCurrentGroup();
					}
				}

				GrabData leftData;
				hasLeft = m_GrabData.TryGetValue(Node.LeftHand, out leftData);

				GrabData rightData;
				hasRight = m_GrabData.TryGetValue(Node.RightHand, out rightData);

				var leftHeld = leftData != null && leftData.input.select.isHeld;
				var rightHeld = rightData != null && rightData.input.select.isHeld;
				if (hasLeft && hasRight && leftHeld && rightHeld) // Two-handed scaling
				{
					// Offsets will change while scaling. Whichever hand keeps holding the trigger after scaling is done will need to reset itself
					m_WasScaling = true;

					m_ScaleFactor = (leftData.rayOrigin.position - rightData.rayOrigin.position).magnitude / m_ScaleStartDistance;
					if (m_ScaleFactor > 0 && m_ScaleFactor < Mathf.Infinity)
					{
						if (m_ScaleFirstNode == Node.LeftHand)
							leftData.ScaleObjects(m_ScaleFactor);
						else
							rightData.ScaleObjects(m_ScaleFactor);
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
						leftData.UpdatePositions();

					if (hasRight && rightHeld)
						rightData.UpdatePositions();
				}

				if (hasLeft && leftData.input.select.wasJustReleased)
				{
					DropObjects(Node.LeftHand);
					consumeControl(leftData.input.select);
				}

				if (hasRight && rightData.input.select.wasJustReleased)
				{
					DropObjects(Node.RightHand);
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
				manipulatorTransform.position = Vector3.Lerp(manipulatorTransform.position, m_TargetPosition, k_LazyFollowTranslate * deltaTime);
				if (m_PivotRotation == PivotRotation.Local) // Manipulator does not rotate when in global mode
					manipulatorTransform.rotation = Quaternion.Slerp(manipulatorTransform.rotation, m_TargetRotation, k_LazyFollowRotate * deltaTime);

				Undo.RecordObjects(Selection.transforms, "Move");

				foreach (var t in Selection.transforms)
				{
					t.rotation = Quaternion.Slerp(t.rotation, m_TargetRotation * m_RotationOffsets[t], k_LazyFollowRotate * deltaTime);

					if (m_PivotMode == PivotMode.Center) // Rotate the position offset from the manipulator when rotating around center
					{
						m_PositionOffsetRotation = Quaternion.Slerp(m_PositionOffsetRotation, m_TargetRotation * Quaternion.Inverse(m_StartRotation), k_LazyFollowRotate * deltaTime);
						t.position = manipulatorTransform.position + m_PositionOffsetRotation * m_PositionOffsets[t];
					}
					else
					{
						t.position = manipulatorTransform.position + m_PositionOffsets[t];
					}

					t.localScale = Vector3.Lerp(t.localScale, Vector3.Scale(m_TargetScale, m_ScaleOffsets[t]), k_LazyFollowTranslate * deltaTime);
				}
			}
		}

		public void DropHeldObjects(Transform rayOrigin, out Vector3[] positionOffsets, out Quaternion[] rotationOffsets)
		{
			foreach (var kvp in m_GrabData)
			{
				var grabData = kvp.Value;
				if (grabData.rayOrigin == rayOrigin)
				{
					positionOffsets = grabData.positionOffsets;
					rotationOffsets = grabData.rotationOffsets;
					DropObjects(kvp.Key);
					return;
				}
			}

			positionOffsets = null;
			rotationOffsets = null;
		}

		public Transform[] GetHeldObjects(Transform rayOrigin)
		{
			foreach (var grabData in m_GrabData.Values)
			{
				if (grabData.rayOrigin == rayOrigin)
					return grabData.grabbedObjects.ToArray();
			}

			return null;
		}

		public void TransferHeldObjects(Transform rayOrigin, Transform destRayOrigin, Vector3 deltaOffset)
		{
			foreach (var grabData in m_GrabData.Values)
			{
				if (grabData.rayOrigin == rayOrigin)
				{
					grabData.rayOrigin = destRayOrigin;
					var positionOffsets = grabData.positionOffsets;
					for (int i = 0; i < positionOffsets.Length; i++)
					{
						positionOffsets[i] += deltaOffset;
					}
					grabData.UpdatePositions();

					// Prevent lock from getting stuck
					unlockRay(rayOrigin, this);
					lockRay(destRayOrigin, this);
					return;
				}
			}
		}

		public void GrabObjects(Node node, Transform rayOrigin, ActionMapInput input, Transform[] objects)
		{
			m_GrabData[node] = new GrabData(rayOrigin, (DirectSelectInput)input, objects);
		}

		void DropObjects(Node inputNode)
		{
			var grabData = m_GrabData[inputNode];
			objectsDropped(grabData.grabbedObjects.ToArray(), grabData.rayOrigin);
			m_GrabData.Remove(inputNode);

			unlockRay(grabData.rayOrigin, this);
			this.ShowDefaultRay(grabData.rayOrigin, true);
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

		static void OnDragStarted()
		{
			Undo.IncrementCurrentGroup();
		}

		private void UpdateSelectionBounds()
		{
			m_SelectionBounds = ObjectUtils.GetBounds(Selection.gameObjects);
		}

		BaseManipulator CreateManipulator(GameObject prefab)
		{
			var go = ObjectUtils.Instantiate(prefab, transform, active: false);
			go.SetActive(false);
			var manipulator = go.GetComponent<BaseManipulator>();
			manipulator.translate = Translate;
			manipulator.rotate = Rotate;
			manipulator.scale = Scale;
			manipulator.dragStarted += OnDragStarted;
			return manipulator;
		}

		private void UpdateCurrentManipulator()
		{
			var selectionTransforms = Selection.transforms;
			if (selectionTransforms.Length <= 0)
				return;

			var manipulatorGameObject = m_CurrentManipulator.gameObject;
			manipulatorGameObject.SetActive(manipulatorVisible);

			UpdateSelectionBounds();
			var manipulatorTransform = manipulatorGameObject.transform;
			var activeTransform = Selection.activeTransform;
			manipulatorTransform.position = m_PivotMode == PivotMode.Pivot ? activeTransform.position : m_SelectionBounds.center;
			manipulatorTransform.rotation = m_PivotRotation == PivotRotation.Global ? Quaternion.identity : activeTransform.rotation;
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

		bool TogglePivotMode()
		{
			m_PivotMode = m_PivotMode == PivotMode.Pivot ? PivotMode.Center : PivotMode.Pivot;
			UpdatePivotModeAction();
			UpdateCurrentManipulator();
			return true;
		}

		void UpdatePivotModeAction()
		{
			var isCenter = m_PivotMode == PivotMode.Center;
			m_PivotModeToggleAction.tooltipText = isCenter ? "Manipulator at Center" : "Manipulator at Pivot";
			m_PivotModeToggleAction.icon = isCenter ? m_OriginCenterIcon : m_OriginPivotIcon;
		}

		bool TogglePivotRotation()
		{
			m_PivotRotation = m_PivotRotation == PivotRotation.Global ? PivotRotation.Local : PivotRotation.Global;
			UpdatePivotRotationAction();
			UpdateCurrentManipulator();
			return true;
		}

		void UpdatePivotRotationAction()
		{
			var isGlobal = m_PivotRotation == PivotRotation.Global;
			m_PivotRotationToggleAction.tooltipText = isGlobal ? "Local Rotation" : "Global Rotation";
			m_PivotRotationToggleAction.icon = isGlobal ? m_RotationGlobalIcon : m_RotationLocalIcon;
		}

		bool ToggleManipulator()
		{
			m_CurrentManipulator.gameObject.SetActive(false);

			m_CurrentManipulator = m_CurrentManipulator == m_StandardManipulator ? m_ScaleManipulator : m_StandardManipulator;
			UpdateManipulatorAction();
			UpdateCurrentManipulator();
			return true;
		}

		void UpdateManipulatorAction()
		{
			var isStandard = m_CurrentManipulator == m_StandardManipulator;
			m_ManipulatorToggleAction.tooltipText = isStandard ? "Switch to Scale Manipulator" : "Switch to Standard Manipulator";
			m_ManipulatorToggleAction.icon = isStandard ? m_ScaleManipulatorIcon : m_StandardManipulatorIcon;
		}
	}
}
#endif

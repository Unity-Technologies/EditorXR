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
	sealed class TransformTool : MonoBehaviour, ITool, ITransformer, ISelectionChanged, IActions, IUsesDirectSelection,
		IGrabObjects, ICustomRay, IProcessInput, ISelectObject, IManipulatorVisibility, IUsesSnapping
	{
		const float k_LazyFollowTranslate = 8f;
		const float k_LazyFollowRotate = 12f;
		const float k_DirectLazyFollowTranslate = 20f;
		const float k_DirectLazyFollowRotate = 30f;

		class GrabData
		{
			public Transform rayOrigin;
			public DirectSelectInput input;
			public Vector3[] positionOffsets { get; private set; }
			public Quaternion[] rotationOffsets { get; private set; }
			public Transform[] grabbedObjects;
			IUsesSnapping m_UsesSnapping;
			Vector3[] m_InitialScales;
			GameObject[] m_Objects;

			public GrabData(Transform rayOrigin, DirectSelectInput input, Transform[] grabbedObjects, IUsesSnapping usesSnapping)
			{
				this.rayOrigin = rayOrigin;
				this.input = input;
				this.grabbedObjects = grabbedObjects;
				m_UsesSnapping = usesSnapping;

				m_Objects = new GameObject[grabbedObjects.Length];
				for (int i = 0; i < grabbedObjects.Length; i++)
				{
					var go = grabbedObjects[i].gameObject;
					m_Objects[i] = go;
				}

				Reset();
			}

			public void Reset()
			{
				var length = grabbedObjects.Length;
				positionOffsets = new Vector3[length];
				rotationOffsets = new Quaternion[length];
				m_InitialScales = new Vector3[length];
				for (int i = 0; i < length; i++)
				{
					var grabbedObject = grabbedObjects[i];
					MathUtilsExt.GetTransformOffset(rayOrigin, grabbedObject, out positionOffsets[i], out rotationOffsets[i]);
					m_InitialScales[i] = grabbedObject.transform.localScale;
				}
			}

			public void UpdatePositions()
			{
				Undo.RecordObjects(grabbedObjects, "Move");

				for (int i = 0; i < grabbedObjects.Length; i++)
				{
					var grabbedObject = grabbedObjects[i];
					var position = grabbedObject.position;
					var rotation = grabbedObject.rotation;
					var targetPosition = rayOrigin.position + rayOrigin.rotation * positionOffsets[i];
					var targetRotation = rayOrigin.rotation * rotationOffsets[i];

					if (m_UsesSnapping.DirectTransformWithSnapping(rayOrigin, m_Objects, ref position, ref rotation, targetPosition, targetRotation))
					{
						var deltaTime = Time.unscaledDeltaTime;
						grabbedObject.position = Vector3.Lerp(grabbedObject.position, position, k_DirectLazyFollowTranslate * deltaTime);
						grabbedObject.rotation = Quaternion.Lerp(grabbedObject.rotation, rotation, k_DirectLazyFollowRotate * deltaTime);
					}
					else
					{
						grabbedObject.position = targetPosition;
						grabbedObject.rotation = targetRotation;
					}
				}
			}

			public void ScaleObjects(float scaleFactor)
			{
				Undo.RecordObjects(grabbedObjects, "Move");

				for (int i = 0; i < grabbedObjects.Length; i++)
				{
					var grabbedObject = grabbedObjects[i];
					grabbedObject.position = rayOrigin.position + positionOffsets[i] * scaleFactor;
					grabbedObject.localScale = m_InitialScales[i] * scaleFactor;
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

		public event Action<GameObject> objectGrabbed;
		public event Action<Transform[], Transform> objectsDropped;

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

				var hoveringSelection = false;
				foreach (var selection in directSelection.Values)
				{
					if (Selection.gameObjects.Contains(selection.gameObject))
					{
						hoveringSelection = true;
						break;
					}
				}

				// Disable manipulator on direct hover or drag
				if (manipulatorGameObject.activeSelf && (hoveringSelection || hasObject))
					manipulatorGameObject.SetActive(false);

				foreach (var kvp in directSelection)
				{
					var rayOrigin = kvp.Key;
					var selection = kvp.Value;
					var hoveredObject = selection.gameObject;

					var selectionCandidate = this.GetSelectionCandidate(hoveredObject, true);

					// Can't select this object (it might be locked or static)
					if (hoveredObject && !selectionCandidate)
						return;

					if (selectionCandidate)
						hoveredObject = selectionCandidate;

					if (!this.CanGrabObject(hoveredObject, rayOrigin))
						continue;

					var directSelectInput = (DirectSelectInput)selection.input;
					if (directSelectInput.select.wasJustPressed)
					{
						this.ClearSnappingState(rayOrigin);

						if (objectGrabbed != null)
							objectGrabbed(hoveredObject);

						// Only add to selection, don't remove
						if (!Selection.objects.Contains(hoveredObject))
							this.SelectObject(hoveredObject, rayOrigin, directSelectInput.multiSelect.isHeld);

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

						m_GrabData[selectedNode] = new GrabData(rayOrigin, directSelectInput, Selection.transforms, this);

						this.HideDefaultRay(rayOrigin, true); // This will also unhighlight the object
						this.LockRay(rayOrigin, this);

						// Wait a frame since OnSelectionChanged is called at the end of the frame, and will set m_DirectSelected to false
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

				var leftInput = leftData != null ? leftData.input : null;
				var leftHeld = leftData != null && leftInput.select.isHeld;
				var rightInput = rightData != null ? rightData.input : null;
				var rightHeld = rightData != null && rightInput.select.isHeld;
				if (hasLeft && hasRight && leftHeld && rightHeld) // Two-handed scaling
				{
					// Offsets will change while scaling. Whichever hand keeps holding the trigger after scaling is done will need to reset itself
					m_WasScaling = true;

					var rightRayOrigin = rightData.rayOrigin;
					var leftRayOrigin = leftData.rayOrigin;
					m_ScaleFactor = (leftRayOrigin.position - rightRayOrigin.position).magnitude / m_ScaleStartDistance;
					if (m_ScaleFactor > 0 && m_ScaleFactor < Mathf.Infinity)
					{
						if (m_ScaleFirstNode == Node.LeftHand)
						{
							leftData.ScaleObjects(m_ScaleFactor);
							this.ClearSnappingState(leftRayOrigin);
						}
						else
						{
							rightData.ScaleObjects(m_ScaleFactor);
							this.ClearSnappingState(rightRayOrigin);
						}
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

				if (hasLeft)
				{
					if (leftInput.cancel.wasJustPressed)
					{
						DropObjects(Node.LeftHand);
						consumeControl(leftInput.cancel);
						Undo.PerformUndo();
					}

					if (leftInput.select.wasJustReleased)
					{
						DropObjects(Node.LeftHand);
						consumeControl(leftInput.select);
					}
				}

				if (hasRight)
				{
					if (rightInput.cancel.wasJustPressed)
					{
						DropObjects(Node.RightHand);
						consumeControl(rightInput.cancel);
						Undo.PerformUndo();
					}

					if (rightInput.select.wasJustReleased)
					{
						DropObjects(Node.RightHand);
						consumeControl(rightInput.select);
					}
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
					this.UnlockRay(rayOrigin, this);
					this.LockRay(destRayOrigin, this);
					return;
				}
			}
		}

		public void GrabObjects(Node node, Transform rayOrigin, ActionMapInput input, Transform[] objects)
		{
			m_GrabData[node] = new GrabData(rayOrigin, (DirectSelectInput)input, objects, this);
		}

		void DropObjects(Node inputNode)
		{
			var grabData = m_GrabData[inputNode];
			var grabbedObjects = grabData.grabbedObjects;
			var rayOrigin = grabData.rayOrigin;

			if (objectsDropped != null)
				objectsDropped(grabbedObjects, rayOrigin);

			m_GrabData.Remove(inputNode);

			this.UnlockRay(grabData.rayOrigin, this);
			this.ShowDefaultRay(grabData.rayOrigin, true);

			this.ClearSnappingState(rayOrigin);
		}

		void Translate(Vector3 delta, Transform rayOrigin, bool constrained)
		{
			if (constrained)
				m_TargetPosition += delta;
			else
				this.TranslateWithSnapping(rayOrigin, Selection.gameObjects, ref m_TargetPosition, ref m_TargetRotation, delta);
		}

		void Rotate(Quaternion delta)
		{
			m_TargetRotation = delta * m_TargetRotation;
		}

		void Scale(Vector3 delta)
		{
			m_TargetScale += delta;
		}

		static void OnDragStarted()
		{
			Undo.IncrementCurrentGroup();
		}

		void OnDragEnded(Transform rayOrigin)
		{
			this.ClearSnappingState(rayOrigin);
		}

		void UpdateSelectionBounds()
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
			manipulator.dragEnded += OnDragEnded;
			return manipulator;
		}

		void UpdateCurrentManipulator()
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

			// Save the initial position, rotation, and scale relative to the manipulator
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

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
		IGrabObjects, ICustomRay, IProcessInput, ISelectObject, IManipulatorVisibility, IUsesSnapping, ISetHighlight,
		ILinkedObject
	{
		const float k_LazyFollowTranslate = 8f;
		const float k_LazyFollowRotate = 12f;
		const float k_DirectLazyFollowTranslate = 20f;
		const float k_DirectLazyFollowRotate = 30f;

		class GrabData
		{
			Vector3[] m_PositionOffsets;
			Quaternion[] m_RotationOffsets;
			Vector3[] m_InitialScales;

			public Transform[] grabbedObjects { get; private set; }
			public DirectSelectInput input { get; private set; }
			public Transform rayOrigin { get; private set; }

			public bool suspended { private get; set; }

			public GrabData(Transform rayOrigin, DirectSelectInput input, Transform[] grabbedObjects)
			{
				this.rayOrigin = rayOrigin;
				this.input = input;
				this.grabbedObjects = grabbedObjects;
				Reset();
			}

			public void Reset()
			{
				if (suspended)
					return;

				var length = grabbedObjects.Length;
				m_PositionOffsets = new Vector3[length];
				m_RotationOffsets = new Quaternion[length];
				m_InitialScales = new Vector3[length];
				for (int i = 0; i < length; i++)
				{
					var grabbedObject = grabbedObjects[i];
					MathUtilsExt.GetTransformOffset(rayOrigin, grabbedObject, out m_PositionOffsets[i], out m_RotationOffsets[i]);
					m_InitialScales[i] = grabbedObject.transform.localScale;
				}
			}

			public void UpdatePositions(IUsesSnapping usesSnapping)
			{
				if (suspended)
					return;

				Undo.RecordObjects(grabbedObjects, "Move");

				for (int i = 0; i < grabbedObjects.Length; i++)
				{
					var grabbedObject = grabbedObjects[i];
					var position = grabbedObject.position;
					var rotation = grabbedObject.rotation;
					var targetPosition = rayOrigin.position + rayOrigin.rotation * m_PositionOffsets[i];
					var targetRotation = rayOrigin.rotation * m_RotationOffsets[i];

					if (usesSnapping.DirectSnap(rayOrigin, grabbedObject, ref position, ref rotation, targetPosition, targetRotation))
					{
						var deltaTime = Time.deltaTime;
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
				if (suspended)
					return;

				Undo.RecordObjects(grabbedObjects, "Move");

				for (int i = 0; i < grabbedObjects.Length; i++)
				{
					var grabbedObject = grabbedObjects[i];
					grabbedObject.position = rayOrigin.position + m_PositionOffsets[i] * scaleFactor;
					grabbedObject.localScale = m_InitialScales[i] * scaleFactor;
				}
			}

			public void TransferTo(Transform destRayOrigin, Vector3 deltaOffset)
			{
				rayOrigin = destRayOrigin;
				for (int i = 0; i < m_PositionOffsets.Length; i++)
				{
					m_PositionOffsets[i] += deltaOffset;
				}
			}

			public void StartScaling()
			{
				for (int i = 0; i < grabbedObjects.Length; i++)
				{
					m_PositionOffsets[i] = grabbedObjects[i].position - rayOrigin.position;
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
				if (!this.IsSharedUpdater(this))
					return null;

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
		bool m_Scaling;

		readonly TransformAction m_PivotModeToggleAction = new TransformAction();
		readonly TransformAction m_PivotRotationToggleAction = new TransformAction();
		readonly TransformAction m_ManipulatorToggleAction = new TransformAction();

		public event Action<Transform, HashSet<Transform>> objectsGrabbed;
		public event Action<Transform, Transform[]> objectsDropped;
		public event Action<Transform, Transform> objectsTransferred;

		public bool manipulatorVisible { private get; set; }

		public List<ILinkedObject> linkedObjects { private get; set; }

		void Start()
		{
			if (!this.IsSharedUpdater(this))
				return;

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
			if (!this.IsSharedUpdater(this))
				return;

			// Reset direct selection state in case of a ray selection
			m_DirectSelected = false;

			if (Selection.gameObjects.Length == 0)
				m_CurrentManipulator.gameObject.SetActive(false);
			else
				UpdateCurrentManipulator();
		}

		public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
			if (!this.IsSharedUpdater(this))
				return;

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
					var directRayOrigin = kvp.Key;
					var directSelectionData = kvp.Value;
					var directHoveredObject = directSelectionData.gameObject;

					var selectionCandidate = this.GetSelectionCandidate(directHoveredObject, true);

					// Can't select this object (it might be locked or static)
					if (directHoveredObject && !selectionCandidate)
						continue;

					if (selectionCandidate)
						directHoveredObject = selectionCandidate;

					if (!this.CanGrabObject(directHoveredObject, directRayOrigin))
						continue;

					var directSelectInput = (DirectSelectInput)directSelectionData.input;
					if (directSelectInput.select.wasJustPressed)
					{
						this.ClearSnappingState(directRayOrigin);

						consumeControl(directSelectInput.select);

						var grabbingNode = directSelectionData.node;

						// Check if the other hand is already grabbing for two-handed scale
						foreach (var grabData in m_GrabData)
						{
							var otherNode = grabData.Key;
							var otherData = grabData.Value;

							if (otherNode != grabbingNode && otherData.grabbedObjects.Contains(directHoveredObject.transform))
							{
								m_ScaleStartDistance = (directRayOrigin.position - otherData.rayOrigin.position).magnitude;
								m_ScaleFirstNode = otherNode;
								otherData.StartScaling();
								m_Scaling = true;
								break;
							}
						}

						var grabbedObjects = new HashSet<Transform> { directHoveredObject.transform };
						grabbedObjects.UnionWith(Selection.transforms);

						if (objectsGrabbed != null && !m_Scaling)
							objectsGrabbed(directRayOrigin, grabbedObjects);

						m_GrabData[grabbingNode] = new GrabData(directRayOrigin, directSelectInput, grabbedObjects.ToArray());

						this.HideDefaultRay(directRayOrigin, true); // This will also unhighlight the object
						this.LockRay(directRayOrigin, this);

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

				if (hasLeft)
				{
					if (leftInput.cancel.wasJustPressed)
					{
						DropHeldObjects(Node.LeftHand);
						hasLeft = false;
						consumeControl(leftInput.cancel);
						Undo.PerformUndo();
					}

					if (leftInput.select.wasJustReleased)
					{
						DropHeldObjects(Node.LeftHand);
						hasLeft = false;
						consumeControl(leftInput.select);
					}
				}

				if (hasRight)
				{
					if (rightInput.cancel.wasJustPressed)
					{
						DropHeldObjects(Node.RightHand);
						hasRight = false;
						consumeControl(rightInput.cancel);
						Undo.PerformUndo();
					}

					if (rightInput.select.wasJustReleased)
					{
						DropHeldObjects(Node.RightHand);
						hasRight = false;
						consumeControl(rightInput.select);
					}
				}

				if (hasLeft && hasRight && leftHeld && rightHeld && m_Scaling) // Two-handed scaling
				{
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
					// Offsets will change while scaling. Whichever hand keeps holding the trigger after scaling is done will need to reset itself
					if (m_Scaling)
					{
						if (hasLeft)
						{
							leftData.Reset();

							if (objectsTransferred != null && m_ScaleFirstNode == Node.RightHand)
								objectsTransferred(rightData.rayOrigin, leftData.rayOrigin);
						}
						if (hasRight)
						{
							rightData.Reset();

							if (objectsTransferred != null && m_ScaleFirstNode == Node.LeftHand)
								objectsTransferred(leftData.rayOrigin, rightData.rayOrigin);
						}

						m_Scaling = false;
					}

					if (hasLeft && leftHeld)
						leftData.UpdatePositions(this);

					if (hasRight && rightHeld)
						rightData.UpdatePositions(this);
				}
			}

			// Manipulator is disabled while direct manipulation is happening
			if (hasObject || m_DirectSelected)
				return;

			if (Selection.gameObjects.Length > 0)
			{
				if (!m_CurrentManipulator.dragging)
					UpdateCurrentManipulator();

				var deltaTime = Time.deltaTime;
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

		public void Suspend(Node node)
		{
			GrabData grabData;
			if (m_GrabData.TryGetValue(node, out grabData))
				grabData.suspended = true;
		}

		public void Resume(Node node)
		{
			GrabData grabData;
			if (m_GrabData.TryGetValue(node, out grabData))
				grabData.suspended = false;
		}

		public Transform[] GetHeldObjects(Node node)
		{
			GrabData grabData;
			return m_GrabData.TryGetValue(node, out grabData) ? grabData.grabbedObjects : null;
		}

		public void TransferHeldObjects(Transform rayOrigin, Transform destRayOrigin, Vector3 deltaOffset = default(Vector3))
		{
			if (!this.IsSharedUpdater(this))
				return;

			foreach (var grabData in m_GrabData.Values)
			{
				if (grabData.rayOrigin == rayOrigin)
				{
					grabData.TransferTo(destRayOrigin, deltaOffset);
					this.ClearSnappingState(rayOrigin);
					grabData.UpdatePositions(this);

					// Prevent lock from getting stuck
					this.UnlockRay(rayOrigin, this);
					this.LockRay(destRayOrigin, this);

					if (objectsTransferred != null)
						objectsTransferred(rayOrigin, destRayOrigin);

					return;
				}
			}
		}

		public void DropHeldObjects(Node node)
		{
			if (!this.IsSharedUpdater(this))
				return;

			var grabData = m_GrabData[node];
			var grabbedObjects = grabData.grabbedObjects;
			var rayOrigin = grabData.rayOrigin;

			if (objectsDropped != null && !m_Scaling)
				objectsDropped(rayOrigin, grabbedObjects);

			m_GrabData.Remove(node);

			this.UnlockRay(grabData.rayOrigin, this);
			this.ShowDefaultRay(grabData.rayOrigin, true);

			this.ClearSnappingState(rayOrigin);
		}

		void Translate(Vector3 delta, Transform rayOrigin, bool constrained)
		{
			if (constrained)
				m_TargetPosition += delta;
			else
				this.ManipulatorSnap(rayOrigin, Selection.transforms, ref m_TargetPosition, ref m_TargetRotation, delta);
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
			m_SelectionBounds = ObjectUtils.GetBounds(Selection.transforms);
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

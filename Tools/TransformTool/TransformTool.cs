#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Manipulators;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Tools
{
    sealed class TransformTool : MonoBehaviour, ITool, ITransformer, ISelectionChanged, IActions, IUsesDirectSelection,
        IGrabObjects, ISelectObject, IManipulatorController, IUsesSnapping, ISetHighlight, ILinkedObject, IRayToNode,
        IControlHaptics, IUsesRayOrigin, IUsesNode, ICustomActionMap, ITwoHandedScaler, IIsMainMenuVisible,
        IGetRayVisibility, IRayVisibilitySettings, IRequestFeedback
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
            public TransformInput input { get; private set; }
            public Transform rayOrigin { get; private set; }

            public bool suspended { private get; set; }

            public GrabData(Transform rayOrigin, TransformInput input, Transform[] grabbedObjects)
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

            public void UpdatePositions(IUsesSnapping usesSnapping, bool interpolate = true)
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
                        if (interpolate)
                        {
                            var deltaTime = Time.deltaTime;
                            grabbedObject.position = Vector3.Lerp(grabbedObject.position, position, k_DirectLazyFollowTranslate * deltaTime);
                            grabbedObject.rotation = Quaternion.Lerp(grabbedObject.rotation, rotation, k_DirectLazyFollowRotate * deltaTime);
                        }
                        else
                        {
                            grabbedObject.position = position;
                            grabbedObject.rotation = rotation;
                        }
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

                    var targetPosition = rayOrigin.position + m_PositionOffsets[i] * scaleFactor;
                    if (grabbedObject.position != targetPosition)
                        grabbedObject.position = targetPosition;

                    var targetScale = m_InitialScales[i] * scaleFactor;
                    if (grabbedObject.localScale != targetScale)
                        grabbedObject.localScale = targetScale;
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
                    m_Actions = new List<IAction>
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

        [SerializeField]
        ActionMap m_ActionMap;

        [SerializeField]
        HapticPulse m_DragPulse;

        [SerializeField]
        HapticPulse m_RotatePulse;

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

        GrabData m_LeftGrabData, m_RightGrabData;
        bool m_DirectSelected;
        float m_ScaleStartDistance;
        Node m_ScaleFirstNode;
        float m_ScaleFactor;
        bool m_Scaling;
        bool m_CurrentlySnapping;

        TransformInput m_Input;

        readonly BindingDictionary m_Controls = new BindingDictionary();
        readonly List<ProxyFeedbackRequest> m_GrabFeedback = new List<ProxyFeedbackRequest>();
        readonly List<ProxyFeedbackRequest> m_ScaleFeedback = new List<ProxyFeedbackRequest>();

        readonly TransformAction m_PivotModeToggleAction = new TransformAction();
        readonly TransformAction m_PivotRotationToggleAction = new TransformAction();
        readonly TransformAction m_ManipulatorToggleAction = new TransformAction();

        public event Action<Transform, HashSet<Transform>> objectsGrabbed;
        public event Action<Transform, Transform[]> objectsDropped;
        public event Action<Transform, Transform> objectsTransferred;

        public bool manipulatorVisible { private get; set; }

        public bool manipulatorDragging
        {
            get
            {
                return
                    m_StandardManipulator && m_StandardManipulator.dragging
                    || m_ScaleManipulator && m_ScaleManipulator.dragging;
            }
        }

        public List<ILinkedObject> linkedObjects { private get; set; }

        public Transform rayOrigin { private get; set; }
        public Node node { private get; set; }

        public ActionMap actionMap { get { return m_ActionMap; } }
        public bool ignoreLocking { get { return false; } }

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

            InputUtils.GetBindingDictionaryFromActionMap(m_ActionMap, m_Controls);
        }

        public void OnSelectionChanged()
        {
            if (!this.IsSharedUpdater(this))
                return;

            if (Selection.gameObjects.Length == 0)
                m_CurrentManipulator.gameObject.SetActive(false);
            else
                UpdateCurrentManipulator();
        }

        public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
        {
            m_Input = (TransformInput)input;

            if (!this.IsSharedUpdater(this))
                return;

            var hasObject = false;
            var manipulatorGameObject = m_CurrentManipulator.gameObject;
            var gameObjects = Selection.gameObjects;
            if (!m_CurrentManipulator.dragging)
            {
                var directSelection = this.GetDirectSelection();

                var hasLeft = m_LeftGrabData != null;
                var hasRight = m_RightGrabData != null;
                hasObject = directSelection.Count > 0 || hasLeft || hasRight;

                var hoveringSelection = false;
                foreach (var kvp in directSelection)
                {
                    if (gameObjects.Contains(kvp.Value.gameObject))
                    {
                        hoveringSelection = true;
                        break;
                    }
                }

                // Disable manipulator on direct hover or drag
                if (manipulatorGameObject.activeSelf && (hoveringSelection || hasLeft || hasRight))
                    manipulatorGameObject.SetActive(false);

                var scaleHover = false;
                foreach (var kvp in directSelection)
                {
                    var directRayOrigin = kvp.Key;

                    if (!(hasLeft || hasRight) && this.IsMainMenuVisible(directRayOrigin))
                        continue;

                    var directHoveredObject = kvp.Value;

                    var selectionCandidate = this.GetSelectionCandidate(directHoveredObject, true);

                    // Can't select this object (it might be locked or static)
                    if (directHoveredObject && !selectionCandidate)
                        continue;

                    if (selectionCandidate)
                        directHoveredObject = selectionCandidate;

                    if (!this.CanGrabObject(directHoveredObject, directRayOrigin))
                        continue;

                    this.AddRayVisibilitySettings(directRayOrigin, this, false, true); // This will also disable ray selection

                    if (!this.IsConeVisible(directRayOrigin))
                        continue;

                    var grabbingNode = this.RequestNodeFromRayOrigin(directRayOrigin);
                    var transformTool = linkedObjects.Cast<TransformTool>().FirstOrDefault(linkedObject => linkedObject.node == grabbingNode);
                    if (transformTool == null)
                        continue;

                    // Check if the other hand is already grabbing for two-handed scale
                    var otherData = grabbingNode == Node.LeftHand ? m_RightGrabData : m_LeftGrabData;

                    if (otherData != null && !otherData.grabbedObjects.Contains(directHoveredObject.transform))
                        otherData = null;

                    if (otherData != null)
                    {
                        scaleHover = true;
                        if (m_ScaleFeedback.Count == 0)
                            ShowScaleFeedback(grabbingNode);
                    }

                    var transformInput = transformTool.m_Input;

                    if (transformInput.select.wasJustPressed)
                    {
                        this.ClearSnappingState(directRayOrigin);

                        consumeControl(transformInput.select);

                        if (otherData != null)
                        {
                            m_ScaleStartDistance = (directRayOrigin.position - otherData.rayOrigin.position).magnitude;
                            m_ScaleFirstNode = grabbingNode == Node.LeftHand ? Node.RightHand : Node.LeftHand;
                            otherData.StartScaling();
                            m_Scaling = true;
                        }

                        var grabbedObjects = new HashSet<Transform> { directHoveredObject.transform };
                        grabbedObjects.UnionWith(Selection.transforms);

                        if (objectsGrabbed != null && !m_Scaling)
                            objectsGrabbed(directRayOrigin, grabbedObjects);

                        var grabData = new GrabData(directRayOrigin, transformInput, grabbedObjects.ToArray());
                        if (grabbingNode == Node.LeftHand)
                            m_LeftGrabData = grabData;
                        else
                            m_RightGrabData = grabData;
                        ShowGrabFeedback(grabbingNode);

                        // A direct selection has been made. Hide the manipulator until the selection changes
                        m_DirectSelected = true;

                        Undo.IncrementCurrentGroup();
                    }
                }

                if (!scaleHover)
                    HideScaleFeedback();

                hasLeft = m_LeftGrabData != null;
                hasRight = m_RightGrabData != null;

                var leftInput = m_LeftGrabData != null ? m_LeftGrabData.input : null;
                var leftHeld = m_LeftGrabData != null && leftInput.select.isHeld;
                var rightInput = m_RightGrabData != null ? m_RightGrabData.input : null;
                var rightHeld = m_RightGrabData != null && rightInput.select.isHeld;

                if (hasLeft)
                {
                    consumeControl(leftInput.cancel);
                    if (leftInput.cancel.wasJustPressed)
                    {
                        if (m_Scaling)
                        {
                            m_Scaling = false;
                            DropHeldObjects(Node.LeftHand);
                            DropHeldObjects(Node.RightHand);
                            hasRight = false;
                        }
                        else
                        {
                            DropHeldObjects(Node.LeftHand);
                        }
                        hasLeft = false;
                        Undo.PerformUndo();
                    }

                    if (leftInput.select.wasJustReleased)
                    {
                        if (rightInput != null && rightInput.select.wasJustReleased)
                            m_Scaling = false;

                        DropHeldObjects(Node.LeftHand);
                        hasLeft = false;
                        consumeControl(leftInput.select);
                    }
                }

                if (hasRight)
                {
                    consumeControl(rightInput.cancel);
                    if (rightInput.cancel.wasJustPressed)
                    {
                        if (m_Scaling)
                        {
                            m_Scaling = false;
                            DropHeldObjects(Node.RightHand);
                            DropHeldObjects(Node.LeftHand);
                            hasLeft = false;
                        }
                        else
                        {
                             DropHeldObjects(Node.RightHand);
                        }
                        hasRight = false;
                        Undo.PerformUndo();
                    }

                    if (rightInput.select.wasJustReleased)
                    {
                        if (leftInput != null && leftInput.select.wasJustReleased)
                            m_Scaling = false;

                        DropHeldObjects(Node.RightHand);
                        hasRight = false;
                        consumeControl(rightInput.select);
                    }
                }

                if (hasLeft && hasRight && leftHeld && rightHeld && m_Scaling) // Two-handed scaling
                {
                    var rightRayOrigin = m_RightGrabData.rayOrigin;
                    var leftRayOrigin = m_LeftGrabData.rayOrigin;
                    m_ScaleFactor = (leftRayOrigin.position - rightRayOrigin.position).magnitude / m_ScaleStartDistance;
                    if (m_ScaleFactor > 0 && m_ScaleFactor < Mathf.Infinity)
                    {
                        if (m_ScaleFirstNode == Node.LeftHand)
                        {
                            m_LeftGrabData.ScaleObjects(m_ScaleFactor);
                            this.ClearSnappingState(leftRayOrigin);
                        }
                        else
                        {
                            m_RightGrabData.ScaleObjects(m_ScaleFactor);
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
                            m_LeftGrabData.Reset();

                            if (objectsTransferred != null && m_ScaleFirstNode == Node.RightHand)
                                objectsTransferred(m_RightGrabData.rayOrigin, m_LeftGrabData.rayOrigin);
                        }
                        if (hasRight)
                        {
                            m_RightGrabData.Reset();

                            if (objectsTransferred != null && m_ScaleFirstNode == Node.LeftHand)
                                objectsTransferred(m_LeftGrabData.rayOrigin, m_RightGrabData.rayOrigin);
                        }

                        m_Scaling = false;
                    }

                    if (hasLeft && leftHeld)
                        m_LeftGrabData.UpdatePositions(this);

                    if (hasRight && rightHeld)
                        m_RightGrabData.UpdatePositions(this);
                }

                foreach (var linkedObject in linkedObjects)
                {
                    var transformTool = (TransformTool)linkedObject;
                    var rayOrigin = transformTool.rayOrigin;
                    if (!(m_Scaling || directSelection.ContainsKey(rayOrigin) || GrabDataForNode(transformTool.node) != null))
                    {
                        this.RemoveRayVisibilitySettings(rayOrigin, this);
                    }
                }
            }

            // Manipulator is disabled while direct manipulation is happening
            if (hasObject || m_DirectSelected)
                return;

            if (gameObjects.Length > 0)
            {
                if (!m_CurrentManipulator.dragging)
                    UpdateCurrentManipulator();

                var deltaTime = Time.deltaTime;
                var manipulatorTransform = manipulatorGameObject.transform;
                var lerp = m_CurrentlySnapping ? 1f : k_LazyFollowTranslate * deltaTime;
                manipulatorTransform.position = Vector3.Lerp(manipulatorTransform.position, m_TargetPosition, lerp);

                // Manipulator does not rotate when in global mode
                if (m_PivotRotation == PivotRotation.Local && m_CurrentManipulator == m_StandardManipulator)
                    manipulatorTransform.rotation = Quaternion.Slerp(manipulatorTransform.rotation, m_TargetRotation, k_LazyFollowRotate * deltaTime);

                var selectionTransforms = Selection.transforms;
                Undo.RecordObjects(selectionTransforms, "Move");

                foreach (var t in selectionTransforms)
                {
                    var targetRotation = Quaternion.Slerp(t.rotation, m_TargetRotation * m_RotationOffsets[t], k_LazyFollowRotate * deltaTime);
                    if (t.rotation != targetRotation)
                        t.rotation = targetRotation;

                    Vector3 targetPosition;
                    if (m_PivotMode == PivotMode.Center) // Rotate the position offset from the manipulator when rotating around center
                    {
                        m_PositionOffsetRotation = Quaternion.Slerp(m_PositionOffsetRotation, m_TargetRotation * Quaternion.Inverse(m_StartRotation), k_LazyFollowRotate * deltaTime);
                        targetPosition = manipulatorTransform.position + m_PositionOffsetRotation * m_PositionOffsets[t];
                    }
                    else
                    {
                        targetPosition = manipulatorTransform.position + m_PositionOffsets[t];
                    }

                    if (t.position != targetPosition)
                        t.position = targetPosition;

                    var targetScale = Vector3.Lerp(t.localScale, Vector3.Scale(m_TargetScale, m_ScaleOffsets[t]), k_LazyFollowTranslate * deltaTime);
                    if (t.localScale != targetScale)
                        t.localScale = targetScale;
                }
            }
        }

        public void Suspend(Node node)
        {
            var grabData = GrabDataForNode(node);
            if (grabData != null)
                grabData.suspended = true;
        }

        public void Resume(Node node)
        {
            var grabData = GrabDataForNode(node);
            if (grabData != null)
            {
                grabData.suspended = false;
                grabData.UpdatePositions(this, false);
            }
        }

        public Transform[] GetHeldObjects(Node node)
        {
            var grabData = GrabDataForNode(node);
            return grabData == null ? null : grabData.grabbedObjects;
        }

        public void TransferHeldObjects(Transform rayOrigin, Transform destRayOrigin, Vector3 deltaOffset = default(Vector3))
        {
            if (!this.IsSharedUpdater(this))
                return;

            var grabData = GrabDataForRayOrigin(rayOrigin);

            if (grabData == null)
                return;

            grabData.TransferTo(destRayOrigin, deltaOffset);
            this.ClearSnappingState(rayOrigin);
            grabData.UpdatePositions(this, false);

            // Prevent lock from getting stuck
            this.RemoveRayVisibilitySettings(rayOrigin, this);
            this.AddRayVisibilitySettings(destRayOrigin, this, false, true);

            if (objectsTransferred != null)
                objectsTransferred(rayOrigin, destRayOrigin);
        }

        public void DropHeldObjects(Node node)
        {
            if (!this.IsSharedUpdater(this))
                return;

            var grabData = GrabDataForNode(node);
            var grabbedObjects = grabData.grabbedObjects;
            var rayOrigin = grabData.rayOrigin;

            if (objectsDropped != null)
                objectsDropped(rayOrigin, grabbedObjects);

            if (node == Node.LeftHand)
                m_LeftGrabData = null;
            else
                m_RightGrabData = null;

            m_ScaleFirstNode = Node.None;

            HideGrabFeedback();

            this.RemoveRayVisibilitySettings(grabData.rayOrigin, this);

            this.ClearSnappingState(rayOrigin);
        }

        void Translate(Vector3 delta, Transform rayOrigin, AxisFlags constraints)
        {
            switch (constraints)
            {
                case AxisFlags.X | AxisFlags.Y:
                case AxisFlags.Y | AxisFlags.Z:
                case AxisFlags.X | AxisFlags.Z:
                    m_TargetPosition += delta;
                    break;
                default:
                    m_CurrentlySnapping = this.ManipulatorSnap(rayOrigin, Selection.transforms, ref m_TargetPosition, ref m_TargetRotation, delta, constraints, m_PivotMode);

                    if (constraints == 0)
                        m_CurrentlySnapping = false;
                    break;
            }

            this.Pulse(this.RequestNodeFromRayOrigin(rayOrigin), m_DragPulse);
        }

        void Rotate(Quaternion delta, Transform rayOrigin)
        {
            m_TargetRotation = delta * m_TargetRotation;

            this.Pulse(this.RequestNodeFromRayOrigin(rayOrigin), m_RotatePulse);
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

            m_SelectionBounds = ObjectUtils.GetBounds(selectionTransforms);

            var manipulatorTransform = manipulatorGameObject.transform;
            var activeTransform = Selection.activeTransform ?? selectionTransforms[0];
            manipulatorTransform.position = m_PivotMode == PivotMode.Pivot ? activeTransform.position : m_SelectionBounds.center;
            manipulatorTransform.rotation = m_PivotRotation == PivotRotation.Global && m_CurrentManipulator == m_StandardManipulator
                ? Quaternion.identity : activeTransform.rotation;
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

        public void OnResetDirectSelectionState()
        {
            m_DirectSelected = false;
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

        public bool IsTwoHandedScaling(Transform rayOrigin)
        {
            return m_Scaling && GrabDataForRayOrigin(rayOrigin) != null;
        }

        GrabData GrabDataForNode(Node node)
        {
            return node == Node.LeftHand ? m_LeftGrabData : m_RightGrabData;
        }

        GrabData GrabDataForRayOrigin(Transform rayOrigin)
        {
            if (m_LeftGrabData != null && m_LeftGrabData.rayOrigin == rayOrigin)
                return m_LeftGrabData;

            if (m_RightGrabData != null && m_RightGrabData.rayOrigin == rayOrigin)
                return m_RightGrabData;

            return null;
        }

        void ShowFeedback(List<ProxyFeedbackRequest> requests, string controlName, string tooltipText, Node node, bool suppressExisting = false)
        {
            List<VRInputDevice.VRControl> ids;
            if (m_Controls.TryGetValue(controlName, out ids))
            {
                foreach (var id in ids)
                {
                    var request = (ProxyFeedbackRequest)this.GetFeedbackRequestObject(typeof(ProxyFeedbackRequest));
                    request.node = node;
                    request.control = id;
                    request.tooltipText = tooltipText;
                    request.priority = 1;
                    request.suppressExisting = suppressExisting;
                    requests.Add(request);
                    this.AddFeedbackRequest(request);
                }
            }
        }

        void ShowGrabFeedback(Node node)
        {
            ShowFeedback(m_GrabFeedback, "Cancel", "Cancel", node);
            ShowFeedback(m_GrabFeedback, "Select", null, node, true);
        }

        void ShowScaleFeedback(Node node)
        {
            ShowFeedback(m_ScaleFeedback, "Select", "Scale", node);
        }

        void HideFeedback(List<ProxyFeedbackRequest> requests)
        {
            foreach (var request in requests)
            {
                this.RemoveFeedbackRequest(request);
            }
            requests.Clear();
        }

        void HideGrabFeedback()
        {
            HideFeedback(m_GrabFeedback);
        }

        void HideScaleFeedback()
        {
            HideFeedback(m_ScaleFeedback);
        }
    }
}
#endif

using System;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Menus;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;

#if INCLUDE_TEXT_MESH_PRO
using TMPro;
#endif

[assembly: OptionalDependency("TMPro.TextMeshProUGUI", "INCLUDE_TEXT_MESH_PRO")]

namespace UnityEditor.Experimental.EditorVR.Modules
{
    [MainMenuItem("Snapping", "Settings", "Select snapping modes")]
    sealed class SnappingModule : MonoBehaviour, ISystemModule, IUsesViewerScale, ISettingsMenuProvider, ISerializePreferences,
        IRaycast, IStandardIgnoreList
    {
        const float k_GroundPlaneScale = 1000f;

        const float k_GroundSnappingMaxRayLength = 25f;
        const float k_SurfaceSnappingMaxRayLength = 100f;

        const float k_GroundHeight = 0f;

        const float k_BreakDistance = 0.04f;
        const float k_SnapDistanceScale = 0.75f;
        const float k_BlockedBreakScale = 5f;
        const float k_MaxRayDot = -0.5f;
        const float k_RayExtra = 0.02f;

        const float k_WidgetScale = 0.03f;

        const string k_MaterialColorLeftProperty = "_ColorLeft";
        const string k_MaterialColorRightProperty = "_ColorRight";

#pragma warning disable 649
        [SerializeField]
        GameObject m_GroundPlane;

        [SerializeField]
        GameObject m_Widget;

        [SerializeField]
        GameObject m_SettingsMenuPrefab;

        [SerializeField]
        Material m_ButtonHighlightMaterial;
#pragma warning restore 649

        class SnappingState
        {
            public Vector3 currentPosition { get; set; }
            public bool groundSnapping { get; set; }
            public bool surfaceSnapping { get; set; }

            public bool snapping { get { return surfaceSnapping || groundSnapping; } }

            public Quaternion startRotation { get; private set; }
            public Bounds identityBounds { get; private set; }

            public Transform widget { get; set; }
            public Vector3 snappingPosition { get; set; }
            public Quaternion snappingRotation { get; set; }
            public Vector3 snappingNormal { get; set; }
            public int directionIndex { get; set; }

            public SnappingState(Transform[] transforms, Vector3 position, Quaternion rotation)
            {
                currentPosition = position;
                startRotation = rotation;
                Bounds identityBounds;

                if (transforms.Length == 1)
                {
                    var transform = transforms[0];
                    var objRotation = transform.rotation;

                    transform.rotation = Quaternion.identity;
                    identityBounds = ObjectUtils.GetBounds(transform);
                    transform.rotation = objRotation;
                }
                else
                {
                    float angle;
                    Vector3 axis;
                    rotation.ToAngleAxis(out angle, out axis);
                    foreach (var transform in transforms)
                    {
                        transform.transform.RotateAround(position, axis, -angle);
                    }

                    identityBounds = ObjectUtils.GetBounds(transforms);

                    foreach (var transform in transforms)
                    {
                        transform.transform.RotateAround(position, axis, angle);
                    }
                }
                identityBounds.center -= position;
                this.identityBounds = identityBounds;
            }

            public void OnDestroy()
            {
                if (widget)
                    ObjectUtils.Destroy(widget.gameObject);
            }
        }

        struct SnappingDirection
        {
            public Vector3 direction;
            public Vector3 upVector;
            public Quaternion rotationOffset;
        }

        static readonly SnappingDirection[] k_Directions =
        {
            new SnappingDirection
            {
                direction = Vector3.down,
                upVector = Vector3.back,
                rotationOffset = Quaternion.AngleAxis(90, Vector3.right)
            },
            new SnappingDirection
            {
                direction = Vector3.left,
                upVector = Vector3.up,
                rotationOffset = Quaternion.AngleAxis(90, Vector3.down)
            },
            new SnappingDirection
            {
                direction = Vector3.back,
                upVector = Vector3.up,
                rotationOffset = Quaternion.identity
            },
            new SnappingDirection
            {
                direction = Vector3.right,
                upVector = Vector3.up,
                rotationOffset = Quaternion.AngleAxis(90, Vector3.up)
            },
            new SnappingDirection
            {
                direction = Vector3.forward,
                upVector = Vector3.up,
                rotationOffset = Quaternion.AngleAxis(180, Vector3.up)
            },
            new SnappingDirection
            {
                direction = Vector3.up,
                upVector = Vector3.forward,
                rotationOffset = Quaternion.AngleAxis(90, Vector3.left)
            }
        };

        [Serializable]
        class Preferences
        {
            [SerializeField]
            bool m_DisableAll;

            // Snapping Modes
            [SerializeField]
            bool m_GroundSnappingEnabled = true;
            [SerializeField]
            bool m_SurfaceSnappingEnabled = true;

            // Modifiers (do not require reset on value change)
            [SerializeField]
            bool m_PivotSnappingEnabled;
            [SerializeField]
            bool m_RotationSnappingEnabled;
            [SerializeField]
            bool m_LimitRadius = true;

            // Sources
            [SerializeField]
            bool m_ManipulatorSnappingEnabled = true;
            [SerializeField]
            bool m_DirectSnappingEnabled = true;

            public bool disableAll
            {
                get { return m_DisableAll; }
                set { m_DisableAll = value; }
            }

            public bool groundSnappingEnabled
            {
                get { return m_GroundSnappingEnabled; }
                set { m_GroundSnappingEnabled = value; }
            }

            public bool surfaceSnappingEnabled
            {
                get { return m_SurfaceSnappingEnabled; }
                set { m_SurfaceSnappingEnabled = value; }
            }

            public bool pivotSnappingEnabled
            {
                get { return m_PivotSnappingEnabled; }
                set { m_PivotSnappingEnabled = value; }
            }

            public bool rotationSnappingEnabled
            {
                get { return m_RotationSnappingEnabled; }
                set { m_RotationSnappingEnabled = value; }
            }

            public bool limitRadius
            {
                get { return m_LimitRadius; }
                set { m_LimitRadius = value; }
            }

            public bool manipulatorSnappingEnabled
            {
                get { return m_ManipulatorSnappingEnabled; }
                set { m_ManipulatorSnappingEnabled = value; }
            }

            public bool directSnappingEnabled
            {
                get { return m_DirectSnappingEnabled; }
                set { m_DirectSnappingEnabled = value; }
            }
        }

        Preferences m_Preferences = new Preferences();

        SnappingModuleSettingsUI m_SnappingModuleSettingsUI;
        Material m_ButtonHighlightMaterialClone;

        readonly Dictionary<Transform, Dictionary<Transform, SnappingState>> m_SnappingStates = new Dictionary<Transform, Dictionary<Transform, SnappingState>>();

        public bool widgetEnabled { get; set; }

        public List<GameObject> ignoreList { private get; set; }

        public GameObject settingsMenuPrefab { get { return m_SettingsMenuPrefab; } }

        public GameObject settingsMenuInstance
        {
            set
            {
                if (value == null)
                {
                    m_SnappingModuleSettingsUI = null;
                    return;
                }

                m_SnappingModuleSettingsUI = value.GetComponent<SnappingModuleSettingsUI>();
                SetupUI();
            }
        }

        public bool snappingEnabled
        {
            get { return !m_Preferences.disableAll && (groundSnappingEnabled || surfaceSnappingEnabled); }
            set
            {
                Reset();
                m_Preferences.disableAll = !value;

                if (m_SnappingModuleSettingsUI)
                    m_SnappingModuleSettingsUI.snappingEnabled.isOn = value;
            }
        }

        public bool groundSnappingEnabled
        {
            get { return m_Preferences.groundSnappingEnabled; }
            set
            {
                if (value == m_Preferences.groundSnappingEnabled)
                    return;

                Reset();
                m_Preferences.groundSnappingEnabled = value;

                if (m_SnappingModuleSettingsUI)
                    m_SnappingModuleSettingsUI.groundSnappingEnabled.isOn = value;
            }
        }

        public bool surfaceSnappingEnabled
        {
            get { return m_Preferences.surfaceSnappingEnabled; }
            set
            {
                if (value == m_Preferences.surfaceSnappingEnabled)
                    return;

                Reset();
                m_Preferences.surfaceSnappingEnabled = value;

                if (m_SnappingModuleSettingsUI)
                    m_SnappingModuleSettingsUI.surfaceSnappingEnabled.isOn = value;
            }
        }

        public bool pivotSnappingEnabled
        {
            get { return m_Preferences.pivotSnappingEnabled; }
            set
            {
                m_Preferences.pivotSnappingEnabled = value;

                if (m_SnappingModuleSettingsUI)
                    m_SnappingModuleSettingsUI.pivotSnappingEnabled.isOn = value;
            }
        }

        public bool rotationSnappingEnabled
        {
            get { return m_Preferences.rotationSnappingEnabled; }
            set
            {
                m_Preferences.rotationSnappingEnabled = value;

                if (m_SnappingModuleSettingsUI)
                    m_SnappingModuleSettingsUI.rotationSnappingEnabled.isOn = value;
            }
        }

        public bool limitRadius
        {
            get { return m_Preferences.limitRadius; }
            set
            {
                m_Preferences.limitRadius = value;

                if (m_SnappingModuleSettingsUI)
                    m_SnappingModuleSettingsUI.limitRadius.isOn = value;
            }
        }

        public bool manipulatorSnappingEnabled
        {
            get { return m_Preferences.manipulatorSnappingEnabled; }
            set
            {
                m_Preferences.manipulatorSnappingEnabled = value;

                if (m_SnappingModuleSettingsUI)
                    m_SnappingModuleSettingsUI.manipulatorSnappingEnabled.isOn = value;
            }
        }

        public bool directSnappingEnabled
        {
            get { return m_Preferences.directSnappingEnabled; }
            set
            {
                m_Preferences.directSnappingEnabled = value;

                if (m_SnappingModuleSettingsUI)
                    m_SnappingModuleSettingsUI.directSnappingEnabled.isOn = value;
            }
        }

        public Transform rayOrigin { get { return null; } }

        // Local method use only -- created here to reduce garbage collection
        readonly List<GameObject> m_CombinedIgnoreList = new List<GameObject>();
        Transform[] m_SingleTransformArray = new Transform[1];

        void Awake()
        {
            m_GroundPlane = ObjectUtils.Instantiate(m_GroundPlane, transform);
            m_GroundPlane.SetActive(false);

            m_ButtonHighlightMaterialClone = Instantiate(m_ButtonHighlightMaterial);

            widgetEnabled = true;
        }

        public object OnSerializePreferences()
        {
            return m_Preferences;
        }

        public void OnDeserializePreferences(object obj)
        {
            m_Preferences = (Preferences)obj;
        }

        void Update()
        {
            if (snappingEnabled)
            {
                var camera = CameraUtils.GetMainCamera();
                var shouldActivateGroundPlane = false;
                foreach (var statesForRay in m_SnappingStates)
                {
                    foreach (var kvp in statesForRay.Value)
                    {
                        var state = kvp.Value;
                        if (state.groundSnapping)
                            shouldActivateGroundPlane = true;

                        var widget = state.widget;
                        if (state.surfaceSnapping && widgetEnabled)
                        {
                            if (widget == null)
                            {
                                widget = ObjectUtils.Instantiate(m_Widget, transform).transform;
                                state.widget = widget;
                            }

                            widget.gameObject.SetActive(true);

                            var distanceToCamera = Vector3.Distance(camera.transform.position, state.snappingPosition);
                            widget.position = state.snappingPosition;
                            widget.rotation = state.snappingRotation;
                            widget.localScale = Vector3.one * k_WidgetScale * distanceToCamera;
                        }
                        else if (state.widget != null)
                            widget.gameObject.SetActive(false);
                    }
                }

                m_GroundPlane.SetActive(shouldActivateGroundPlane);

                if (shouldActivateGroundPlane)
                    m_GroundPlane.transform.localScale = Vector3.one * k_GroundPlaneScale * this.GetViewerScale();
            }
            else
            {
                m_GroundPlane.SetActive(false);
                m_Widget.SetActive(false);
            }
        }

        public bool ManipulatorSnap(Transform rayOrigin, Transform[] transforms, ref Vector3 position, ref Quaternion rotation, Vector3 delta, AxisFlags constraints, PivotMode pivotMode)
        {
            if (transforms.Length == 0)
                return false;

            if (delta == Vector3.zero)
                return false;

            if (snappingEnabled && manipulatorSnappingEnabled)
            {
                var state = GetSnappingState(rayOrigin, transforms, position, rotation);

                state.currentPosition += delta;
                var targetPosition = state.currentPosition;
                var targetRotation = state.startRotation;

                AddToIgnoreList(transforms);

                var breakScale = Vector3.Distance(CameraUtils.GetMainCamera().transform.position, position);

                switch (constraints)
                {
                    case 0:
                        if (limitRadius)
                        {
                            if (LocalSnapToSurface(ref position, ref rotation, targetPosition, targetRotation, state))
                                return true;
                        }
                        else
                        {
                            var pointerRay = new Ray(rayOrigin.position, rayOrigin.forward);
                            if (surfaceSnappingEnabled)
                            {
                                var bounds = state.identityBounds;
                                var boundsExtents = bounds.extents;
                                var projectedExtents = Vector3.Project(boundsExtents, Vector3.down);
                                var offset = projectedExtents - bounds.center;
                                var rotationOffset = Quaternion.AngleAxis(90, Vector3.right);
                                var startRotation = state.startRotation;
                                var upVector = startRotation * Vector3.back;
                                var maxRayLength = this.GetViewerScale() * k_SurfaceSnappingMaxRayLength;

                                if (SnapToSurface(pointerRay, ref position, ref rotation, state, offset, targetRotation, rotationOffset, upVector, maxRayLength))
                                    return true;

                                state.surfaceSnapping = false;
                            }

                            if (groundSnappingEnabled)
                            {
                                var raycastDistance = this.GetViewerScale() * k_GroundSnappingMaxRayLength;
                                if (SnapToGround(pointerRay, ref position, ref rotation, targetPosition, targetRotation, state, raycastDistance))
                                    return true;
                            }

                            if (TryBreakSnap(ref position, ref rotation, targetPosition, targetRotation, state, breakScale * k_BreakDistance))
                                return true;
                        }

                        break;
                    default:
                        if (ManipulatorSnapConstrained(ref position, ref rotation, delta, targetPosition, targetRotation, state, breakScale * k_BreakDistance, constraints, pivotMode))
                            return true;
                        break;
                }
            }

            position += delta;

            return false;
        }

        public bool DirectSnap(Transform rayOrigin, Transform transform, ref Vector3 position, ref Quaternion rotation, Vector3 targetPosition, Quaternion targetRotation)
        {
            if (snappingEnabled && directSnappingEnabled)
            {
                var state = GetSnappingState(rayOrigin, transform, position, rotation);

                state.currentPosition = targetPosition;

                AddToIgnoreList(transform);
                if (LocalSnapToSurface(ref position, ref rotation, targetPosition, targetRotation, state))
                    return true;
            }

            position = targetPosition;
            rotation = targetRotation;

            return false;
        }

        bool ManipulatorSnapConstrained(ref Vector3 position, ref Quaternion rotation, Vector3 delta, Vector3 targetPosition, Quaternion targetRotation, SnappingState state, float raycastDistance, AxisFlags constraints, PivotMode pivotMode)
        {
            var rotationOffset = Quaternion.AngleAxis(90, Vector3.right);
            var startRotation = state.startRotation;
            var upVector = startRotation * Vector3.back;

            var direction = delta.normalized;
            var bounds = state.identityBounds;
            var projectedExtents = Vector3.Project(rotation * bounds.extents, direction);
            var axisRay = new Ray(targetPosition, direction);

            var objectCenter = Vector3.zero;
            var offset = Vector3.zero;

            if (!pivotSnappingEnabled)
            {
                objectCenter = targetPosition;
                if (pivotMode != PivotMode.Center)
                    objectCenter += targetRotation * state.identityBounds.center;

                switch (constraints)
                {
                    case AxisFlags.X:
                        if (Vector3.Dot(rotation * Vector3.right, direction) > 0)
                            projectedExtents *= -1;
                        break;
                    case AxisFlags.Y:
                        if (Vector3.Dot(rotation * Vector3.up, direction) > 0)
                            projectedExtents *= -1;
                        break;
                    case AxisFlags.Z:
                        if (Vector3.Dot(rotation * Vector3.forward, direction) > 0)
                            projectedExtents *= -1;
                        break;
                }

                axisRay.origin = objectCenter - projectedExtents;
                offset = targetPosition - axisRay.origin;
            }

            if (state.snapping)
            {
                var breakDistance = raycastDistance;
                if (Vector3.Dot(targetPosition - position, state.snappingNormal) < 0)
                    breakDistance *= k_BlockedBreakScale;

                TryBreakSnap(ref position, ref rotation, targetPosition, startRotation, state, breakDistance);
                return true;
            }

            if (surfaceSnappingEnabled && SnapToSurface(axisRay, ref position, ref rotation, state, offset, targetRotation, rotationOffset, upVector, raycastDistance, constrained: true))
                return true;

            if (groundSnappingEnabled && SnapToGround(axisRay, ref position, ref rotation, targetPosition, targetRotation, state, raycastDistance, offset, true))
                return true;

            // Check other direction
            axisRay.direction *= -1;
            if (!pivotSnappingEnabled)
            {
                axisRay.origin = objectCenter + projectedExtents;
                offset = targetPosition - axisRay.origin;
            }

            if (surfaceSnappingEnabled && SnapToSurface(axisRay, ref position, ref rotation, state, offset, targetRotation, rotationOffset, upVector, raycastDistance, constrained: true))
                return true;

            if (groundSnappingEnabled && SnapToGround(axisRay, ref position, ref rotation, targetPosition, targetRotation, state, raycastDistance, offset, true))
                return true;

            if (TryBreakSnap(ref position, ref rotation, targetPosition, startRotation, state, raycastDistance))
                return true;

            return false;
        }

        bool LocalSnapToSurface(ref Vector3 position, ref Quaternion rotation, Vector3 targetPosition, Quaternion targetRotation, SnappingState state)
        {
            var bounds = state.identityBounds;
            var boundsCenter = bounds.center;
            var boundsExtents = bounds.extents;

            var viewerScale = this.GetViewerScale();
            var breakDistance = viewerScale * k_BreakDistance;

            if (state.snapping)
            {
                var directionIndex = state.directionIndex;
                var direction = k_Directions[directionIndex];
                var upVector = targetRotation * direction.upVector;
                var directionVector = direction.direction;
                var rotationOffset = direction.rotationOffset;

                var projectedExtents = Vector3.Project(boundsExtents, directionVector);
                var offset = -boundsCenter;
                if (directionIndex > 2)
                    offset -= projectedExtents;
                else
                    offset += projectedExtents;

                offset = rotation * offset;

                var snappingNormal = state.snappingNormal;
                var breakVector = targetPosition - position;
                if (Vector3.Dot(snappingNormal, breakVector) < 0)
                {
                    var boundsBreakDist = breakDistance * k_BlockedBreakScale;
                    var raycastDistance = projectedExtents.magnitude + breakVector.magnitude;
                    directionVector = targetRotation * directionVector;

                    var boundsRay = new Ray(targetPosition - Vector3.Project(breakVector, directionVector), directionVector);
                    if (pivotSnappingEnabled)
                    {
                        var extra = k_RayExtra * viewerScale;
                        raycastDistance += extra;
                        boundsRay.origin -= directionVector * extra;
                    }
                    else
                    {
                        boundsRay.origin += targetRotation * boundsCenter;
                    }

                    if (TryBreakSnap(ref position, ref rotation, targetPosition, targetRotation, state, boundsBreakDist))
                        return true;

                    if (surfaceSnappingEnabled && SnapToSurface(boundsRay, ref position, ref rotation, state, offset, targetRotation, rotationOffset, upVector, raycastDistance))
                        return true;

                    if (groundSnappingEnabled && SnapToGround(boundsRay, ref position, ref rotation, targetPosition, targetRotation, state, raycastDistance, offset))
                        return true;

                    return true;
                }

                if (TryBreakSnap(ref position, ref rotation, targetPosition, targetRotation, state, breakDistance))
                    return true;
            }

            for (var i = 0; i < k_Directions.Length; i++)
            {
                var direction = k_Directions[i];
                var upVector = targetRotation * direction.upVector;
                var directionVector = direction.direction;
                var rotationOffset = direction.rotationOffset;
                var offset = Vector3.zero;
                if (!pivotSnappingEnabled)
                {
                    var projectedExtents = Vector3.Project(boundsExtents, directionVector);
                    offset = -boundsCenter;
                    if (i > 2)
                        offset -= projectedExtents;
                    else
                        offset += projectedExtents;

                    offset = rotation * offset;
                }

                var raycastDistance = breakDistance * 2;
                directionVector = targetRotation * directionVector;
                var boundsRay = new Ray(targetPosition - offset - directionVector * breakDistance, directionVector);

                if (surfaceSnappingEnabled && SnapToSurface(boundsRay, ref position, ref rotation, state, offset, targetRotation, rotationOffset, upVector, raycastDistance, k_MaxRayDot))
                {
                    state.directionIndex = i;
                    return true;
                }

                if (groundSnappingEnabled && SnapToGround(boundsRay, ref position, ref rotation, targetPosition, targetRotation, state, raycastDistance, offset))
                {
                    state.directionIndex = i;
                    return true;
                }
            }

            if (TryBreakSnap(ref position, ref rotation, targetPosition, targetRotation, state, breakDistance))
                return true;

            if (TryBreakSnap(ref position, ref rotation, state.snappingPosition, targetRotation, state, breakDistance))
                return true;

            return false;
        }

        static bool TryBreakSnap(ref Vector3 position, ref Quaternion rotation, Vector3 targetPosition, Quaternion targetRotation, SnappingState state, float breakDistance)
        {
            if (state.snapping)
            {
                if (Vector3.Distance(position, targetPosition) > breakDistance)
                {
                    position = targetPosition;
                    rotation = targetRotation;
                    state.surfaceSnapping = false;
                    state.groundSnapping = false;
                    return true;
                }
            }
            return false;
        }

        void AddToIgnoreList(Transform transform)
        {
            m_SingleTransformArray[0] = transform;
            AddToIgnoreList(m_SingleTransformArray);
        }

        void AddToIgnoreList(Transform[] transforms)
        {
            m_CombinedIgnoreList.Clear();

            for (var i = 0; i < transforms.Length; i++)
            {
                var renderers = transforms[i].GetComponentsInChildren<Renderer>();
                for (var j = 0; j < renderers.Length; j++)
                {
                    m_CombinedIgnoreList.Add(renderers[j].gameObject);
                }
            }

            for (var i = 0; i < ignoreList.Count; i++)
            {
                m_CombinedIgnoreList.Add(ignoreList[i]);
            }
        }

        bool SnapToSurface(Ray ray, ref Vector3 position, ref Quaternion rotation, SnappingState state, Vector3 boundsOffset, Quaternion targetRotation, Quaternion rotationOffset, Vector3 upVector, float raycastDistance, float maxRayDot = Mathf.Infinity, bool constrained = false)
        {
            RaycastHit hit;
            GameObject go;
            if (this.Raycast(ray, out hit, out go, raycastDistance, m_CombinedIgnoreList))
            {
                if (Vector3.Dot(ray.direction, hit.normal) > maxRayDot)
                    return false;

                if (!state.surfaceSnapping && hit.distance > raycastDistance * k_SnapDistanceScale)
                    return false;

                var snappedRotation = Quaternion.LookRotation(hit.normal, upVector) * rotationOffset;

                state.snappingNormal = hit.normal;
                var hitPoint = hit.point;

                var snappedPosition = pivotSnappingEnabled ? hitPoint : hitPoint + boundsOffset;

                state.surfaceSnapping = true;
                state.groundSnapping = false;

                position = snappedPosition;
                rotation = !constrained && rotationSnappingEnabled ? snappedRotation : targetRotation;

                state.snappingPosition = hitPoint;
                state.snappingRotation = snappedRotation;

                return true;
            }

            return false;
        }

        bool SnapToGround(Ray ray, ref Vector3 position, ref Quaternion rotation, Vector3 targetPosition, Quaternion targetRotation, SnappingState state, float raycastDistance, Vector3 boundsOffset = default(Vector3), bool constrained = false)
        {
            if (Mathf.Approximately(Vector3.Dot(ray.direction, Vector3.up), 0))
                return false;

            var groundPlane = new Plane(Vector3.up, k_GroundHeight);
            float distance;
            if (groundPlane.Raycast(ray, out distance) && distance <= raycastDistance)
            {
                state.groundSnapping = true;

                state.snappingNormal = -Vector3.Project(ray.direction, Vector3.up).normalized;

                var hitPoint = ray.origin + ray.direction * distance;
                var snappedPosition = pivotSnappingEnabled ? hitPoint : hitPoint + boundsOffset;
                position = snappedPosition;

                if (!constrained && rotationSnappingEnabled)
                    rotation = Quaternion.LookRotation(Vector3.up, targetRotation * Vector3.back) * Quaternion.AngleAxis(90, Vector3.right);
                else
                    rotation = targetRotation;

                return true;
            }

            state.groundSnapping = false;
            position = targetPosition;
            rotation = targetRotation;

            return false;
        }

        SnappingState GetSnappingState(Transform rayOrigin, Transform transform, Vector3 position, Quaternion rotation)
        {
            m_SingleTransformArray[0] = transform;
            return GetSnappingState(rayOrigin, m_SingleTransformArray, position, rotation);
        }

        SnappingState GetSnappingState(Transform rayOrigin, Transform[] transforms, Vector3 position, Quaternion rotation)
        {
            Dictionary<Transform, SnappingState> states;
            if (!m_SnappingStates.TryGetValue(rayOrigin, out states))
            {
                states = new Dictionary<Transform, SnappingState>();
                m_SnappingStates[rayOrigin] = states;
            }

            var firstObject = transforms[0];
            SnappingState state;
            if (!states.TryGetValue(firstObject, out state))
            {
                state = new SnappingState(transforms, position, rotation);
                states[firstObject] = state;
            }
            return state;
        }

        public void ClearSnappingState(Transform rayOrigin)
        {
            Dictionary<Transform, SnappingState> states;
            if (m_SnappingStates.TryGetValue(rayOrigin, out states))
            {
                foreach (var kvp in states)
                {
                    kvp.Value.OnDestroy();
                }
                m_SnappingStates.Remove(rayOrigin);
            }
        }

        void Reset()
        {
            foreach (var statesForRay in m_SnappingStates)
            {
                foreach (var kvp in statesForRay.Value)
                {
                    kvp.Value.OnDestroy();
                }
            }
            m_SnappingStates.Clear();
        }

        void SetupUI()
        {
#if INCLUDE_TEXT_MESH_PRO
            var snappingEnabledUI = m_SnappingModuleSettingsUI.snappingEnabled;
            var text = snappingEnabledUI.GetComponentInChildren<TextMeshProUGUI>();
            snappingEnabledUI.isOn = !m_Preferences.disableAll;
            snappingEnabledUI.onValueChanged.AddListener(b =>
            {
                m_Preferences.disableAll = !snappingEnabledUI.isOn;
                text.text = m_Preferences.disableAll ? "Snapping disabled" : "Snapping enabled";
                Reset();
                SetDependentTogglesGhosted();
            });

            var handle = snappingEnabledUI.GetComponent<BaseHandle>();
            handle.hoverStarted += (baseHandle, data) => { text.text = m_Preferences.disableAll ? "Enable snapping" : "Disable snapping"; };
            handle.hoverEnded += (baseHandle, data) => { text.text = m_Preferences.disableAll ? "Snapping disabled" : "Snapping enabled"; };
#endif

            var groundSnappingUI = m_SnappingModuleSettingsUI.groundSnappingEnabled;
            groundSnappingUI.isOn = m_Preferences.groundSnappingEnabled;
            groundSnappingUI.onValueChanged.AddListener(b =>
            {
                m_Preferences.groundSnappingEnabled = groundSnappingUI.isOn;
                Reset();
            });

            var surfaceSnappingUI = m_SnappingModuleSettingsUI.surfaceSnappingEnabled;
            surfaceSnappingUI.isOn = m_Preferences.surfaceSnappingEnabled;
            surfaceSnappingUI.onValueChanged.AddListener(b =>
            {
                m_Preferences.surfaceSnappingEnabled = surfaceSnappingUI.isOn;
                Reset();
            });

            var pivotSnappingUI = m_SnappingModuleSettingsUI.pivotSnappingEnabled;
            m_SnappingModuleSettingsUI.SetToggleValue(pivotSnappingUI, m_Preferences.pivotSnappingEnabled);
            pivotSnappingUI.onValueChanged.AddListener(b => { m_Preferences.pivotSnappingEnabled = pivotSnappingUI.isOn; });

            var snapRotationUI = m_SnappingModuleSettingsUI.rotationSnappingEnabled;
            snapRotationUI.isOn = m_Preferences.rotationSnappingEnabled;
            snapRotationUI.onValueChanged.AddListener(b => { m_Preferences.rotationSnappingEnabled = snapRotationUI.isOn; });

            var localOnlyUI = m_SnappingModuleSettingsUI.limitRadius;
            localOnlyUI.isOn = m_Preferences.limitRadius;
            localOnlyUI.onValueChanged.AddListener(b => { m_Preferences.limitRadius = localOnlyUI.isOn; });

            var manipulatorSnappingUI = m_SnappingModuleSettingsUI.manipulatorSnappingEnabled;
            manipulatorSnappingUI.isOn = m_Preferences.manipulatorSnappingEnabled;
            manipulatorSnappingUI.onValueChanged.AddListener(b => { m_Preferences.manipulatorSnappingEnabled = manipulatorSnappingUI.isOn; });

            var directSnappingUI = m_SnappingModuleSettingsUI.directSnappingEnabled;
            directSnappingUI.isOn = m_Preferences.directSnappingEnabled;
            directSnappingUI.onValueChanged.AddListener(b => { m_Preferences.directSnappingEnabled = directSnappingUI.isOn; });

            SetDependentTogglesGhosted();

            SetSessionGradientMaterial(m_SnappingModuleSettingsUI.GetComponent<SubmenuFace>().gradientPair);
        }

        void SetDependentTogglesGhosted()
        {
            var toggles = new List<Toggle>
            {
                m_SnappingModuleSettingsUI.groundSnappingEnabled,
                m_SnappingModuleSettingsUI.surfaceSnappingEnabled,
                m_SnappingModuleSettingsUI.rotationSnappingEnabled,
                m_SnappingModuleSettingsUI.limitRadius,
                m_SnappingModuleSettingsUI.manipulatorSnappingEnabled,
                m_SnappingModuleSettingsUI.directSnappingEnabled
            };

            toggles.AddRange(m_SnappingModuleSettingsUI.pivotSnappingEnabled.group.GetComponentsInChildren<Toggle>(true));

            foreach (var toggle in toggles)
            {
                toggle.interactable = !m_Preferences.disableAll;
                if (toggle.isOn)
                    toggle.graphic.gameObject.SetActive(!m_Preferences.disableAll);
            }

#if INCLUDE_TEXT_MESH_PRO
            foreach (var text in m_SnappingModuleSettingsUI.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                text.color = m_Preferences.disableAll ? Color.gray : Color.white;
            }
#endif
        }

        void SetSessionGradientMaterial(GradientPair gradientPair)
        {
            m_ButtonHighlightMaterialClone.SetColor(k_MaterialColorLeftProperty, gradientPair.a);
            m_ButtonHighlightMaterialClone.SetColor(k_MaterialColorRightProperty, gradientPair.b);
            foreach (var graphic in m_SnappingModuleSettingsUI.GetComponentsInChildren<Graphic>())
            {
                if (graphic.material == m_ButtonHighlightMaterial)
                    graphic.material = m_ButtonHighlightMaterialClone;
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    [MainMenuItem("MiniWorld", "Workspaces", "Edit a smaller version of your scene(s)", typeof(MiniWorldTooltip))]
    [SpatialMenuItem("MiniWorld", "Workspaces", "Edit a smaller version of your scene(s)")]
    sealed class MiniWorldWorkspace : Workspace, ISerializeWorkspace, IRequestFeedback, IControlHaptics, IRayToNode
    {
        class MiniWorldTooltip : ITooltip
        {
            public string tooltipText
            {
                get
                {
#if UNITY_EDITOR
                    return PlayerSettings.stereoRenderingPath == StereoRenderingPath.MultiPass
                        ? string.Empty
                        : "Not currently working in single pass";
#else
                    return string.Empty;
#endif
                }
            }
        }

        static readonly float k_InitReferenceYOffset = DefaultBounds.y / 2.05f; // Show more space above ground than below

        static readonly Vector3 k_LocatePlayerOffset = new Vector3(0.1385f, 0.035f, -0.05f);
        static readonly float k_LocatePlayerArrowOffset = 0.05f;

        const float k_InitReferenceScale = 15f; // We want to see a big region by default

        const string k_LeftActionString = "Move Resize Left";
        const string k_RightActionString = "Move Resize Right";

        const string k_MoveFeedbackString = "Move Miniworld View";
        const string k_ScaleFeedbackString = "Scale Miniworld View";

        // Scales larger or smaller than this spam errors in the console
        const float k_MinScale = 0.01f;
        const float k_MaxScale = 1e12f;

        // Scale slider min/max (maps to referenceTransform uniform scale)
        const float k_ZoomSliderMin = 0.5f;
        const float k_ZoomSliderMax = 200f;

        [SerializeField]
        GameObject m_ContentPrefab;

        [SerializeField]
        GameObject m_RecenterUIPrefab;

        [SerializeField]
        GameObject m_LocatePlayerPrefab;

        [SerializeField]
        GameObject m_PlayerDirectionArrowPrefab;

        [SerializeField]
        GameObject m_ZoomSliderPrefab;

        [SerializeField]
        HapticPulse m_EnterExitPulse;

        [Serializable]
        class Preferences
        {
            [SerializeField]
            Vector3 m_MiniWorldRefeferenceScale;

            [SerializeField]
            Vector3 m_MiniWorldReferencePosition;

            [SerializeField]
            float m_ZoomSliderValue;

            public Vector3 miniWorldRefeferenceScale
            {
                get { return m_MiniWorldRefeferenceScale; }
                set { m_MiniWorldRefeferenceScale = value; }
            }

            public Vector3 miniWorldReferencePosition
            {
                get { return m_MiniWorldReferencePosition; }
                set { m_MiniWorldReferencePosition = value; }
            }

            public float zoomSliderValue
            {
                get { return m_ZoomSliderValue; }
                set { m_ZoomSliderValue = value; }
            }
        }

        MiniWorldUI m_MiniWorldUI;
        MiniWorld m_MiniWorld;
        Material m_GridMaterial;
        ZoomSliderUI m_ZoomSliderUI;
        Transform m_LocatePlayerUI;
        Transform m_PlayerDirectionButton;
        Transform m_PlayerDirectionArrow;
        readonly List<Transform> m_Rays = new List<Transform>(2);
        float m_StartScale;
        float m_StartDistance;
        Vector3 m_StartPosition;
        Vector3 m_StartOffset;
        Vector3 m_StartMidPoint;
        Vector3 m_StartDirection;
        float m_StartYaw;

        // Bools denoting that a given proxy/rayOrigin is contained/inside of the workpace bounds.  Used for haptis.
        bool m_LeftRayOriginContained;
        bool m_RightRayOriginContained;

        Coroutine m_UpdateLocationCoroutine;

        readonly BindingDictionary m_Controls = new BindingDictionary();
        readonly List<ProxyFeedbackRequest> m_LeftMoveFeedback = new List<ProxyFeedbackRequest>();
        readonly List<ProxyFeedbackRequest> m_RightMoveFeedback = new List<ProxyFeedbackRequest>();
        readonly List<ProxyFeedbackRequest> m_LeftScaleFeedback = new List<ProxyFeedbackRequest>();
        readonly List<ProxyFeedbackRequest> m_RightScaleFeedback = new List<ProxyFeedbackRequest>();
        readonly List<ProxyFeedbackRequest> m_SuppressFeedback = new List<ProxyFeedbackRequest>();

        public IMiniWorld miniWorld
        {
            get { return m_MiniWorld; }
        }

        public float zoomSliderMax
        {
            set { m_ZoomSliderUI.zoomSlider.maxValue = Mathf.Log10(value); }
        }

        public override void Setup()
        {
            const float yBounds = 0.5f;

            // Initial bounds must be set before the base.Setup() is called
            minBounds = new Vector3(MinBounds.x, yBounds, 0.25f);
            m_CustomStartingBounds = new Vector3(MinBounds.x, yBounds, 0.5f);

            base.Setup();

            ObjectUtils.Instantiate(m_ContentPrefab, m_WorkspaceUI.sceneContainer, false);
            m_MiniWorldUI = GetComponentInChildren<MiniWorldUI>();
            m_GridMaterial = MaterialUtils.GetMaterialClone(m_MiniWorldUI.grid);

            var resetUI = ObjectUtils.Instantiate(m_RecenterUIPrefab, m_WorkspaceUI.frontPanel, false).GetComponentInChildren<ResetUI>();
            resetUI.resetButton.onClick.AddListener(ResetChessboard);
            foreach (var mb in resetUI.GetComponentsInChildren<MonoBehaviour>())
            {
                this.ConnectInterfaces(mb);
            }

            var parent = m_WorkspaceUI.frontPanel.parent;
            m_LocatePlayerUI = ObjectUtils.Instantiate(m_LocatePlayerPrefab, parent, false).transform;
            m_PlayerDirectionButton = m_LocatePlayerUI.GetChild(0);
            foreach (var mb in m_LocatePlayerUI.GetComponentsInChildren<MonoBehaviour>())
            {
                var button = mb as Button;
                if (button)
                    button.onClick.AddListener(RecenterOnPlayer);
            }

            m_PlayerDirectionArrow = ObjectUtils.Instantiate(m_PlayerDirectionArrowPrefab, parent, false).transform;

            // Set up MiniWorld
            m_MiniWorld = GetComponentInChildren<MiniWorld>();
            m_MiniWorld.referenceTransform.position = Vector3.up * k_InitReferenceYOffset * k_InitReferenceScale;
            m_MiniWorld.referenceTransform.localScale = Vector3.one * k_InitReferenceScale;

            // Set up Zoom Slider
            var sliderObject = ObjectUtils.Instantiate(m_ZoomSliderPrefab, m_WorkspaceUI.frontPanel, false);
            m_ZoomSliderUI = sliderObject.GetComponentInChildren<ZoomSliderUI>();
            m_ZoomSliderUI.sliding += OnSliding;
            m_ZoomSliderUI.zoomSlider.maxValue = Mathf.Log10(k_ZoomSliderMax);
            m_ZoomSliderUI.zoomSlider.minValue = Mathf.Log10(k_ZoomSliderMin);
            m_ZoomSliderUI.zoomSlider.direction = Slider.Direction.RightToLeft; // Invert direction for expected ux; zoom in as slider moves left to right
            m_ZoomSliderUI.zoomSlider.value = Mathf.Log10(k_InitReferenceScale);
            foreach (var mb in m_ZoomSliderUI.GetComponentsInChildren<MonoBehaviour>())
            {
                this.ConnectInterfaces(mb);
            }

            var zoomTooltip = sliderObject.GetComponentInChildren<Tooltip>();
            if (zoomTooltip)
                zoomTooltip.tooltipText = "Drag the Handle to Zoom the Mini World";

            // Propagate initial bounds
            OnBoundsChanged();
        }

        public object OnSerializeWorkspace()
        {
            var preferences = new Preferences();

            var referenceTransform = m_MiniWorld.referenceTransform;
            preferences.miniWorldRefeferenceScale = referenceTransform.localScale;
            preferences.miniWorldReferencePosition = referenceTransform.position;
            preferences.zoomSliderValue = m_ZoomSliderUI.zoomSlider.value;

            return preferences;
        }

        public void OnDeserializeWorkspace(object obj)
        {
            var preferences = (Preferences)obj;

            var referenceTransform = m_MiniWorld.referenceTransform;
            referenceTransform.localScale = preferences.miniWorldRefeferenceScale;
            referenceTransform.position = preferences.miniWorldReferencePosition;
            m_ZoomSliderUI.zoomSlider.value = preferences.zoomSliderValue;
        }

        void Update()
        {
            var inBounds = IsPlayerInBounds();
            m_PlayerDirectionButton.gameObject.SetActive(!inBounds);
            m_PlayerDirectionArrow.gameObject.SetActive(!inBounds);

            if (!inBounds)
                UpdatePlayerDirectionArrow();

            //Set grid height, deactivate if out of bounds
            var referenceTransform = m_MiniWorld.referenceTransform;
            float gridHeight = referenceTransform.position.y / referenceTransform.localScale.y;
            var grid = m_MiniWorldUI.grid;

            var inverseRotation = Quaternion.Inverse(referenceTransform.rotation);
            var gridTransform = grid.transform;
            if (Mathf.Abs(gridHeight) < contentBounds.extents.y)
            {
                grid.gameObject.SetActive(true);
                gridTransform.localPosition = Vector3.down * gridHeight;
                gridTransform.localRotation = inverseRotation * Quaternion.AngleAxis(90, Vector3.right);
            }
            else
            {
                grid.gameObject.SetActive(false);
            }

            var referenceScale = referenceTransform.localScale.x;
            var gridScale = gridTransform.localScale.x;

            m_GridMaterial.SetFloat("_GridFade", referenceScale);
            m_GridMaterial.SetFloat("_GridScale", referenceScale * gridScale);
            m_GridMaterial.SetVector("_GridCenter", -new Vector2(referenceTransform.position.x,
                referenceTransform.position.z) / (gridScale * referenceScale));
            inverseRotation = Quaternion.Inverse(m_MiniWorld.transform.rotation);
            m_GridMaterial.SetMatrix("_InverseRotation", Matrix4x4.TRS(Vector3.zero, inverseRotation, Vector3.one));
            m_GridMaterial.SetVector("_ClipExtents", m_MiniWorld.localBounds.extents * this.GetViewerScale() * transform.localScale.x);
            m_GridMaterial.SetVector("_ClipCenter", inverseRotation * m_MiniWorld.transform.position);
        }

        public override void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
        {
            if (m_Controls.Count == 0)
                InputUtils.GetBindingDictionaryFromActionMap(input.actionMap, m_Controls);

            base.ProcessInput(input, consumeControl);
            var workspaceInput = (WorkspaceInput)input;

            var leftControl = workspaceInput.miniWorldPanZoomLeft;
            var leftContained = miniWorld.Contains(leftRayOrigin.position);
            var rightControl = workspaceInput.miniWorldPanZoomRight;
            var rightContained = miniWorld.Contains(rightRayOrigin.position);

            if (leftContained && !m_LeftRayOriginContained)
                this.Pulse(leftNode, m_EnterExitPulse);
            else if (!leftContained && m_LeftRayOriginContained)
                this.Pulse(leftNode, m_EnterExitPulse);

            if (rightContained && !m_RightRayOriginContained)
                this.Pulse(rightNode, m_EnterExitPulse);
            else if (!rightContained && m_RightRayOriginContained)
                this.Pulse(rightNode, m_EnterExitPulse);

            m_LeftRayOriginContained = leftContained;
            m_RightRayOriginContained = rightContained;

            if (leftControl.isHeld && rightControl.isHeld)
                ShowSuppressFeedback();
            else
                HideSuppressFeedback();

            if (leftContained)
            {
                if (m_LeftMoveFeedback.Count == 0)
                    ShowLeftMoveFeedback();

                if (rightContained && rightControl.isHeld && m_LeftScaleFeedback.Count == 0)
                    ShowLeftScaleFeedback();

                if (leftControl.wasJustPressed)
                {
                    OnPanZoomDragStarted(leftRayOrigin);
                    consumeControl(leftControl);
                }
            }
            else
            {
                HideLeftMoveFeedback();
            }

            if (rightContained)
            {
                if (m_RightMoveFeedback.Count == 0)
                    ShowRightFeedback();

                if (leftContained && leftControl.isHeld && m_RightScaleFeedback.Count == 0)
                    ShowRightScaleFeedback();

                if (rightControl.wasJustPressed)
                {
                    OnPanZoomDragStarted(rightRayOrigin);
                    consumeControl(rightControl);
                }
            }
            else
            {
                HideRightMoveFeedback();
            }

            if (leftControl.isHeld || rightControl.isHeld)
                OnPanZoomDragging();

            if (leftControl.wasJustReleased)
            {
                OnPanZoomDragEnded(leftRayOrigin);
                HideRightScaleFeedback();
            }

            if (rightControl.wasJustReleased)
            {
                OnPanZoomDragEnded(rightRayOrigin);
                HideLeftScaleFeedback();
            }
        }

        protected override void OnBoundsChanged()
        {
            m_MiniWorld.transform.localPosition = Vector3.up * contentBounds.extents.y;

            var boundsWithMargin = contentBounds;
            var size = contentBounds.size;
            size.x -= FaceMargin;
            size.z -= FaceMargin;
            boundsWithMargin.size = size;
            m_MiniWorld.localBounds = boundsWithMargin;
            m_MiniWorldUI.boundsCube.transform.localScale = size;
            m_MiniWorldUI.grid.transform.localScale = Vector3.one * new Vector2(size.x, size.z).magnitude;

            m_LocatePlayerUI.localPosition = Vector3.left * boundsWithMargin.extents.x + k_LocatePlayerOffset;
            m_PlayerDirectionArrow.localPosition = m_LocatePlayerUI.localPosition + Vector3.up * k_LocatePlayerArrowOffset;
        }

        void OnSliding(float value)
        {
            ScaleMiniWorld(Mathf.Pow(10, value));
        }

        void ScaleMiniWorld(float value)
        {
            var scaleDiff = (value - m_MiniWorld.referenceTransform.localScale.x) / m_MiniWorld.referenceTransform.localScale.x;
            m_MiniWorld.referenceTransform.position += Vector3.up * m_MiniWorld.referenceBounds.extents.y * scaleDiff;
            m_MiniWorld.referenceTransform.localScale = Vector3.one * value;
        }

        void OnPanZoomDragStarted(Transform rayOrigin)
        {
            var referenceTransform = miniWorld.referenceTransform;
            m_StartPosition = referenceTransform.position;
            var rayOriginPosition = miniWorld.miniWorldTransform.InverseTransformPoint(rayOrigin.position);

            // On introduction of second ray
            if (m_Rays.Count == 1)
            {
                var rayToRay = miniWorld.miniWorldTransform.InverseTransformPoint(m_Rays[0].position) - rayOriginPosition;
                var midPoint = rayOriginPosition + rayToRay * 0.5f;
                m_StartScale = referenceTransform.localScale.x;
                m_StartDistance = rayToRay.magnitude;
                m_StartMidPoint = MathUtilsExt.ConstrainYawRotation(referenceTransform.rotation) * midPoint;
                m_StartDirection = rayToRay;
                m_StartDirection.y = 0;
                m_StartOffset = m_StartMidPoint * m_StartScale;

                m_StartPosition += m_StartOffset;
                m_StartYaw = referenceTransform.rotation.eulerAngles.y;
            }
            else
            {
                m_StartMidPoint = rayOriginPosition;
            }
            m_Rays.Add(rayOrigin);
        }

        void OnPanZoomDragging()
        {
            var rayCount = m_Rays.Count;
            if (rayCount == 0)
                return;

            var firstRayPosition = miniWorld.miniWorldTransform.InverseTransformPoint(m_Rays[0].position);
            var referenceTransform = m_MiniWorld.referenceTransform;

            // If we have two rays, scale
            if (rayCount > 1)
            {
                var secondRayPosition = miniWorld.miniWorldTransform.InverseTransformPoint(m_Rays[1].position);
                var rayToRay = firstRayPosition - secondRayPosition;
                var midPoint = secondRayPosition + rayToRay * 0.5f;

                var scaleFactor = m_StartDistance / rayToRay.magnitude;
                var currentScale = m_StartScale * scaleFactor;
                currentScale = Mathf.Clamp(currentScale, k_MinScale, k_MaxScale);
                scaleFactor = currentScale / m_StartScale;

                m_ZoomSliderUI.zoomSlider.value = Mathf.Log10(referenceTransform.localScale.x);

                rayToRay.y = 0;
                var yawSign = Mathf.Sign(Vector3.Dot(Quaternion.AngleAxis(90, Vector3.down) * m_StartDirection, rayToRay));
                var rotationDiff = Vector3.Angle(m_StartDirection, rayToRay) * yawSign;
                var currentRotation = Quaternion.AngleAxis(m_StartYaw + rotationDiff, Vector3.up);
                var worldMidPoint = currentRotation * midPoint;
                referenceTransform.rotation = currentRotation;
                referenceTransform.localScale = Vector3.one * currentScale;

                referenceTransform.position = m_StartPosition - m_StartOffset * scaleFactor
                    + (m_StartMidPoint - worldMidPoint) * currentScale;

                this.Pulse(Node.LeftHand, m_MovePulse);
                this.Pulse(Node.RightHand, m_MovePulse);
            }
            else
            {
                referenceTransform.position = m_StartPosition + referenceTransform.rotation
                    * Vector3.Scale(m_StartMidPoint - firstRayPosition, referenceTransform.localScale);

                this.Pulse(this.RequestNodeFromRayOrigin(m_Rays[0]), m_MovePulse);
            }
        }

        void OnPanZoomDragEnded(Transform rayOrigin)
        {
            m_Rays.RemoveAll(rayData => rayData.Equals(rayOrigin));

            // Set up remaining ray with new offset
            if (m_Rays.Count > 0)
            {
                var firstRay = m_Rays[0];
                var referenceTransform = m_MiniWorld.referenceTransform;
                m_StartMidPoint = miniWorld.miniWorldTransform.InverseTransformPoint(firstRay.position);
                m_StartPosition = referenceTransform.position;
                m_StartScale = referenceTransform.localScale.x;
            }
        }

        void RecenterOnPlayer()
        {
            this.RestartCoroutine(ref m_UpdateLocationCoroutine, UpdateLocation(CameraUtils.GetMainCamera().transform.position));
        }

        void ResetChessboard()
        {
            ScaleMiniWorld(k_InitReferenceScale);
            m_ZoomSliderUI.zoomSlider.value = Mathf.Log10(m_MiniWorld.referenceTransform.localScale.x);

            this.RestartCoroutine(ref m_UpdateLocationCoroutine, UpdateLocation(Vector3.up * k_InitReferenceYOffset * k_InitReferenceScale));
        }

        IEnumerator UpdateLocation(Vector3 targetPosition)
        {
            const float kTargetDuration = 0.25f;
            var transform = m_MiniWorld.referenceTransform;
            var smoothVelocity = Vector3.zero;
            var currentDuration = 0f;
            while (currentDuration < kTargetDuration)
            {
                currentDuration += Time.deltaTime;
                transform.position = MathUtilsExt.SmoothDamp(transform.position, targetPosition, ref smoothVelocity, kTargetDuration, Mathf.Infinity, Time.deltaTime);
                yield return null;
            }

            transform.position = targetPosition;
        }

        bool IsPlayerInBounds()
        {
            return m_MiniWorld.referenceBounds.Contains(CameraUtils.GetMainCamera().transform.position);
        }

        void UpdatePlayerDirectionArrow()
        {
            var directionArrowTransform = m_PlayerDirectionArrow.transform;
            var playerPos = CameraUtils.GetMainCamera().transform.position;
            var miniWorldPos = m_MiniWorld.referenceTransform.position;
            var targetDir = playerPos - miniWorldPos;
            var newDir = Vector3.RotateTowards(directionArrowTransform.up, targetDir, 360f, 360f);

            directionArrowTransform.localRotation = Quaternion.LookRotation(newDir);
            directionArrowTransform.Rotate(Vector3.right, -90.0f);
        }

        protected override void OnDestroy()
        {
            ObjectUtils.Destroy(m_GridMaterial);
            base.OnDestroy();
        }

        void ShowFeedback(List<ProxyFeedbackRequest> requests, Node node, string controlName, string tooltipText, bool suppressExisting = false)
        {
            List<VRInputDevice.VRControl> ids;
            if (m_Controls.TryGetValue(controlName, out ids))
            {
                foreach (var id in ids)
                {
                    var request = (ProxyFeedbackRequest)this.GetFeedbackRequestObject(typeof(ProxyFeedbackRequest));
                    request.node = node;
                    request.control = id;
                    request.priority = 1;
                    request.tooltipText = tooltipText;
                    request.suppressExisting = suppressExisting;
                    requests.Add(request);
                    this.AddFeedbackRequest(request);
                }
            }
        }

        void ShowLeftMoveFeedback()
        {
            ShowFeedback(m_LeftMoveFeedback, Node.LeftHand, k_LeftActionString, k_MoveFeedbackString);
        }

        void ShowRightFeedback()
        {
            ShowFeedback(m_RightMoveFeedback, Node.RightHand, k_RightActionString, k_MoveFeedbackString);
        }

        void ShowLeftScaleFeedback()
        {
            ShowFeedback(m_LeftScaleFeedback, Node.LeftHand, k_LeftActionString, k_ScaleFeedbackString);
        }

        void ShowRightScaleFeedback()
        {
            ShowFeedback(m_RightScaleFeedback, Node.RightHand, k_RightActionString, k_ScaleFeedbackString);
        }

        void ShowSuppressFeedback()
        {
            ShowFeedback(m_SuppressFeedback, Node.LeftHand, k_LeftActionString, null, true);
            ShowFeedback(m_SuppressFeedback, Node.RightHand, k_RightActionString, null, true);
        }

        void HideFeedback(List<ProxyFeedbackRequest> requests)
        {
            foreach (var request in requests)
            {
                this.RemoveFeedbackRequest(request);
            }
            requests.Clear();
        }

        void HideLeftMoveFeedback()
        {
            HideFeedback(m_LeftMoveFeedback);
        }

        void HideRightMoveFeedback()
        {
            HideFeedback(m_RightMoveFeedback);
        }

        void HideLeftScaleFeedback()
        {
            HideFeedback(m_LeftScaleFeedback);
        }

        void HideRightScaleFeedback()
        {
            HideFeedback(m_RightScaleFeedback);
        }

        void HideSuppressFeedback()
        {
            HideFeedback(m_SuppressFeedback);
        }
    }
}

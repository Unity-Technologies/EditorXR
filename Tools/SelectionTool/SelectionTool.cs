
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Tools
{
    sealed class SelectionTool : MonoBehaviour, ITool, IUsesRayOrigin, IUsesRaycastResults, ICustomActionMap,
        ISetHighlight, ISelectObject, ISetManipulatorsVisible, IIsHoveringOverUI, IUsesDirectSelection, ILinkedObject,
        ICanGrabObject, IGetManipulatorDragState, IUsesNode, IGetRayVisibility, IIsMainMenuVisible, IIsInMiniWorld,
        IRayToNode, IGetDefaultRayColor, ISetDefaultRayColor, ITooltip, ITooltipPlacement, ISetTooltipVisibility,
        IUsesDeviceType, IMenuIcon, IUsesPointer, IRayVisibilitySettings, IUsesViewerScale, ICheckBounds,
        ISettingsMenuItemProvider, ISerializePreferences, IStandardIgnoreList, IBlockUIInteraction, IRequestFeedback,
        IGetVRPlayerObjects
    {
        [Serializable]
        class Preferences
        {
            [SerializeField]
            bool m_SphereMode;

            public bool sphereMode
            {
                get { return m_SphereMode; }
                set { m_SphereMode = value; }
            }
        }

        const float k_MultiselectHueShift = 0.5f;
        static readonly Vector3 k_TouchTooltipPosition = new Vector3(0, -0.08f, -0.13f);
        static readonly Vector3 k_ViveTooltipPosition = new Vector3(0, 0.05f, -0.18f);
        const float k_BLockSelectDragThreshold = 0.01f;
        static readonly Quaternion k_TooltipRotation = Quaternion.AngleAxis(90, Vector3.right);

        [SerializeField]
        Sprite m_Icon;

        [SerializeField]
        ActionMap m_ActionMap;

        [SerializeField]
        GameObject m_BlockSelectCube;

        [SerializeField]
        GameObject m_BlockSelectSphere;

        [SerializeField]
        GameObject m_SettingsMenuItemPrefab;

        Preferences m_Preferences;

        GameObject m_PressedObject;

        SelectionInput m_SelectionInput;

        float m_LastMultiSelectClickTime;
        Color m_NormalRayColor;
        Color m_MultiselectRayColor;
        bool m_MultiSelect;
        bool m_HasDirectHover;
        bool m_BlockSelect;
        Vector3 m_SelectStartPosition;
        Renderer m_BlockSelectCubeRenderer;

        readonly BindingDictionary m_Controls = new BindingDictionary();
        readonly List<ProxyFeedbackRequest> m_SelectFeedback = new List<ProxyFeedbackRequest>();
        readonly List<ProxyFeedbackRequest> m_DirectSelectFeedback = new List<ProxyFeedbackRequest>();

        Toggle m_CubeToggle;
        Toggle m_SphereToggle;
        bool m_BlockValueChangedListener;

        readonly Dictionary<Transform, GameObject> m_HoverGameObjects = new Dictionary<Transform, GameObject>();

        readonly Dictionary<Transform, GameObject> m_SelectionHoverGameObjects = new Dictionary<Transform, GameObject>();
        readonly List<GameObject> m_BlockSelectHoverGameObjects = new List<GameObject>();

        public ActionMap actionMap { get { return m_ActionMap; } }
        public bool ignoreLocking { get { return false; } }

        public Transform rayOrigin { get; set; }
        public Node node { private get; set; }

        public Sprite icon { get { return m_Icon; } }

        public event Action<GameObject, Transform> hovered;

        public List<GameObject> ignoreList { private get; set; }
        public List<ILinkedObject> linkedObjects { get; set; }

        public string tooltipText { get { return m_MultiSelect ? "Multi-Select Enabled" : ""; } }
        public Transform tooltipTarget { get; private set; }
        public Transform tooltipSource { get { return rayOrigin; } }
        public TextAlignment tooltipAlignment { get { return TextAlignment.Center; } }

        public GameObject settingsMenuItemPrefab { get { return m_SettingsMenuItemPrefab; } }

        public GameObject settingsMenuItemInstance
        {
            set
            {
                var defaultToggleGroup = value.GetComponentInChildren<DefaultToggleGroup>();
                foreach (var toggle in value.GetComponentsInChildren<Toggle>())
                {
                    if (toggle == defaultToggleGroup.defaultToggle)
                    {
                        m_CubeToggle = toggle;
                        toggle.onValueChanged.AddListener(isOn =>
                        {
                            if (m_BlockValueChangedListener)
                                return;

                            // m_Preferences on all instances refer
                            m_Preferences.sphereMode = !isOn;
                            foreach (var linkedObject in linkedObjects)
                            {
                                var selectionTool = (SelectionTool)linkedObject;
                                if (selectionTool != this)
                                {
                                    selectionTool.m_BlockValueChangedListener = true;

                                    //selectionTool.m_ToggleGroup.NotifyToggleOn(isOn ? m_CubeToggle : m_SphereToggle);
                                    // HACK: Toggle Group claims these toggles are not a part of the group
                                    selectionTool.m_CubeToggle.isOn = isOn;
                                    selectionTool.m_SphereToggle.isOn = !isOn;
                                    selectionTool.m_BlockValueChangedListener = false;
                                }
                            }
                        });
                    }
                    else
                    {
                        m_SphereToggle = toggle;
                    }
                }
            }
        }

        // Local method use only -- created here to reduce garbage collection
        static readonly Dictionary<Transform, GameObject> k_TempHovers = new Dictionary<Transform, GameObject>();

        void Start()
        {
            if (this.IsSharedUpdater(this) && m_Preferences == null)
            {
                m_Preferences = new Preferences();

                // Share one preferences object across all instances
                foreach (var linkedObject in linkedObjects)
                {
                    ((SelectionTool)linkedObject).m_Preferences = m_Preferences;
                }
            }

            m_NormalRayColor = this.GetDefaultRayColor(rayOrigin);
            m_MultiselectRayColor = m_NormalRayColor;
            m_MultiselectRayColor = MaterialUtils.HueShift(m_MultiselectRayColor, k_MultiselectHueShift);

            tooltipTarget = ObjectUtils.CreateEmptyGameObject("SelectionTool Tooltip Target", rayOrigin).transform;
            tooltipTarget.localPosition = this.GetDeviceType() == DeviceType.Oculus ? k_TouchTooltipPosition : k_ViveTooltipPosition;
            tooltipTarget.localRotation = k_TooltipRotation;

            m_BlockSelectCube = ObjectUtils.Instantiate(m_BlockSelectCube, transform);
            m_BlockSelectCube.SetActive(false);
            m_BlockSelectCubeRenderer = m_BlockSelectCube.GetComponent<Renderer>();

            m_BlockSelectSphere = ObjectUtils.Instantiate(m_BlockSelectSphere, transform);
            m_BlockSelectSphere.SetActive(false);

            InputUtils.GetBindingDictionaryFromActionMap(m_ActionMap, m_Controls);
        }

        void OnDestroy()
        {
            ObjectUtils.Destroy(m_BlockSelectCube);
            this.ClearFeedbackRequests();
        }

        public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
        {
            if (this.GetManipulatorDragState())
                return;

            m_SelectionInput = (SelectionInput)input;

            var multiSelectControl = m_SelectionInput.multiSelect;
            if (this.GetDeviceType() == DeviceType.Vive)
                multiSelectControl = m_SelectionInput.multiSelectAlt;

            if (multiSelectControl.wasJustPressed)
            {
                var realTime = Time.realtimeSinceStartup;
                if (UIUtils.IsDoubleClick(realTime - m_LastMultiSelectClickTime))
                {
                    foreach (var linkedObject in linkedObjects)
                    {
                        var selectionTool = (SelectionTool)linkedObject;
                        selectionTool.m_MultiSelect = !selectionTool.m_MultiSelect;
                        this.HideTooltip(selectionTool);
                    }

                    if (m_MultiSelect)
                        this.ShowTooltip(this);
                }

                m_LastMultiSelectClickTime = realTime;
            }

            this.SetDefaultRayColor(rayOrigin, m_MultiSelect ? m_MultiselectRayColor : m_NormalRayColor);

            if (this.IsSharedUpdater(this))
            {
                this.SetManipulatorsVisible(this, !m_MultiSelect);

                m_SelectionHoverGameObjects.Clear();
                foreach (var linkedObject in linkedObjects)
                {
                    var selectionTool = (SelectionTool)linkedObject;
                    selectionTool.m_HasDirectHover = false; // Clear old hover state

                    if (selectionTool.m_BlockSelect)
                        continue;

                    if (!selectionTool.IsRayActive())
                        continue;

                    var selectionRayOrigin = selectionTool.rayOrigin;

                    var hover = this.GetFirstGameObject(selectionRayOrigin);

                    if (!selectionTool.GetSelectionCandidate(ref hover))
                        continue;

                    if (hover)
                    {
                        GameObject lastHover;
                        if (m_HoverGameObjects.TryGetValue(selectionRayOrigin, out lastHover) && lastHover != hover)
                            this.SetHighlight(lastHover, false, selectionRayOrigin);

                        m_SelectionHoverGameObjects[selectionRayOrigin] = hover;
                        m_HoverGameObjects[selectionRayOrigin] = hover;
                    }
                }

                var directSelection = this.GetDirectSelection();

                // Unset highlight old hovers
                k_TempHovers.Clear();
                foreach (var kvp in m_HoverGameObjects)
                {
                    k_TempHovers[kvp.Key] = kvp.Value;
                }

                foreach (var kvp in k_TempHovers)
                {
                    var directRayOrigin = kvp.Key;
                    var hover = kvp.Value;

                    if (!directSelection.ContainsKey(directRayOrigin)
                        && !m_SelectionHoverGameObjects.ContainsKey(directRayOrigin))
                    {
                        this.SetHighlight(hover, false, directRayOrigin);
                        m_HoverGameObjects.Remove(directRayOrigin);
                    }
                }

                // Find new hovers
                foreach (var kvp in directSelection)
                {
                    var directRayOrigin = kvp.Key;
                    var directHoveredObject = kvp.Value;

                    var directSelectionCandidate = this.GetSelectionCandidate(directHoveredObject, true);

                    // Can't select this object (it might be locked or static)
                    if (directHoveredObject && !directSelectionCandidate)
                    {
                        if (directHoveredObject != null)
                            this.SetHighlight(directHoveredObject, false);

                        continue;
                    }

                    if (directSelectionCandidate)
                        directHoveredObject = directSelectionCandidate;

                    if (!this.CanGrabObject(directHoveredObject, directRayOrigin))
                        continue;

                    var grabbingNode = this.RequestNodeFromRayOrigin(directRayOrigin);
                    var selectionTool = linkedObjects.Cast<SelectionTool>().FirstOrDefault(linkedObject => linkedObject.node == grabbingNode);
                    if (selectionTool == null)
                        continue;

                    if (selectionTool.m_BlockSelect)
                        continue;

                    GameObject lastHover;
                    if (m_HoverGameObjects.TryGetValue(directRayOrigin, out lastHover) && lastHover != directHoveredObject)
                        this.SetHighlight(lastHover, false, directRayOrigin);

                    if (!selectionTool.IsDirectActive())
                    {
                        m_HoverGameObjects.Remove(directRayOrigin);
                        this.SetHighlight(directHoveredObject, false, directRayOrigin);
                        continue;
                    }

                    // Only overwrite an existing selection if it does not contain the hovered object
                    // In the case of multi-select, only add, do not remove
                    if (selectionTool.m_SelectionInput.select.wasJustPressed && !Selection.objects.Contains(directHoveredObject))
                        this.SelectObject(directHoveredObject, directRayOrigin, m_MultiSelect);

                    m_HoverGameObjects[directRayOrigin] = directHoveredObject;
                    selectionTool.m_HasDirectHover = true;
                }

                // Set highlight on new hovers
                foreach (var hover in m_HoverGameObjects)
                {
                    this.SetHighlight(hover.Value, true, hover.Key);
                }
            }

            if (!m_HasDirectHover)
                HideDirectSelectFeedback();
            else if (m_DirectSelectFeedback.Count == 0)
                ShowDirectSelectFeedback();

            GameObject hoveredObject = null;
            var rayActive = IsRayActive();
            if (rayActive)
            {
                // Need to call GetFirstGameObject a second time because we do not guarantee shared updater executes first
                hoveredObject = this.GetFirstGameObject(rayOrigin);

                if (hovered != null)
                    hovered(hoveredObject, rayOrigin);

                GetSelectionCandidate(ref hoveredObject);

                if (hoveredObject && this.GetVRPlayerObjects().Contains(hoveredObject))
                    hoveredObject = null;
            }

            if (!hoveredObject)
                HideSelectFeedback();
            else if (m_SelectFeedback.Count == 0)
                ShowSelectFeedback();

            var pointerPosition = this.GetPointerPosition(rayOrigin);

            // Capture object on press
            var select = m_SelectionInput.select;
            if (select.wasJustPressed)
            {
                m_SelectStartPosition = pointerPosition;

                // Ray selection only if ray is visible
                m_PressedObject = hoveredObject;
            }

            if (select.isHeld)
            {
                var startToEnd = pointerPosition - m_SelectStartPosition;
                var visuals = m_Preferences.sphereMode ? m_BlockSelectSphere : m_BlockSelectCube;
                var distance = startToEnd.magnitude;
                if (!m_BlockSelect && distance > k_BLockSelectDragThreshold * this.GetViewerScale())
                {
                    m_BlockSelect = true;
                    visuals.SetActive(true);

                    m_PressedObject = null;
                    this.AddRayVisibilitySettings(rayOrigin, this, false, true);
                    this.SetUIBlockedForRayOrigin(rayOrigin, true);
                }

                if (m_BlockSelect)
                    this.SetManipulatorsVisible(this, false);

                //TODO: use hashsets to only unset highlights for removed objects
                foreach (var hover in m_BlockSelectHoverGameObjects)
                {
                    this.SetHighlight(hover, false, rayOrigin);
                }
                m_BlockSelectHoverGameObjects.Clear();

                var visualsTransform = visuals.transform;
                if (m_Preferences.sphereMode)
                {
                    visualsTransform.localScale = Vector3.one * distance * 2;
                    visualsTransform.position = m_SelectStartPosition;
                    this.CheckSphere(m_SelectStartPosition, distance, m_BlockSelectHoverGameObjects, ignoreList);
                }
                else
                {
                    visualsTransform.localScale = startToEnd;
                    visualsTransform.position = m_SelectStartPosition + startToEnd * 0.5f;
                    this.CheckBounds(m_BlockSelectCubeRenderer.bounds, m_BlockSelectHoverGameObjects, ignoreList);
                }

                foreach (var hover in m_BlockSelectHoverGameObjects)
                {
                    this.SetHighlight(hover, true, rayOrigin);
                }

                if (m_BlockSelect)
                    consumeControl(select);
            }

            // Make selection on release
            if (select.wasJustReleased)
            {
                if (m_BlockSelect)
                {
                    if (!m_MultiSelect)
                    {
                        this.SetManipulatorsVisible(this, true);
                        Selection.activeGameObject = null;
                    }

                    this.SelectObjects(m_BlockSelectHoverGameObjects, rayOrigin, m_MultiSelect);

                    foreach (var hover in m_BlockSelectHoverGameObjects)
                    {
                        if (hover != null)
                            this.SetHighlight(hover, false, rayOrigin);
                    }

                    this.ResetDirectSelectionState();
                }
                else if (rayActive)
                {
                    if (m_PressedObject == hoveredObject)
                    {
                        this.SelectObject(m_PressedObject, rayOrigin, m_MultiSelect, true);
                        this.ResetDirectSelectionState();

                        if (m_PressedObject != null)
                            this.SetHighlight(m_PressedObject, false, rayOrigin);
                    }

                    if (m_PressedObject)
                        consumeControl(select);
                }

                this.SetUIBlockedForRayOrigin(rayOrigin, false);
                this.RemoveRayVisibilitySettings(rayOrigin, this);
                (m_Preferences.sphereMode ? m_BlockSelectSphere : m_BlockSelectCube).SetActive(false);
                m_PressedObject = null;
                m_BlockSelect = false;
            }
        }

        bool GetSelectionCandidate(ref GameObject hoveredObject)
        {
            var selectionCandidate = this.GetSelectionCandidate(hoveredObject, true);

            // Can't select this object (it might be locked or static)
            if (hoveredObject && !selectionCandidate)
            {
                if (hoveredObject != null)
                    this.SetHighlight(hoveredObject, false);

                return false;
            }

            if (selectionCandidate)
                hoveredObject = selectionCandidate;

            return true;
        }

        bool IsDirectActive()
        {
            if (rayOrigin == null)
                return false;

            if (!this.IsConeVisible(rayOrigin))
                return false;

            if (this.IsInMiniWorld(rayOrigin))
                return true;

            if (this.IsMainMenuVisible(rayOrigin))
                return false;

            return true;
        }

        bool IsRayActive()
        {
            if (rayOrigin == null)
                return false;

            if (this.IsHoveringOverUI(rayOrigin))
                return false;

            if (this.IsMainMenuVisible(rayOrigin))
                return false;

            if (this.IsInMiniWorld(rayOrigin))
                return false;

            if (!this.IsRayVisible(rayOrigin))
                return false;

            return true;
        }

        void OnDisable()
        {
            foreach (var kvp in m_HoverGameObjects)
            {
                this.SetHighlight(kvp.Value, false, kvp.Key);
            }
            m_HoverGameObjects.Clear();
        }

        public void OnResetDirectSelectionState() { }

        public object OnSerializePreferences()
        {
            if (this.IsSharedUpdater(this))
            {
                // Share one preferences object across all instances
                foreach (var linkedObject in linkedObjects)
                {
                    ((SelectionTool)linkedObject).m_Preferences = m_Preferences;
                }

                return m_Preferences;
            }

            return null;
        }

        public void OnDeserializePreferences(object obj)
        {
            if (this.IsSharedUpdater(this))
            {
                var preferences = obj as Preferences;
                if (preferences != null)
                    m_Preferences = preferences;

                // Share one preferences object across all instances
                foreach (var linkedObject in linkedObjects)
                {
                    ((SelectionTool)linkedObject).m_Preferences = m_Preferences;
                }

                m_SphereToggle.isOn = m_Preferences.sphereMode;
                m_CubeToggle.isOn = !m_Preferences.sphereMode;
            }
        }

        void ShowFeedback(List<ProxyFeedbackRequest> requests, string controlName, string tooltipText = null)
        {
            if (tooltipText == null)
                tooltipText = controlName;

            List<VRInputDevice.VRControl> ids;
            if (m_Controls.TryGetValue(controlName, out ids))
            {
                foreach (var id in ids)
                {
                    var request = (ProxyFeedbackRequest)this.GetFeedbackRequestObject(typeof(ProxyFeedbackRequest));
                    request.node = node;
                    request.control = id;
                    request.tooltipText = tooltipText;
                    requests.Add(request);
                    this.AddFeedbackRequest(request);
                }
            }
        }

        void ShowSelectFeedback()
        {
            ShowFeedback(m_SelectFeedback, "Select");
        }

        void ShowDirectSelectFeedback()
        {
            ShowFeedback(m_DirectSelectFeedback, "Select", "Direct Select");
        }

        void HideFeedback(List<ProxyFeedbackRequest> requests)
        {
            foreach (var request in requests)
            {
                this.RemoveFeedbackRequest(request);
            }
            requests.Clear();
        }

        void HideSelectFeedback()
        {
            HideFeedback(m_SelectFeedback);
        }

        void HideDirectSelectFeedback()
        {
            HideFeedback(m_DirectSelectFeedback);
        }
    }
}


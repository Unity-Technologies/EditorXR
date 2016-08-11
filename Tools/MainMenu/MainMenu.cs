using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.InputNew;
using UnityEngine.UI;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Tools
{
    [ExecuteInEditMode]
    public class MainMenu : MonoBehaviour, IMainMenu, IRay
    {
        private enum RotationState
        {
            AtRest,
            Rotating,
            Snapping
        }

        private enum VisibilityState
        {
            Hidden,
            Visible,
            TransitioningIn,
            TransitioningOut
        }
        
        [SerializeField]
        private MainMenuButton m_ButtonTemplatePrefab;
        [SerializeField]
        private Transform m_InputArrowLeft;
        [SerializeField]
        private Transform m_InputArrowRight;
        [SerializeField]
        private MeshRenderer m_InputHighlightLeft;
        [SerializeField]
        private MeshRenderer m_InputHighlightRight;
        [SerializeField]
        private MeshRenderer m_InputOuterBorder;
        [SerializeField]
        private ColorScheme m_MenuColorScheme;
        [SerializeField]
        private Transform[] m_MenuFaceContainers;
        [SerializeField]
        private Transform m_MenuFacePositionTarget;
        [SerializeField]
        private MainMenuFace m_MenuFacePrefab;
        [SerializeField]
        private Transform m_MenuFaceRotationOrigin;
        [SerializeField]
        private SkinnedMeshRenderer m_MenuFrameRenderer;
        [SerializeField]
        private Transform m_MenuInputVisuals;
        
        private Material m_InputHighlightLeftMaterial;
        private Material m_InputHighlightRightMaterial;
        private Material m_InputOuterBorderMaterial;
        private Menu m_MenuActionInput;
        private List<MainMenuFace> m_MenuFaces;
        private Material m_MenuFacesMaterial;
        private Color m_MenuFacesColor;
        private Dictionary<string, List<Transform>> m_MenuFaceToButtons;
        private Menu m_MenuInput;
        private Transform m_MenuInputOrigin;
        private Vector3 m_MenuInputOriginOriginalLocalScale;
        private ActionMap m_MenuMap;
        private Transform m_MenuOrigin;
        private float m_RotationRate;
        private RotationState m_RotationState;
        private Transform m_Transform;
        private VisibilityState m_VisibilityState;

        private readonly float kRotationRateMax = 200f;
        private readonly string kUncategorizedFaceName = "Uncategorized";
        private readonly string kInputHighlightColorProperty = "_Color";
        private readonly string kInputHighlightTopProperty = "_ColorTop";
        private readonly string kInputHighlightBottomProperty = "_ColorBottom";
        private readonly Color kMenuFacesHiddenColor = new Color(1f, 1f, 1f, 0.5f);

        private static readonly int s_FaceCount = 4;
        
        public Camera eventCamera { get; set; }
        public Menu menuActionInput { get { return m_MenuActionInput; } set { m_MenuActionInput = value; } }
        public Transform menuInputOrigin
        {
            get { return m_MenuInputOrigin; }
            set
            {
                m_MenuInputOrigin = value;
                m_MenuInputVisuals.SetParent(m_MenuInputOrigin);
                m_MenuInputVisuals.localPosition = Vector3.zero;
                m_MenuInputVisuals.localRotation = Quaternion.identity;
                m_MenuInputVisuals.localScale = Vector3.one;
                m_MenuInputOriginOriginalLocalScale = menuInputOrigin.localScale;
            }
        }
        public Transform menuOrigin
        {
            get { return m_MenuOrigin; }
            set
            {
                m_MenuOrigin = value;
                m_Transform.SetParent(menuOrigin);
                m_Transform.localPosition = Vector3.zero;
                m_Transform.localRotation = Quaternion.identity;
                m_Transform.localScale = Vector3.one;
            }
        }

        public List<Type> menuTools { private get; set; }
        public Transform rayOrigin { get; set; }
        public Func<IMainMenu, Type, bool> selectTool { private get; set; }

        private void Awake()
        {
            Assert.IsNotNull(m_ButtonTemplatePrefab, "m_ButtonTemplatePrefab is not assigned!");
            Assert.IsNotNull(m_InputArrowLeft, "m_InputArrowLeft is not assigned!");
            Assert.IsNotNull(m_InputArrowRight, "m_InputArrowRight is not assigned!");
            Assert.IsNotNull(m_InputHighlightLeft, "m_InputHighlightLeft is not assigned!");
            Assert.IsNotNull(m_InputHighlightRight, "m_InputHighlightRight is not assigned!");
            Assert.IsNotNull(m_InputOuterBorder, "m_InputOuterBorder is not assigned!");
            Assert.IsNotNull(m_MenuColorScheme, "m_MenuColorScheme is not assigned!");
            Assert.IsNotNull(m_MenuFaceContainers, "m_MenuFaceContainers is not assigned!");
            Assert.IsNotNull(m_MenuFacePositionTarget, "m_MenuFacePositionTarget is not assigned!");
            Assert.IsNotNull(m_MenuFacePrefab, "m_MenuFacePrefab is not assigned!");
            Assert.IsNotNull(m_MenuFaceRotationOrigin, "m_MenuFaceRotationOrigin is not assigned!");
            Assert.IsNotNull(m_MenuFrameRenderer, "m_MenuFrameRenderer is not assigned!");
            Assert.IsNotNull(m_MenuInputVisuals, "m_MenuInputVisuals is not assigned!");

            name = "MainMenu";
            m_Transform = transform;
            m_InputOuterBorderMaterial = m_InputOuterBorder.material;
            m_InputOuterBorderMaterial.SetColor(kInputHighlightTopProperty, Color.black);
            m_InputOuterBorderMaterial.SetColor(kInputHighlightBottomProperty, m_MenuColorScheme.gradientPairs[0].ColorB);
            m_InputHighlightLeftMaterial = m_InputHighlightLeft.material;
            m_InputHighlightRightMaterial = m_InputHighlightRight.material;
            m_InputHighlightLeftMaterial.SetColor(kInputHighlightColorProperty, Color.clear);
            m_InputHighlightRightMaterial.SetColor(kInputHighlightColorProperty, Color.clear);
            m_MenuFacesMaterial = m_MenuFaceRotationOrigin.GetComponent<MeshRenderer>().material;
            m_MenuFacesColor = m_MenuFacesMaterial.color;
        }

        private void Start()
        {
            m_MenuFaces = new List<MainMenuFace>();
            for (int faceCount = 0; faceCount < s_FaceCount; ++faceCount)
            {
                var faceTransform = Utilities.U.Object.InstantiateAndSetActive(m_MenuFacePrefab.gameObject).transform;
                faceTransform.SetParent(m_MenuFaceContainers[faceCount]);
                faceTransform.localRotation = Quaternion.identity;
                faceTransform.localScale = Vector3.one;
                faceTransform.localPosition = Vector3.zero;
                var face = faceTransform.GetComponent<MainMenuFace>();
                m_MenuFaces.Add(face);
            }

            foreach (Canvas canvas in m_Transform.GetComponentsInChildren<Canvas>())
                canvas.worldCamera = eventCamera;
            
            if (menuTools != null && menuTools.Any())
                CreateToolButtons();
            else
                Debug.LogError("Menu Tools was not found in the project. Could not create menu contents!");

            menuOrigin.localScale = Vector3.zero;
            menuInputOrigin.localScale = Vector3.zero;
        }

        private void Update()
        {
            if (m_MenuActionInput == null)
                return;
            
            if (m_VisibilityState == VisibilityState.TransitioningIn || m_VisibilityState == VisibilityState.TransitioningOut)
                return;

            if (m_MenuActionInput.show.wasJustPressed)
            {
                switch (m_VisibilityState)
                {
                    case VisibilityState.Hidden:
                        StartCoroutine(AnimateShow());
                        return;
                    case VisibilityState.Visible:
                        StartCoroutine(AnimateHide());
                        return;
                    case VisibilityState.TransitioningIn: // TODO: remove, the return happens before this switch
                    case VisibilityState.TransitioningOut:
                        return;
                }
            }

            if (m_VisibilityState == VisibilityState.Hidden)
                return;

            if (!Mathf.Approximately(0f, m_MenuActionInput.rotate.rawValue))
            {
                if (m_RotationState != RotationState.Rotating)
                {
                    foreach (var face in m_MenuFaces)
                        face.BeginRotationVisuals();

                    m_RotationState = RotationState.Rotating;

                    StartCoroutine(AnimateFrameRotationShapeChange(RotationState.Rotating));
                }

                float direction = m_MenuActionInput.rotate.rawValue > 0 ? -1 : 1;
                m_RotationRate = m_RotationRate < kRotationRateMax ? m_RotationRate += Time.unscaledDeltaTime * 250 : kRotationRateMax;
                m_MenuFaceRotationOrigin.Rotate(Vector3.up, direction * m_RotationRate * Time.unscaledDeltaTime);
                m_InputHighlightLeftMaterial.SetColor(kInputHighlightColorProperty, direction == -1 ? Color.white: Color.clear);
                m_InputHighlightRightMaterial.SetColor(kInputHighlightColorProperty, direction == 1 ? Color.white: Color.clear);
            }
            else {
                m_InputHighlightLeftMaterial.SetColor(kInputHighlightColorProperty, Color.clear);
                m_InputHighlightRightMaterial.SetColor(kInputHighlightColorProperty, Color.clear);
                m_RotationRate = m_RotationRate > 0 ? m_RotationRate -= Time.unscaledDeltaTime : 0f;

                if (m_RotationState == RotationState.Rotating)
                    StartCoroutine(SnapToFace());
            }
        }

        public void Disable()
        {
            StartCoroutine(AnimateHide());
        }

        public void Enable()
        {
            StartCoroutine(AnimateShow());
        }

        private void OnDestroy()
        {
            foreach (var face in m_MenuFaces)
                GameObject.DestroyImmediate(face.gameObject);
        }

        private void CreateToolButtons()
        {
            m_MenuFaceToButtons = new Dictionary<string, List<Transform>>();
            List<Transform> uncategorizedTransforms = new List<Transform>();
            m_MenuFaceToButtons.Add(kUncategorizedFaceName, uncategorizedTransforms);

            List<MainMenuButton> buttons = new List<MainMenuButton>();

            foreach (var menuTool in menuTools)
            {
                if (menuTool != null)
                {
                    var newButton = U.Object.InstantiateAndSetActive(m_ButtonTemplatePrefab.gameObject);
                    newButton.name = menuTool.Name;
                    MainMenuButton mainMenuButton = newButton.GetComponent<MainMenuButton>();
                    mainMenuButton.SetData(menuTool.Name, "Demo description text here");
                    buttons.Add(mainMenuButton);
                    AddButtonListener(mainMenuButton.button, menuTool);

                    var customMenuAttribute = (VRMenuItemAttribute)menuTool.GetCustomAttributes(typeof(VRMenuItemAttribute), false).FirstOrDefault();
                    if (customMenuAttribute != null)
                    {
                        var found = m_MenuFaceToButtons.Where(x => x.Key == customMenuAttribute.SectionName).Any();

                        if (found)
                        {
                            var kvp = m_MenuFaceToButtons.Where(x => x.Key == customMenuAttribute.SectionName).First();
                            if (!String.IsNullOrEmpty(kvp.Key))
                                kvp.Value.Add(newButton.transform);
                        }
                        else
                        {
                            List<Transform> buttonTransforms = new List<Transform>();
                            buttonTransforms.Add(newButton.transform);
                            m_MenuFaceToButtons.Add(customMenuAttribute.SectionName, buttonTransforms);
                        }
                    }
                    else
                        uncategorizedTransforms.Add(newButton.transform);
                }
                else
                    Debug.LogError("Null menuTool found when creating Menu's tool buttons!");
            }

            SetupMenuFaces();
        }

        private void SetupMenuFaces()
        {
            int position = 0;
            foreach (var faceNameToButtons in m_MenuFaceToButtons)
            {
                // TODO: Add support for 5+ menu faces (swapping face content, cycling through the gradients, etc)
                m_MenuFaces[position].SetFaceData(faceNameToButtons.Key, faceNameToButtons.Value, m_MenuColorScheme.gradientPairs[position]);
                ++position;
            }
        }

        private void AddButtonListener(Button b, Type t)
        {
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() =>
            {
                if (m_VisibilityState == VisibilityState.Visible && selectTool(this, t))
                    StartCoroutine(AnimateHide(3f)); // TODO: hiding should only occur in proper context
            });
            b.onClick.AddListener(OnButtonClicked);
            b.onClick.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
        }

        private void OnButtonClicked()
        {
            // perform a disabling of the menu for a short period of time, preventing input for a short duration, and handling any additional actions that should occur when a button is clicked
        }

        private IEnumerator SnapToFace()
        {
            // when the user releases their input while rotating the menu, snap to the nearest face
            m_RotationState = RotationState.Snapping;
            StartCoroutine(AnimateFrameRotationShapeChange(m_RotationState));

            foreach (var face in m_MenuFaces)
                face.EndRotationVisuals();
            
            m_RotationRate = 0f;

            // Snap if not already aligned
            if (Mathf.Abs(m_MenuFaceRotationOrigin.localRotation.eulerAngles.y % 90) > 1f)
            {
                float smoothTransitionIntoSnap = 0f;
                const float kTargetSnapSpeed = 3f;
                const float kTargetSnapThreshold = 0.0005f;
                const float kEaseStepping = 1f;

                Vector3 roundedRotation = m_MenuFaceRotationOrigin.localRotation.eulerAngles;
                roundedRotation.y = Mathf.Round(roundedRotation.y / 90) * 90;
                
                while (m_RotationState == RotationState.Snapping && Mathf.Abs(m_MenuFaceRotationOrigin.localRotation.eulerAngles.y - roundedRotation.y) > 1f)
                {
                    smoothTransitionIntoSnap = U.Math.Ease(smoothTransitionIntoSnap, kTargetSnapSpeed, kEaseStepping, kTargetSnapThreshold);
                    float angle = Mathf.LerpAngle(m_MenuFaceRotationOrigin.localRotation.eulerAngles.y, roundedRotation.y, Time.unscaledDeltaTime * smoothTransitionIntoSnap);
                    m_MenuFaceRotationOrigin.localRotation = Quaternion.Euler(new Vector3(0, angle, 0));
                    yield return null;
                }
            }

            m_RotationState = RotationState.AtRest;
            m_RotationRate = 0f;
        }

        private IEnumerator AnimateShow()
        {
            m_VisibilityState = VisibilityState.TransitioningIn;

            foreach (var face in m_MenuFaces)
                face.ShowContent();

            StartCoroutine(AnimateFrameReveal());

            const float kTargetScale = 1f;
            const float kTargetSnapThreshold = 0.0005f;
            const float kEaseStepping = 2f;

            float scale = 0f;

            while (m_VisibilityState == VisibilityState.TransitioningIn && scale < 1)
            {
                menuOrigin.localScale = Vector3.one * scale;
                menuInputOrigin.localScale = m_MenuInputOriginOriginalLocalScale * scale;
                scale = U.Math.Ease(scale, kTargetScale, kEaseStepping, kTargetSnapThreshold);
                yield return null;
            }

            if (m_VisibilityState == VisibilityState.TransitioningIn)
            {
                m_VisibilityState = VisibilityState.Visible;
                menuOrigin.localScale = Vector3.one;
                menuInputOrigin.localScale = m_MenuInputOriginOriginalLocalScale;
            }
        }

        private IEnumerator AnimateHide(float restartPauseDuration = 0f)
        {
            m_VisibilityState = VisibilityState.TransitioningOut;

            foreach (var face in m_MenuFaces)
                face.HideContent();

            StartCoroutine(AnimateFrameReveal(m_VisibilityState));

            const float kTargetScale = 0f;
            const float kTargetSnapThreshold = 0.0005f;
            const float kEaseStepping = 1.1f;
            float scale = menuOrigin.localScale.x;

            while (m_VisibilityState == VisibilityState.TransitioningOut && scale > 0.0001f)
            {
                menuOrigin.localScale = Vector3.one * scale;
                menuInputOrigin.localScale = m_MenuInputOriginOriginalLocalScale * scale;
                scale = U.Math.Ease(scale, kTargetScale, kEaseStepping, kTargetSnapThreshold);
                yield return null;
            }

            if (m_VisibilityState == VisibilityState.TransitioningOut)
            {
                m_VisibilityState = VisibilityState.Hidden;
                menuOrigin.localScale = Vector3.zero;
                menuInputOrigin.localScale = Vector3.zero;

                if (restartPauseDuration > 0)
                {
                    yield return new WaitForSeconds(restartPauseDuration);
                    StartCoroutine(AnimateShow());
                }
            }
        }

        private IEnumerator AnimateFrameRotationShapeChange(RotationState rotationState)
        {
            float easeDivider = rotationState == RotationState.Rotating ? 8f : 6f;
            float currentBlendShapeWeight = m_MenuFrameRenderer.GetBlendShapeWeight(0);
            float targetWeight = rotationState == RotationState.Rotating ? 100f : 0f;
            const float kSnapValue = 0.001f;
            while (m_RotationState == rotationState && !Mathf.Approximately(currentBlendShapeWeight, targetWeight))
            {
                currentBlendShapeWeight = U.Math.Ease(currentBlendShapeWeight, targetWeight, easeDivider, kSnapValue);
                m_MenuFrameRenderer.SetBlendShapeWeight(0, currentBlendShapeWeight);
                yield return null;
            }

            if (m_RotationState == rotationState)
                m_MenuFrameRenderer.SetBlendShapeWeight(0, targetWeight);
        }

        private IEnumerator AnimateFrameReveal(VisibilityState visibiityState = VisibilityState.TransitioningIn)
        {
            m_MenuFrameRenderer.SetBlendShapeWeight(1, 100f);
            float easeDivider = visibiityState == VisibilityState.TransitioningIn ? 3f : 1.5f;
            const float zeroStartBlendShapePadding = 20f;
            float currentBlendShapeWeight = m_MenuFrameRenderer.GetBlendShapeWeight(1);
            currentBlendShapeWeight = currentBlendShapeWeight > 0 ? currentBlendShapeWeight : zeroStartBlendShapePadding;
            float targetWeight = visibiityState == VisibilityState.TransitioningIn ? 0f : 100f;
            const float kSnapValue = 0.001f;
            while (m_VisibilityState != VisibilityState.Hidden && !Mathf.Approximately(currentBlendShapeWeight, targetWeight))
            {
                currentBlendShapeWeight = U.Math.Ease(currentBlendShapeWeight, targetWeight, easeDivider, kSnapValue);
                m_MenuFrameRenderer.SetBlendShapeWeight(1, currentBlendShapeWeight * currentBlendShapeWeight);
                m_MenuFacesMaterial.color = Color.Lerp(m_MenuFacesColor, kMenuFacesHiddenColor, currentBlendShapeWeight * 0.25f);
                yield return null;
            }

            if (m_VisibilityState == visibiityState)
            {
                m_MenuFrameRenderer.SetBlendShapeWeight(1, targetWeight);
                m_MenuFacesMaterial.color = targetWeight > 0 ? m_MenuFacesColor : kMenuFacesHiddenColor;
            }
        }
    }

    #region Attribute
    /// <summary>
    /// Attribute used to tag items (tools, actions, etc) that can be added to menus
    /// </summary>
    public class VRMenuItemAttribute : System.Attribute
    {
        public string SectionName;
        public string Description;

        public VRMenuItemAttribute(string sectionName, string description)
        {
            SectionName = sectionName;
            Description = description;
        }
    }
    #endregion
}
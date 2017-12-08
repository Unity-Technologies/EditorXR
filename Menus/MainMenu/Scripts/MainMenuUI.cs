#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    sealed class MainMenuUI : MonoBehaviour, IInstantiateUI, IConnectInterfaces, IRayEnterHandler, IRayExitHandler
    {
        public class ButtonData
        {
            public string name { get; private set; }
            public string sectionName { get; set; }
            public string description { get; set; }

            public ButtonData(string name)
            {
                this.name = name.Replace("Tool", string.Empty).Replace("Module", string.Empty)
                    .Replace("Workspace", string.Empty);
            }
        }

        enum RotationState
        {
            AtRest,
            Snapping,
        }

        enum VisibilityState
        {
            Hidden,
            Visible,
            TransitioningIn,
            TransitioningOut
        }

        const float k_FaceRotationSnapAngle = 90f;
        const float k_DefaultSnapSpeed = 10f;
        const float k_RotationEpsilon = 1f;
        const int k_FaceCount = 4;

        readonly string k_UncategorizedFaceName = "Uncategorized";
        readonly Color k_MenuFacesHiddenColor = new Color(1f, 1f, 1f, 0.5f);

        [SerializeField]
        MainMenuButton m_ButtonTemplatePrefab;

        [SerializeField]
        Transform[] m_MenuFaceContainers;

        [SerializeField]
        MainMenuFace m_MenuFacePrefab;

        [SerializeField]
        Transform m_MenuFaceRotationOrigin;

        [SerializeField]
        SkinnedMeshRenderer m_MenuFrameRenderer;

        [SerializeField]
        Transform m_AlternateMenu;

        int m_TargetFaceIndex;

        readonly Dictionary<string, MainMenuFace> m_Faces = new Dictionary<string, MainMenuFace>();
        readonly  List<Material> m_MaterialsToCleanup = new List<Material>();

        VisibilityState m_VisibilityState = VisibilityState.Hidden;
        RotationState m_RotationState;
        Material m_MenuFacesMaterial;
        Color m_MenuFacesColor;
        Transform m_MenuOrigin;
        Transform m_AlternateMenuOrigin;
        Vector3 m_AlternateMenuOriginOriginalLocalScale;
        Coroutine m_VisibilityCoroutine;
        Coroutine m_FrameRevealCoroutine;
        int m_Direction;

        public int targetFaceIndex
        {
            get { return m_TargetFaceIndex; }
            set
            {
                m_Direction = (int)Mathf.Sign(value - m_TargetFaceIndex);

                // Loop around both ways
                if (value < 0)
                    value += k_FaceCount;

                m_TargetFaceIndex = value % k_FaceCount;
            }
        }

        public bool rotating { get { return m_RotationState != RotationState.AtRest; } }

        public Transform menuOrigin
        {
            get { return m_MenuOrigin; }
            set
            {
                m_MenuOrigin = value;
                transform.SetParent(menuOrigin);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
            }
        }

        public Transform alternateMenuOrigin
        {
            get { return m_AlternateMenuOrigin; }
            set
            {
                m_AlternateMenuOrigin = value;
                m_AlternateMenu.SetParent(m_AlternateMenuOrigin);
                m_AlternateMenu.localPosition = Vector3.zero;
                m_AlternateMenu.localRotation = Quaternion.identity;
                m_AlternateMenuOriginOriginalLocalScale = m_AlternateMenu.localScale;
            }
        }

        public float targetRotation { get; set; }

        public Node node { get; set; }

        public bool visible
        {
            get { return m_VisibilityState == VisibilityState.Visible || m_VisibilityState == VisibilityState.TransitioningIn; }
            set
            {
                switch (m_VisibilityState)
                {
                    case VisibilityState.TransitioningOut:
                    case VisibilityState.Hidden:
                        if (value)
                        {
                            gameObject.SetActive(true);
                            this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateShow());
                        }

                        return;
                    case VisibilityState.TransitioningIn:
                    case VisibilityState.Visible:
                        if (!value)
                            this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateHide());

                        return;
                }
            }
        }

        int currentFaceIndex
        {
            get
            {
                // Floating point can leave us close to our actual rotation, but not quite (179.3438,
                // which effectively we want to treat as 180)
                return GetActualFaceIndexForRotation(Mathf.Ceil(currentRotation));
            }
        }

        float currentRotation { get { return m_MenuFaceRotationOrigin.localRotation.eulerAngles.y; } }

        public Bounds localBounds { get; private set; }
        public bool hovering { get; private set; }

        void Awake()
        {
            m_MenuFacesMaterial = MaterialUtils.GetMaterialClone(m_MenuFaceRotationOrigin.GetComponent<MeshRenderer>());
            m_MenuFacesColor = m_MenuFacesMaterial.color;
            localBounds = ObjectUtils.GetBounds(transform);
        }

        public void Setup()
        {
            transform.localScale = Vector3.zero;
            m_AlternateMenu.localScale = Vector3.zero;
            gameObject.SetActive(false);
        }

        void Update()
        {
            switch (m_VisibilityState)
            {
                case VisibilityState.TransitioningIn:
                case VisibilityState.TransitioningOut:
                case VisibilityState.Hidden:
                    return;
            }

            // Allow any snaps to finish before resuming any other operations
            if (m_RotationState == RotationState.Snapping)
                return;

            var faceIndex = currentFaceIndex;

            if (faceIndex != targetFaceIndex)
                StartCoroutine(SnapToFace(faceIndex + m_Direction, k_DefaultSnapSpeed));
        }

        void OnDestroy()
        {
            foreach (var kvp in m_Faces)
            {
                ObjectUtils.Destroy(kvp.Value.gameObject);
            }

            foreach (var material in m_MaterialsToCleanup)
            {
                ObjectUtils.Destroy(material);
            }
        }

        public MainMenuButton CreateFaceButton(ButtonData buttonData)
        {
            var button = ObjectUtils.Instantiate(m_ButtonTemplatePrefab.gameObject);
            button.name = buttonData.name;
            var mainMenuButton = button.GetComponent<MainMenuButton>();

            if (string.IsNullOrEmpty(buttonData.sectionName))
                buttonData.sectionName = k_UncategorizedFaceName;

            mainMenuButton.SetData(buttonData.name, buttonData.description);
            this.ConnectInterfaces(mainMenuButton);

            MainMenuFace face;
            if (!m_Faces.TryGetValue(buttonData.sectionName, out face))
                face = CreateFace(buttonData.sectionName);

            if (face == null)
                return null;

            face.AddButton(button.transform);
            return mainMenuButton;
        }

        public GameObject CreateCustomButton(GameObject prefab, string sectionName)
        {
            var button = ObjectUtils.Instantiate(prefab);

            if (string.IsNullOrEmpty(sectionName))
                sectionName = k_UncategorizedFaceName;

            MainMenuFace face;
            if (!m_Faces.TryGetValue(sectionName, out face))
                face = CreateFace(sectionName);

            if (face == null)
                return null;

            face.AddButton(button.transform);

            var buttonGraphics = button.GetComponentsInChildren<Graphic>();
            if (buttonGraphics != null && buttonGraphics.Length > 0)
                SetupCustomButtonMaterials(buttonGraphics, face);

            return button;
        }

        void SetupCustomButtonMaterials(Graphic[] graphics, MainMenuFace face)
        {
            const string topGradientProperty = "_ColorTop";
            const string bottomGradientProperty = "_ColorBottom";
            Material materialClone = null;
            foreach (var graphic in graphics)
            {
                // Assign face gradient to custom buttons on a given face (Settings face: locomotion, snapping, etc)
                var material = graphic.material;
                if (material.HasProperty(topGradientProperty))
                {
                    if (materialClone == null)
                    {
                        // Only clone the material if a material with a matching property is found in this custom-button/sub-menu
                        materialClone = MaterialUtils.GetMaterialClone(graphic);
                        m_MaterialsToCleanup.Add(materialClone);
                        materialClone.SetColor(topGradientProperty, face.gradientPair.a);
                        if (materialClone.HasProperty(bottomGradientProperty))
                            materialClone.SetColor(bottomGradientProperty, face.gradientPair.b);
                    }

                    graphic.material = materialClone;
                }
            }
        }

        MainMenuFace CreateFace(string sectionName)
        {
            if (m_Faces.Count == k_FaceCount)
            {
                Debug.LogWarning("Main Menu does not support more than 4 faces");
                return null;
            }

            var faceTransform = this.InstantiateUI(m_MenuFacePrefab.gameObject).transform;
            faceTransform.name = sectionName;
            faceTransform.SetParent(m_MenuFaceContainers[m_Faces.Count]);
            faceTransform.localRotation = Quaternion.identity;
            faceTransform.localScale = Vector3.one;
            faceTransform.localPosition = Vector3.zero;
            var face = faceTransform.GetComponent<MainMenuFace>();
            m_Faces[sectionName] = face;
            face.gradientPair = UnityBrandColorScheme.GetRandomGradient();
            face.title = sectionName;

            return face;
        }

        public GameObject AddSubmenu(string sectionName, GameObject submenuPrefab)
        {
            if (submenuPrefab.GetComponent<SubmenuFace>() == null)
                return null;

            MainMenuFace face;
            if (!m_Faces.TryGetValue(sectionName, out face))
                face = CreateFace(sectionName);

            var submenu = this.InstantiateUI(submenuPrefab);

            face.AddSubmenu(submenu.transform);

            var submenuFace = submenu.GetComponent<SubmenuFace>();
            if (submenuFace)
            {
                submenuFace.SetupBackButton(face.RemoveSubmenu);
                submenuFace.gradientPair = face.gradientPair;

                var submenuGraphics = submenu.GetComponentsInChildren<Graphic>();
                if (submenuGraphics != null && submenuGraphics.Length > 0)
                    SetupCustomButtonMaterials(submenuGraphics, face);
            }

            return submenu;
        }

        static int GetClosestFaceIndexForRotation(float rotation)
        {
            return Mathf.RoundToInt(rotation / k_FaceRotationSnapAngle) % k_FaceCount;
        }

        static int GetActualFaceIndexForRotation(float rotation)
        {
            return Mathf.FloorToInt(rotation / k_FaceRotationSnapAngle) % k_FaceCount;
        }

        static float GetRotationForFaceIndex(int faceIndex)
        {
            return faceIndex * k_FaceRotationSnapAngle;
        }

        IEnumerator SnapToFace(int faceIndex, float snapSpeed)
        {
            if (m_RotationState == RotationState.Snapping)
                yield break;

            m_RotationState = RotationState.Snapping;

            // When the user releases their input while rotating the menu, snap to the nearest face
            StartCoroutine(AnimateFrameRotationShapeChange(m_RotationState));

            var rotation = currentRotation;
            var faceTargetRotation = GetRotationForFaceIndex(faceIndex);

            var smoothVelocity = 0f;
            var smoothSnapSpeed = 0.5f;
            while (Mathf.Abs(Mathf.DeltaAngle(rotation, faceTargetRotation)) > k_RotationEpsilon)
            {
                smoothSnapSpeed = MathUtilsExt.SmoothDamp(smoothSnapSpeed, snapSpeed, ref smoothVelocity, 0.0625f, Mathf.Infinity, Time.deltaTime);
                rotation = Mathf.LerpAngle(rotation, faceTargetRotation, Time.deltaTime * smoothSnapSpeed);
                m_MenuFaceRotationOrigin.localRotation = Quaternion.Euler(new Vector3(0, rotation, 0));
                yield return null;
            }
            m_MenuFaceRotationOrigin.localRotation = Quaternion.Euler(new Vector3(0, faceTargetRotation, 0));

            // Target face index and rotation can be set separately, so both, must be kept in sync
            targetRotation = faceTargetRotation;

            m_RotationState = RotationState.AtRest;
        }

        IEnumerator AnimateShow()
        {
            m_VisibilityState = VisibilityState.TransitioningIn;

            foreach (var kvp in m_Faces)
            {
                kvp.Value.visible = true;
            }

            this.RestartCoroutine(ref m_FrameRevealCoroutine, AnimateFrameReveal(m_VisibilityState));

            const float faceDelay = 0.1f;
            var count = 0;
            foreach (var face in m_Faces)
            {
                face.Value.Reveal(count++ * faceDelay);
            }

            const float kTargetScale = 1f;
            const float kSmoothTime = 0.125f;
            var scale = 0f;
            var smoothVelocity = 0f;
            var currentDuration = 0f;
            while (currentDuration < kSmoothTime)
            {
                scale = MathUtilsExt.SmoothDamp(scale, kTargetScale, ref smoothVelocity, kSmoothTime, Mathf.Infinity, Time.deltaTime);
                currentDuration += Time.deltaTime;
                transform.localScale = Vector3.one * scale;
                m_AlternateMenu.localScale = m_AlternateMenuOriginOriginalLocalScale * scale;
                yield return null;
            }

            m_VisibilityState = VisibilityState.Visible;
        }

        IEnumerator AnimateHide()
        {
            m_VisibilityState = VisibilityState.TransitioningOut;

            foreach (var kvp in m_Faces)
            {
                var face = kvp.Value;
                face.visible = false;
                face.ClearSubmenus();
            }

            this.RestartCoroutine(ref m_FrameRevealCoroutine, AnimateFrameReveal(m_VisibilityState));

            const float kTargetScale = 0f;
            const float kSmoothTime = 0.06875f;
            var scale = transform.localScale.x;
            var smoothVelocity = 0f;
            var currentDuration = 0f;
            while (currentDuration < kSmoothTime)
            {
                scale = MathUtilsExt.SmoothDamp(scale, kTargetScale, ref smoothVelocity, kSmoothTime, Mathf.Infinity, Time.deltaTime);
                currentDuration += Time.deltaTime;
                transform.localScale = Vector3.one * scale;
                m_AlternateMenu.localScale = m_AlternateMenuOriginOriginalLocalScale * scale;
                yield return null;
            }

            gameObject.SetActive(false);

            m_VisibilityState = VisibilityState.Hidden;

            var snapRotation = GetRotationForFaceIndex(GetClosestFaceIndexForRotation(currentRotation));
            m_MenuFaceRotationOrigin.localRotation = Quaternion.Euler(new Vector3(0, snapRotation, 0)); // set intended target rotation
            m_RotationState = RotationState.AtRest;
        }

        IEnumerator AnimateFrameRotationShapeChange(RotationState rotationState)
        {
            const float smoothTime = 0.0375f;
            const float targetWeight = 0f;
            var currentBlendShapeWeight = m_MenuFrameRenderer.GetBlendShapeWeight(0);
            var smoothVelocity = 0f;
            var currentDuration = 0f;
            while (m_RotationState == rotationState && currentDuration < smoothTime)
            {
                currentBlendShapeWeight = MathUtilsExt.SmoothDamp(currentBlendShapeWeight, targetWeight, ref smoothVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);
                currentDuration += Time.deltaTime;
                m_MenuFrameRenderer.SetBlendShapeWeight(0, currentBlendShapeWeight);
                yield return null;
            }

            if (m_RotationState == rotationState)
                m_MenuFrameRenderer.SetBlendShapeWeight(0, targetWeight);
        }

        IEnumerator AnimateFrameReveal(VisibilityState visibilityState)
        {
            m_MenuFrameRenderer.SetBlendShapeWeight(1, 100f);
            const float zeroStartBlendShapePadding = 20f; // start the blendShape at a point slightly above the full hidden value for better visibility
            const float kLerpEmphasisWeight = 0.25f;
            var smoothTime = visibilityState == VisibilityState.TransitioningIn ? 0.1875f : 0.09375f; // slower if transitioning in
            var currentBlendShapeWeight = m_MenuFrameRenderer.GetBlendShapeWeight(1);
            var targetWeight = visibilityState == VisibilityState.TransitioningIn ? 0f : 100f;
            var smoothVelocity = 0f;
            currentBlendShapeWeight = currentBlendShapeWeight > 0 ? currentBlendShapeWeight : zeroStartBlendShapePadding;

            var currentDuration = 0f;
            while (m_VisibilityState != VisibilityState.Hidden && currentDuration - Time.deltaTime < smoothTime)
            {
                currentBlendShapeWeight = MathUtilsExt.SmoothDamp(currentBlendShapeWeight, targetWeight, ref smoothVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);
                currentDuration += Time.deltaTime;
                m_MenuFrameRenderer.SetBlendShapeWeight(1, currentBlendShapeWeight * currentBlendShapeWeight);
                m_MenuFacesMaterial.color = Color.Lerp(m_MenuFacesColor, k_MenuFacesHiddenColor, currentBlendShapeWeight * kLerpEmphasisWeight);
                yield return null;
            }

            m_MenuFrameRenderer.SetBlendShapeWeight(1, targetWeight);
            m_MenuFacesMaterial.color = targetWeight > 0 ? m_MenuFacesColor : k_MenuFacesHiddenColor;

            if (m_VisibilityState == VisibilityState.Hidden)
            {
                m_MenuFrameRenderer.SetBlendShapeWeight(0, 0);
            }
        }

        public void OnRayEnter(RayEventData eventData)
        {
            hovering = true;
        }

        public void OnRayExit(RayEventData eventData)
        {
            hovering = false;
        }
    }
}
#endif

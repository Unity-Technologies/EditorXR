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
	public class MainMenu : MonoBehaviour, IMainMenu, ICustomRay, ILockableRay
	{
		private enum RotationState
		{
			AtRest,
			Rotating,
			Snapping,
			FlickSnapping
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

		private float m_DeltaWait;
		private Material m_InputHighlightLeftMaterial;
		private Material m_InputHighlightRightMaterial;
		private Material m_InputOuterBorderMaterial;
		private MainMenuInput m_MainMenuInput;
		private List<MainMenuFace> m_MenuFaces;
		private Material m_MenuFacesMaterial;
		private Color m_MenuFacesColor;
		private Dictionary<string, List<Transform>> m_FaceToButtons;
		private Menu m_MenuInput;
		private Transform m_MenuInputOrigin;
		private Vector3 m_MenuInputOriginOriginalLocalScale;
		private Transform m_MenuOrigin;
		private float m_RotationRate;
		private RotationState m_RotationState;
		private Transform m_Transform;
		private VisibilityState m_VisibilityState;
		private float m_InputDirection;
		private float m_InputFlickStartTime;
		private float m_InputFlickRate;
		private List<Transform> m_UncategorizedButtons;

		private const float kFaceRotationSnapAngle = 90;
		private const float kRotationRateMax = 200f;

		private static readonly int s_FaceCount = 4;

		private readonly string kUncategorizedFaceName = "Uncategorized";
		private readonly string kInputHighlightColorProperty = "_Color";
		private readonly string kInputHighlightTopProperty = "_ColorTop";
		private readonly string kInputHighlightBottomProperty = "_ColorBottom";
		private readonly Color kMenuFacesHiddenColor = new Color(1f, 1f, 1f, 0.5f);

		public MainMenuInput mainMenuInput
		{
			get { return m_MainMenuInput; }
			set { m_MainMenuInput = value; }
		}

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

		public Camera eventCamera { get; set; }
		public Action hideDefaultRay { get; set; }
		public Action showDefaultRay { get; set; }
		public Action<object> lockRay { get; set; }
		public Action<object> unlockRay { get; set; }
		public Transform rayOrigin { get; set; }
		public List<Type> menuTools { private get; set; }
		public Func<int, Type, bool> selectTool { private get; set; }
		public int tagIndex { get; set; }

		private void Awake()
		{
			Assert.IsNotNull(m_ButtonTemplatePrefab, "m_ButtonTemplatePrefab is not assigned!");
			Assert.IsNotNull(m_InputArrowLeft, "m_InputArrowLeft is not assigned!");
			Assert.IsNotNull(m_InputArrowRight, "m_InputArrowRight is not assigned!");
			Assert.IsNotNull(m_InputHighlightLeft, "m_InputHighlightLeft is not assigned!");
			Assert.IsNotNull(m_InputHighlightRight, "m_InputHighlightRight is not assigned!");
			Assert.IsNotNull(m_InputOuterBorder, "m_InputOuterBorder is not assigned!");
			Assert.IsNotNull(m_MenuFaceContainers, "m_MenuFaceContainers is not assigned!");
			Assert.IsNotNull(m_MenuFacePositionTarget, "m_MenuFacePositionTarget is not assigned!");
			Assert.IsNotNull(m_MenuFacePrefab, "m_MenuFacePrefab is not assigned!");
			Assert.IsNotNull(m_MenuFaceRotationOrigin, "m_MenuFaceRotationOrigin is not assigned!");
			Assert.IsNotNull(m_MenuFrameRenderer, "m_MenuFrameRenderer is not assigned!");
			Assert.IsNotNull(m_MenuInputVisuals, "m_MenuInputVisuals is not assigned!");

			name = "MainMenu";
			m_Transform = transform;
			m_InputOuterBorderMaterial = U.Material.GetMaterialClone(m_InputOuterBorder);
			m_InputOuterBorderMaterial.SetColor(kInputHighlightTopProperty, UnityBrandColorScheme.light);
			m_InputOuterBorderMaterial.SetColor(kInputHighlightBottomProperty, UnityBrandColorScheme.light);
			m_InputHighlightLeftMaterial = U.Material.GetMaterialClone(m_InputHighlightLeft);
			m_InputHighlightRightMaterial = U.Material.GetMaterialClone(m_InputHighlightRight);
			m_InputHighlightLeftMaterial.SetColor(kInputHighlightColorProperty, Color.clear);
			m_InputHighlightRightMaterial.SetColor(kInputHighlightColorProperty, Color.clear);
			m_MenuFacesMaterial = U.Material.GetMaterialClone(m_MenuFaceRotationOrigin.GetComponent<MeshRenderer>());
			m_MenuFacesColor = m_MenuFacesMaterial.color;
		}

		private void Start()
		{
			m_MenuFaces = new List<MainMenuFace>();
			for (int faceCount = 0; faceCount < s_FaceCount; ++faceCount)
			{
				// Add faces to the menu
				var faceTransform = U.Object.InstantiateAndSetActive(m_MenuFacePrefab.gameObject).transform;
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
				CreateToolButtons(menuTools);
			else
				Debug.LogError("Menu Tools were not found in the project. Could not create menu contents!");

			SetupMenuFaces();

			menuOrigin.localScale = Vector3.zero;
			menuInputOrigin.localScale = Vector3.zero;
		}

		private void Update()
		{
			if (m_MainMenuInput == null)
				return;

			if (m_VisibilityState == VisibilityState.TransitioningIn || m_VisibilityState == VisibilityState.TransitioningOut)
				return;

			if (m_MainMenuInput.show.wasJustPressed)
			{
				switch (m_VisibilityState)
				{
					case VisibilityState.Hidden:
						StartCoroutine(AnimateShow());
						return;
					case VisibilityState.Visible:
						StartCoroutine(AnimateHide());
						return;
				}
			}

			if (m_VisibilityState == VisibilityState.Hidden)
				return;

			if (!Mathf.Approximately(0f, m_MainMenuInput.rotate.rawValue) && m_RotationState != RotationState.FlickSnapping)
			{
				if (m_RotationState != RotationState.Rotating)
				{
					foreach (var face in m_MenuFaces)
						face.BeginRotationVisuals();

					m_RotationState = RotationState.Rotating;
					m_InputFlickStartTime = Time.realtimeSinceStartup;
					m_InputFlickRate = 0f;

					StartCoroutine(AnimateFrameRotationShapeChange(RotationState.Rotating));
				}

				const float rotationSpeed = 250;
				m_InputDirection = m_MainMenuInput.rotate.rawValue;
				m_InputFlickRate = Mathf.Max(m_InputFlickRate, Mathf.Abs(m_InputDirection)); // cache the larger x axis delta of input rotation
				m_RotationRate = m_RotationRate < kRotationRateMax ? m_RotationRate += Time.unscaledDeltaTime * rotationSpeed : kRotationRateMax;
				m_MenuFaceRotationOrigin.Rotate(Vector3.up, -m_InputDirection * m_RotationRate * Time.unscaledDeltaTime);
				m_InputHighlightLeftMaterial.SetColor(kInputHighlightColorProperty, m_InputDirection > 0 ? Color.white : Color.clear);
				m_InputHighlightRightMaterial.SetColor(kInputHighlightColorProperty, m_InputDirection < 0 ? Color.white : Color.clear);
			}
			else
			{
				m_InputHighlightLeftMaterial.SetColor(kInputHighlightColorProperty, Color.clear);
				m_InputHighlightRightMaterial.SetColor(kInputHighlightColorProperty, Color.clear);
				m_RotationRate = m_RotationRate > 0 ? m_RotationRate -= Time.unscaledDeltaTime : 0f;  // Allow for the smooth resumption of rotation if rotation is resumed before snapping is stopped

				if (m_RotationState == RotationState.Rotating) // snap to face if a manual rotation was occurring
				{
					float flickToFaceDirection = 0;
					const float kFlickDeltaThreshold = 0.5f;
					const float kFlickDurationThreshold = 0.3f;
					if (Mathf.Abs(m_InputFlickRate) > kFlickDeltaThreshold && Time.realtimeSinceStartup - m_InputFlickStartTime < kFlickDurationThreshold) // perform a quick single face rotation if a quick flick of the input axis occurred
						flickToFaceDirection = m_InputDirection;

					m_InputFlickRate = 100;
					m_InputFlickStartTime = 0;
					m_InputDirection = 0;

					StartCoroutine(SnapToFace(flickToFaceDirection));
				}
			}
		}

		private void OnDisable()
		{
			unlockRay(this);
		}

		private void OnDestroy()
		{
			foreach (var face in m_MenuFaces)
				GameObject.DestroyImmediate(face.gameObject);

			unlockRay(this);
			showDefaultRay();
		}

		private void CreateToolButtons(List<Type> buttonDataSources)
		{
			if (m_FaceToButtons == null)
			{
				m_FaceToButtons = new Dictionary<string, List<Transform>>();
				m_UncategorizedButtons = new List<Transform>();
				m_FaceToButtons.Add(kUncategorizedFaceName, m_UncategorizedButtons);
			}

			List<MainMenuButton> buttons = new List<MainMenuButton>();

			foreach (var source in buttonDataSources)
			{
				if (source != null)
				{
					var newButton = U.Object.InstantiateAndSetActive(m_ButtonTemplatePrefab.gameObject);
					newButton.name = source.Name;
					MainMenuButton mainMenuButton = newButton.GetComponent<MainMenuButton>();
					buttons.Add(mainMenuButton);
					AddButtonListener(mainMenuButton.button, source);

					var customMenuAttribute = (VRMenuItemAttribute)source.GetCustomAttributes(typeof(VRMenuItemAttribute), false).FirstOrDefault();
					if (customMenuAttribute != null)
					{
						mainMenuButton.SetData(customMenuAttribute.Name, customMenuAttribute.Description);

						var found = m_FaceToButtons.Any(x => x.Key == customMenuAttribute.SectionName);

						if (found)
						{
							var kvp = m_FaceToButtons.First(x => x.Key == customMenuAttribute.SectionName);
							if (!String.IsNullOrEmpty(kvp.Key))
								kvp.Value.Add(newButton.transform);
						}
						else
						{
							List<Transform> buttonTransforms = new List<Transform>();
							buttonTransforms.Add(newButton.transform);
							m_FaceToButtons.Add(customMenuAttribute.SectionName, buttonTransforms);
						}
					}
					else
					{
						m_UncategorizedButtons.Add(newButton.transform);
						mainMenuButton.SetData(source.Name, string.Empty);
					}
				}
				else
					Debug.LogError("Null menuTool found when creating Menu's tool buttons!");
			}
		}

		private void SetupMenuFaces()
		{
			int position = 0;
			foreach (var faceNameToButtons in m_FaceToButtons)
			{
				m_MenuFaces[position].SetFaceData(faceNameToButtons.Key, faceNameToButtons.Value, UnityBrandColorScheme.GetRandomGradient());
				++position;
			}
		}

		private void AddButtonListener(Button b, Type t)
		{
			b.onClick.RemoveAllListeners();
			b.onClick.AddListener(() =>
			{
				if (m_VisibilityState == VisibilityState.Visible && selectTool(tagIndex, t))
					TestForHideCondition();
			});
			b.onClick.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
		}

		private IEnumerator SnapToFace(float flickDirection)
		{
			// when the user releases their input while rotating the menu, snap to the nearest face
			flickDirection = Mathf.RoundToInt(flickDirection);
			m_RotationState = flickDirection != 0 ? RotationState.FlickSnapping : RotationState.Snapping;
			StartCoroutine(AnimateFrameRotationShapeChange(m_RotationState));

			foreach (var face in m_MenuFaces)
				face.EndRotationVisuals();

			const float kAngleThresholdForSnap = 1f;
			if (flickDirection != 0 || Mathf.Abs(m_MenuFaceRotationOrigin.localRotation.eulerAngles.y % kFaceRotationSnapAngle) > kAngleThresholdForSnap) // Snap if not already aligned
			{
				float smoothTransitionIntoSnap = 0.5f;
				float targetSnapSpeed = flickDirection == 0 ? 5f : 10f; // If flicking, given UX intention, move faster to intended face

				const float kTargetSnapThreshold = 0.0005f;
				const float kEaseStepping = 1f;

				Vector3 roundedRotation = m_MenuFaceRotationOrigin.localRotation.eulerAngles;
				float rotationDifference = Mathf.Abs(m_MenuFaceRotationOrigin.localRotation.eulerAngles.y - roundedRotation.y);
				while ((m_RotationState == RotationState.Snapping || m_RotationState == RotationState.FlickSnapping) && (rotationDifference > 1f && rotationDifference < 360))
				{
					smoothTransitionIntoSnap = U.Math.Ease(smoothTransitionIntoSnap, targetSnapSpeed, kEaseStepping, kTargetSnapThreshold);
					float angle = Mathf.LerpAngle(m_MenuFaceRotationOrigin.localRotation.eulerAngles.y, roundedRotation.y, Time.unscaledDeltaTime * smoothTransitionIntoSnap);
					m_MenuFaceRotationOrigin.localRotation = Quaternion.Euler(new Vector3(0, angle, 0));
					yield return null;
					rotationDifference = Mathf.Abs(m_MenuFaceRotationOrigin.localRotation.eulerAngles.y - roundedRotation.y);
				}
			}

			if (m_RotationState == RotationState.Snapping || m_RotationState == RotationState.FlickSnapping)
				m_RotationState = RotationState.AtRest;

			m_RotationRate = 0f;
		}

		private IEnumerator AnimateShow()
		{
			m_VisibilityState = VisibilityState.TransitioningIn;

			foreach (var face in m_MenuFaces)
				face.Show();

			StartCoroutine(AnimateFrameReveal());
			hideDefaultRay();
			lockRay(this);

			const float kTargetScale = 1f;
			const float kTargetSnapThreshold = 0.0005f;
			const float kEaseStepping = 2f;

			float scale = 0f;

			while (m_VisibilityState == VisibilityState.TransitioningIn && scale < kTargetScale)
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

		private IEnumerator AnimateHide()
		{
			m_VisibilityState = VisibilityState.TransitioningOut;

			foreach (var face in m_MenuFaces)
				face.Hide();

			StartCoroutine(AnimateFrameReveal(m_VisibilityState));
			unlockRay(this);
			showDefaultRay();

			const float kTargetScale = 0f;
			const float kTargetSnapThreshold = 0.0005f;
			const float kEaseStepping = 1.1f;
			const float kScaleThreshold = 0.0001f;
			float scale = menuOrigin.localScale.x;

			while (m_VisibilityState == VisibilityState.TransitioningOut && scale > kScaleThreshold)
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

				float roundedRotation = m_MenuFaceRotationOrigin.localRotation.eulerAngles.y;
				roundedRotation = Mathf.Round((roundedRotation) / kFaceRotationSnapAngle) * kFaceRotationSnapAngle; // calculate intended target rotation
				m_MenuFaceRotationOrigin.localRotation = Quaternion.Euler(new Vector3(0, roundedRotation, 0)); // set intended target rotation
				m_RotationState = RotationState.AtRest;
			}
		}

		private IEnumerator AnimateFrameRotationShapeChange(RotationState rotationState)
		{
			float easeDivider = rotationState == RotationState.Rotating ? 8f : 6f; // slower when rotating, faster when snapping
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
			float easeDivider = visibiityState == VisibilityState.TransitioningIn ? 3f : 1.5f; // slower if transitioning in
			const float zeroStartBlendShapePadding = 20f; // start the blendShape at a point slightly above the full hidden value for better visibility
			float currentBlendShapeWeight = m_MenuFrameRenderer.GetBlendShapeWeight(1);
			float targetWeight = visibiityState == VisibilityState.TransitioningIn ? 0f : 100f;
			const float kSnapValue = 0.001f;
			const float kLerpEmphasisWeight = 0.25f;
			currentBlendShapeWeight = currentBlendShapeWeight > 0 ? currentBlendShapeWeight : zeroStartBlendShapePadding;
			while (m_VisibilityState != VisibilityState.Hidden && !Mathf.Approximately(currentBlendShapeWeight, targetWeight))
			{
				currentBlendShapeWeight = U.Math.Ease(currentBlendShapeWeight, targetWeight, easeDivider, kSnapValue);
				m_MenuFrameRenderer.SetBlendShapeWeight(1, currentBlendShapeWeight * currentBlendShapeWeight);
				m_MenuFacesMaterial.color = Color.Lerp(m_MenuFacesColor, kMenuFacesHiddenColor, currentBlendShapeWeight * kLerpEmphasisWeight);
				yield return null;
			}

			if (m_VisibilityState == visibiityState)
			{
				m_MenuFrameRenderer.SetBlendShapeWeight(1, targetWeight);
				m_MenuFacesMaterial.color = targetWeight > 0 ? m_MenuFacesColor : kMenuFacesHiddenColor;
			}

			if (m_VisibilityState == VisibilityState.Hidden)
				m_MenuFrameRenderer.SetBlendShapeWeight(0, 0);
		}

		private void TestForHideCondition()
		{
			// TODO: Implement testing for automatic hiding cases.  The Scene-loading Action will be one case.
		}
	}
}
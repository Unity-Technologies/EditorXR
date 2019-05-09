using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Labs.Utils;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Helpers;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;

#if INCLUDE_TEXT_MESH_PRO
using TMPro;
#endif

[assembly: OptionalDependency("TMPro.TextMeshProUGUI", "INCLUDE_TEXT_MESH_PRO")]

namespace UnityEditor.Experimental.EditorVR.Menus
{
    sealed class MainMenuFace : MonoBehaviour
    {
        static readonly Vector3 k_LocalOffset = Vector3.down * 0.15f;

#pragma warning disable 649
        [SerializeField]
        MeshRenderer m_BorderOutline;

        [SerializeField]
        CanvasGroup m_CanvasGroup;

#if INCLUDE_TEXT_MESH_PRO
        [SerializeField]
        TextMeshProUGUI m_FaceTitle;
#endif

        [SerializeField]
        Transform m_GridTransform;

        [SerializeField]
        SkinnedMeshRenderer m_TitleIcon;

        [SerializeField]
        ScrollRect m_ScrollRect;
#pragma warning restore 649

        Material m_BorderOutlineMaterial;
        Vector3 m_BorderOutlineOriginalLocalScale;
        Transform m_BorderOutlineTransform;
        Material m_TitleIconMaterial;
        Coroutine m_VisibilityCoroutine;
        Coroutine m_RevealCoroutine;
        GradientPair m_GradientPair;
        Vector3 m_OriginalLocalScale;
        Vector3 m_HiddenLocalScale;
        readonly Stack<GameObject> m_Submenus = new Stack<GameObject>();

        const string k_BottomGradientProperty = "_ColorBottom";
        const string k_TopGradientProperty = "_ColorTop";
        readonly GradientPair k_EmptyGradient = new GradientPair(UnityBrandColorScheme.light, UnityBrandColorScheme.darker);

        public GradientPair gradientPair
        {
            get { return m_GradientPair; }
            set
            {
                m_GradientPair = value;
                m_BorderOutlineMaterial.SetColor(k_TopGradientProperty, gradientPair.a);
                m_BorderOutlineMaterial.SetColor(k_BottomGradientProperty, gradientPair.b);
                m_TitleIconMaterial.SetColor(k_TopGradientProperty, gradientPair.a);
                m_TitleIconMaterial.SetColor(k_BottomGradientProperty, gradientPair.b);
            }
        }

        public string title
        {
            set
            {
#if INCLUDE_TEXT_MESH_PRO
                m_FaceTitle.text = value;
#endif
            }
        }

        public bool visible { set { this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateVisibility(value)); } }

        void Awake()
        {
            m_CanvasGroup.alpha = 0f;
            m_CanvasGroup.interactable = false;
            m_BorderOutlineMaterial = MaterialUtils.GetMaterialClone(m_BorderOutline);
            m_BorderOutlineTransform = m_BorderOutline.transform;
            m_BorderOutlineOriginalLocalScale = m_BorderOutlineTransform.localScale;
            m_TitleIconMaterial = MaterialUtils.GetMaterialClone(m_TitleIcon);

            m_OriginalLocalScale = transform.localScale;
            m_HiddenLocalScale = new Vector3(0f, m_OriginalLocalScale.y * 0.5f, m_OriginalLocalScale.z);

            gradientPair = k_EmptyGradient;
        }

        public void AddButton(Transform button)
        {
            button.SetParent(m_GridTransform);
            button.localRotation = Quaternion.identity;
            button.localScale = Vector3.one;
            button.localPosition = Vector3.zero;
        }

        public void Reveal(float delay = 0f)
        {
            this.RestartCoroutine(ref m_RevealCoroutine, AnimateReveal(delay));
        }

        IEnumerator AnimateVisibility(bool show)
        {
            if (show)
                m_BorderOutlineTransform.localScale = m_BorderOutlineOriginalLocalScale;

            m_CanvasGroup.interactable = false;

            var smoothTime = show ? 0.35f : 0.125f;
            var startingOpacity = m_CanvasGroup.alpha;
            var targetOpacity = show ? 1f : 0f;
            var smoothVelocity = 0f;
            var currentDuration = 0f;
            while (currentDuration < smoothTime)
            {
                startingOpacity = MathUtilsExt.SmoothDamp(startingOpacity, targetOpacity, ref smoothVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);
                currentDuration += Time.deltaTime;
                m_CanvasGroup.alpha = startingOpacity * startingOpacity;
                yield return null;
            }

            m_CanvasGroup.alpha = targetOpacity;

            if (show)
                m_CanvasGroup.interactable = true;
            else
                m_TitleIcon.SetBlendShapeWeight(0, 0);
        }

        IEnumerator AnimateReveal(float delay = 0f)
        {
            var targetScale = m_OriginalLocalScale;
            var targetPosition = Vector3.zero;
            var currentScale = m_HiddenLocalScale;
            var currentPosition = k_LocalOffset;

            transform.localScale = currentScale;
            transform.localPosition = currentPosition;

            const float kSmoothTime = 0.1f;
            var currentDelay = 0f;
            var delayTarget = 0.25f + delay; // delay duration before starting the face reveal
            while (currentDelay < delayTarget) // delay the reveal of each face slightly more than the previous
            {
                currentDelay += Time.deltaTime;
                yield return null;
            }

            var smoothVelocity = Vector3.zero;
            while (!Mathf.Approximately(currentScale.x, targetScale.x))
            {
                currentScale = Vector3.SmoothDamp(currentScale, targetScale, ref smoothVelocity, kSmoothTime, Mathf.Infinity, Time.deltaTime);
                currentPosition = Vector3.Lerp(currentPosition, targetPosition, Mathf.Pow(currentScale.x / targetScale.x, 2)); // lerp the position with extra emphasis on the beginning transition
                transform.localScale = currentScale;
                transform.localPosition = currentPosition;
                yield return null;
            }

            transform.localScale = targetScale;
            transform.localPosition = targetPosition;
        }

        public void AddSubmenu(Transform submenu)
        {
            submenu.SetParent(transform.parent);

            submenu.localPosition = Vector3.zero;
            submenu.localScale = Vector3.one;
            submenu.localRotation = Quaternion.identity;
            m_Submenus.Push(submenu.gameObject);
            visible = false;
        }

        public void RemoveSubmenu(Transform rayOrigin)
        {
            var target = m_Submenus.Pop();
            target.SetActive(false);
            UnityObjectUtils.Destroy(target, .1f);

            if (m_Submenus.Count > 1)
                m_Submenus.Last().SetActive(true);
            else
                visible = true;
        }

        public void ClearSubmenus()
        {
            foreach (var submenu in m_Submenus)
            {
                UnityObjectUtils.Destroy(submenu);
            }
        }
    }
}

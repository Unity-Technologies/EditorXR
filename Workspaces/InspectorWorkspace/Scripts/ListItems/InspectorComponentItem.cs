using TMPro;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEditor.Experimental.EditorVR.UI.Button;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class InspectorComponentItem : InspectorListItem
    {
        const float k_ExpandArrowRotateSpeed = 0.4f;
        static readonly Quaternion k_ExpandedRotation = Quaternion.AngleAxis(90f, Vector3.forward);
        static readonly Quaternion k_NormalRotation = Quaternion.identity;

        [SerializeField]
        Button m_ExpandArrow;

        [SerializeField]
        RawImage m_Icon;

        [SerializeField]
        Toggle m_EnabledToggle;

        [SerializeField]
        TextMeshProUGUI m_NameText;

        public override void Setup(InspectorData data)
        {
            base.Setup(data);

#if UNITY_EDITOR
            var target = data.serializedObject.targetObject;
            var type = target.GetType();
            m_NameText.text = type.Name;

            StopAllCoroutines();
            StartCoroutine(ObjectUtils.GetAssetPreview(target, texture => m_Icon.texture = texture));

            var enabled = EditorUtility.GetObjectEnabled(target);
            m_EnabledToggle.gameObject.SetActive(enabled != -1);
            m_EnabledToggle.isOn = enabled == 1;
#endif

            m_ExpandArrow.gameObject.SetActive(data.children != null);
        }

        public override void UpdateSelf(float width, int depth, bool expanded)
        {
            base.UpdateSelf(width, depth, expanded);

            // Rotate arrow for expand state
            m_ExpandArrow.transform.localRotation = Quaternion.Lerp(m_ExpandArrow.transform.localRotation,
                expanded ? k_ExpandedRotation : k_NormalRotation, k_ExpandArrowRotateSpeed);
        }

        public override void OnObjectModified()
        {
            base.OnObjectModified();
#if UNITY_EDITOR
            var enabled = EditorUtility.GetObjectEnabled(data.serializedObject.targetObject);
            m_EnabledToggle.isOn = enabled == 1;
#endif
        }

        public void SetEnabled(bool value)
        {
#if UNITY_EDITOR
            var serializedObject = data.serializedObject;
            var target = serializedObject.targetObject;
            if (value != (EditorUtility.GetObjectEnabled(target) == 1))
            {
                EditorUtility.SetObjectEnabled(target, value);

                Undo.IncrementCurrentGroup();
                serializedObject.ApplyModifiedProperties();
            }
#endif
        }
    }
}

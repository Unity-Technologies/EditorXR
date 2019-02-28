using System;
using System.Collections;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEngine;
using UnityEngine.UI;
using InputField = UnityEditor.Experimental.EditorVR.UI.InputField;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class InspectorHeaderItem : InspectorListItem
    {
#pragma warning disable 649
        [SerializeField]
        RawImage m_Icon;

        [SerializeField]
        Toggle m_ActiveToggle;

        [SerializeField]
        StandardInputField m_NameField;

        [SerializeField]
        Toggle m_StaticToggle;

        [SerializeField]
        Toggle m_LockToggle;

        [SerializeField]
        DropDown m_TagDropDown;

        [SerializeField]
        DropDown m_LayerDropDown;

        [SerializeField]
        MeshRenderer m_Button;
#pragma warning restore 649

        public Toggle lockToggle
        {
            get { return m_LockToggle; }
        }

        GameObject m_TargetGameObject;

        public Action<bool> setLocked;

        public override void Setup(InspectorData data)
        {
            base.Setup(data);

#if UNITY_EDITOR
            var target = data.serializedObject.targetObject;

            StopAllCoroutines();
            StartCoroutine(GetAssetPreview());

            m_TargetGameObject = target as GameObject;
#endif

            UpdateHeaderUI();
        }

#if UNITY_EDITOR
        IEnumerator GetAssetPreview()
        {
            m_Icon.texture = null;

            var target = data.serializedObject.targetObject;
            m_Icon.texture = AssetPreview.GetAssetPreview(target);

            while (AssetPreview.IsLoadingAssetPreview(target.GetInstanceID()))
            {
                m_Icon.texture = AssetPreview.GetAssetPreview(target);
                yield return null;
            }

            if (!m_Icon.texture)
                m_Icon.texture = AssetPreview.GetMiniThumbnail(target);
        }
#endif

        public void SetActive(bool active)
        {
            if (m_TargetGameObject != null && m_TargetGameObject.activeSelf != active)
                m_TargetGameObject.SetActive(active);
        }

        public void SetName(string name)
        {
            var target = data.serializedObject.targetObject;
            if (!target.name.Equals(name))
                target.name = name;
        }

        public void SetStatic(bool isStatic)
        {
            if (m_TargetGameObject != null && m_TargetGameObject.isStatic != isStatic)
                m_TargetGameObject.isStatic = isStatic;
        }

        public void SetLock(bool locked)
        {
            if (setLocked != null)
                setLocked(locked);
        }

#if UNITY_EDITOR
        void SetTag(int val, int[] values)
        {
            var tags = UnityEditorInternal.InternalEditorUtility.tags;
            var tag = tags[values[0]];
            if (!m_TargetGameObject.tag.Equals(tag))
                m_TargetGameObject.tag = tag;
        }
#endif

        void SetLayer(int val, int[] values)
        {
#if UNITY_EDITOR
            var layers = UnityEditorInternal.InternalEditorUtility.layers;
            var layer = LayerMask.NameToLayer(layers[values[0]]);
            if (m_TargetGameObject.layer != layer)
                m_TargetGameObject.layer = layer;
#endif
        }

        protected override object GetDropObjectForFieldBlock(Transform fieldBlock)
        {
            var inputField = fieldBlock.GetComponentInChildren<StandardInputField>();
            if (inputField)
                return inputField.text;
            return null;
        }

        protected override bool CanDropForFieldBlock(Transform fieldBlock, object dropObject)
        {
            var inputFields = fieldBlock.GetComponentsInChildren<InputField>();
            return dropObject is string && inputFields.Contains(m_NameField);
        }

        protected override void ReceiveDropForFieldBlock(Transform fieldBlock, object dropObject)
        {
            m_NameField.text = (string)dropObject;
            m_NameField.ForceUpdateLabel();
        }

        public override void SetMaterials(Material rowMaterial, Material backingCubeMaterial, Material uiMaterial, Material uiMaskMaterial, Material noClipBackingCube, Material[] highlightMaterials, Material[] noClipHighlightMaterials)
        {
            base.SetMaterials(rowMaterial, backingCubeMaterial, uiMaterial, uiMaskMaterial, noClipBackingCube, highlightMaterials, noClipHighlightMaterials);
            m_Button.sharedMaterials = highlightMaterials;
        }

        public override void OnObjectModified()
        {
            base.OnObjectModified();
            UpdateHeaderUI();
        }

        public void UpdateHeaderUI()
        {
            if (m_TargetGameObject)
            {
                m_ActiveToggle.isOn = m_TargetGameObject.activeSelf;
                m_StaticToggle.isOn = m_TargetGameObject.isStatic;
            }

            m_NameField.text = m_TargetGameObject.name;
            m_NameField.ForceUpdateLabel();

#if UNITY_EDITOR
            if (m_TargetGameObject)
            {
                var tags = UnityEditorInternal.InternalEditorUtility.tags;
                m_TagDropDown.options = tags;
                var tagIndex = Array.IndexOf(tags, m_TargetGameObject.tag);
                if (tagIndex > -1)
                    m_TagDropDown.value = tagIndex;
                m_TagDropDown.valueChanged += SetTag;

                var layers = UnityEditorInternal.InternalEditorUtility.layers;
                m_LayerDropDown.options = layers;
                var layerIndex = Array.IndexOf(layers, LayerMask.LayerToName(m_TargetGameObject.layer));
                if (layerIndex > -1)
                    m_LayerDropDown.value = layerIndex;
                m_LayerDropDown.valueChanged += SetLayer;
            }
#endif
        }
    }
}

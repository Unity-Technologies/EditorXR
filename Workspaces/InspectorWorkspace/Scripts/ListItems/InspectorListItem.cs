#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR.Data;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.UI;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.UI;
using InputField = UnityEditor.Experimental.EditorVR.UI.InputField;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    abstract class InspectorListItem : DraggableListItem<InspectorData, int>, ISetHighlight, IGetFieldGrabOrigin
    {
        const float k_Indent = 0.02f;
        const float k_HorizThreshold = 0.85f;

        protected CuboidLayout m_CuboidLayout;

        protected InputField[] m_InputFields;

        [SerializeField]
        BaseHandle m_Cube;

        [SerializeField]
        RectTransform m_UIContainer;

        [SerializeField]
        Material m_NoClipText;

        [SerializeField]
        Material m_DropHighlightMaterial;

        ClipText[] m_ClipTexts;

        Material m_NoClipBackingCube;
        Material[] m_NoClipHighlightMaterials;

        bool m_Setup;
        bool m_HorizontalDrag;

        Transform m_DragClone;
        protected NumericInputField m_DraggedField;

        public bool setup { get; set; }

        public Action<int> toggleExpanded { private get; set; }

        protected override bool singleClickDrag
        {
            get { return false; }
        }

        public override void Setup(InspectorData data)
        {
            base.Setup(data);

            if (!m_Setup)
            {
                m_Setup = true;
                FirstTimeSetup();
            }
        }

        protected virtual void FirstTimeSetup()
        {
            m_ClipTexts = GetComponentsInChildren<ClipText>(true);
            m_CuboidLayout = GetComponentInChildren<CuboidLayout>(true);
            if (m_CuboidLayout)
                m_CuboidLayout.UpdateObjects();

            var handles = GetComponentsInChildren<BaseHandle>(true);
            foreach (var handle in handles)
            {
                // Ignore m_Cube for now (will be used for Reset action)
                if (handle.Equals(m_Cube))
                    continue;

                // Toggles can't be dragged
                if (handle.transform.parent.GetComponentInChildren<Toggle>())
                    continue;

                handle.dragStarted += OnDragStarted;
                handle.dragging += OnDragging;
                handle.dragEnded += OnDragEnded;

                handle.dropHoverStarted += OnDropHoverStarted;
                handle.dropHoverEnded += OnDropHoverEnded;

                handle.canDrop = CanDrop;
                handle.receiveDrop = ReceiveDrop;
                handle.getDropObject = GetDropObject;
            }

            m_InputFields = GetComponentsInChildren<InputField>(true);
        }

        public virtual void SetMaterials(Material rowMaterial, Material backingCubeMaterial, Material uiMaterial, Material uiMaskMaterial, Material textMaterial, Material noClipBackingCube, Material[] highlightMaterials, Material[] noClipHighlightMaterials)
        {
            m_NoClipBackingCube = noClipBackingCube;
            m_NoClipHighlightMaterials = noClipHighlightMaterials;

            m_Cube.GetComponent<Renderer>().sharedMaterial = rowMaterial;

            var cuboidLayouts = GetComponentsInChildren<CuboidLayout>(true);
            foreach (var cuboidLayout in cuboidLayouts)
            {
                cuboidLayout.SetMaterials(backingCubeMaterial, highlightMaterials);
            }

            var workspaceButtons = GetComponentsInChildren<WorkspaceButton>(true);
            foreach (var button in workspaceButtons)
            {
                button.buttonMeshRenderer.sharedMaterials = highlightMaterials;
            }

            var graphics = GetComponentsInChildren<Graphic>(true);
            foreach (var graphic in graphics)
            {
                graphic.material = uiMaterial;
            }

            // Texts need a specific shader
            var texts = GetComponentsInChildren<Text>(true);
            foreach (var text in texts)
            {
                text.material = textMaterial;
            }

            // Don't clip masks
            var masks = GetComponentsInChildren<Mask>(true);
            foreach (var mask in masks)
            {
                mask.graphic.material = uiMaskMaterial;
            }
        }

        public virtual void UpdateSelf(float width, int depth, bool expanded)
        {
            var cubeScale = m_Cube.transform.localScale;
            cubeScale.x = width;
            m_Cube.transform.localScale = cubeScale;

            if (depth > 0) // Lose one level of indentation because everything is a child of the header
                depth--;

            var indent = k_Indent * depth;
            m_UIContainer.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, indent, width - indent);

            if (m_CuboidLayout)
                m_CuboidLayout.UpdateObjects();
        }

        public virtual void OnObjectModified()
        {
            if (data.serializedObject.targetObject) // An exception is thrown if the targetObject has been deleted
                data.serializedObject.Update();
        }

        public void UpdateClipTexts(Matrix4x4 parentMatrix, Vector3 clipExtents)
        {
            foreach (var clipText in m_ClipTexts)
            {
                clipText.clipExtents = clipExtents;
                clipText.parentMatrix = parentMatrix;
                clipText.UpdateMaterialClip();
            }
        }

        protected virtual void OnDropHoverStarted(BaseHandle handle)
        {
            this.SetHighlight(handle.gameObject, true, material: m_DropHighlightMaterial);
        }

        protected virtual void OnDropHoverEnded(BaseHandle handle)
        {
            this.SetHighlight(handle.gameObject, false, material: m_DropHighlightMaterial);
        }

        object GetDropObject(BaseHandle handle)
        {
            if (!m_DragObject || m_HorizontalDrag)
                return null;

            return GetDropObjectForFieldBlock(handle.transform.parent);
        }

        bool CanDrop(BaseHandle handle, object dropObject)
        {
            return CanDropForFieldBlock(handle.transform.parent, dropObject);
        }

        void ReceiveDrop(BaseHandle handle, object dropObject)
        {
            ReceiveDropForFieldBlock(handle.transform.parent, dropObject);
        }

        protected override void OnDragStarted(BaseHandle handle, HandleEventData eventData)
        {
            base.OnDragStarted(handle, eventData);
            m_HorizontalDrag = false;
        }

        protected override void OnDragStarted(BaseHandle handle, HandleEventData eventData, Vector3 dragStart)
        {
            var dragVector = eventData.rayOrigin.position - dragStart;
            var distance = dragVector.magnitude;
            m_HorizontalDrag = Mathf.Abs(Vector3.Dot(dragVector, m_DragObject.right)) / distance > k_HorizThreshold;

            var fieldBlock = handle.transform.parent;
            if (fieldBlock)
            {
                if (m_HorizontalDrag)
                    OnHorizontalDragStart(eventData.rayOrigin, fieldBlock);
                else
                    OnVerticalDragStart(fieldBlock);
            }
        }

        protected override void OnDragging(BaseHandle handle, HandleEventData eventData, Vector3 dragStart)
        {
            if (m_HorizontalDrag)
                OnHorizontalDragging(eventData.rayOrigin);
            else
                OnVerticalDragging(eventData.rayOrigin);
        }

        protected virtual void OnVerticalDragStart(Transform fieldBlock)
        {
            var clone = ((GameObject)Instantiate(fieldBlock.gameObject, fieldBlock.parent)).transform;

            // Re-center pivot
            clone.GetComponent<RectTransform>().pivot = Vector2.one * 0.5f;

            // Re-center backing cube
            foreach (Transform child in clone)
            {
                if (child.GetComponent<BaseHandle>())
                {
                    var localPos = child.localPosition;
                    localPos.x = 0;
                    localPos.y = 0;
                    child.localPosition = localPos;
                }
            }

            var graphics = clone.GetComponentsInChildren<Graphic>(true);
            foreach (var graphic in graphics)
            {
                graphic.raycastTarget = false;

                if (graphic.GetComponent<Mask>())
                    continue;

                graphic.material = null;
            }

            var renderers = clone.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterials.Length > 1)
                {
                    renderer.sharedMaterials = m_NoClipHighlightMaterials;
                }
                else
                {
                    renderer.sharedMaterial = m_NoClipBackingCube;
                }
            }

            var texts = clone.GetComponentsInChildren<Text>(true);
            foreach (var text in texts)
            {
                text.material = m_NoClipText;
            }

            var colliders = clone.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }

            m_DragClone = clone;

            StartCoroutine(Magnetize());
        }

        protected virtual void OnVerticalDragging(Transform rayOrigin)
        {
            if (m_DragClone)
            {
                var fieldGrabOrigin = this.GetFieldGrabOriginForRayOrigin(rayOrigin);
                var rotation = MathUtilsExt.ConstrainYawRotation(CameraUtils.GetMainCamera().transform.rotation);
                MathUtilsExt.LerpTransform(m_DragClone, fieldGrabOrigin.position, rotation, m_DragLerp);
            }
        }

        protected virtual void OnHorizontalDragStart(Transform rayOrigin, Transform fieldBlock)
        {
            // Get RayInputField from direct children
            foreach (Transform child in fieldBlock.transform)
            {
                var inputField = child.GetComponent<InputField>();
                if (inputField)
                {
                    m_DraggedField = inputField as NumericInputField;
                    m_DraggedField.BeginSliderDrag(rayOrigin);
                    break;
                }
            }
        }

        protected virtual void OnHorizontalDragging(Transform rayOrigin)
        {
            if (m_DraggedField)
                m_DraggedField.SliderDrag(rayOrigin);
        }

        protected override void OnDragEnded(BaseHandle handle, HandleEventData eventData)
        {
            if (m_DraggedField)
                m_DraggedField.EndSliderDrag(eventData.rayOrigin);

            // Delay call fixes errors when you close the workspace or change data while dragging a field
            EditorApplication.delayCall += () =>
            {
                if (m_DragClone)
                    ObjectUtils.Destroy(m_DragClone.gameObject);
            };

            if (!m_DragObject)
            {
                InputField inputField = null;
                var fieldBlock = handle.transform.parent;
                foreach (Transform child in fieldBlock.transform)
                {
                    inputField = child.GetComponent<InputField>();
                    if (inputField)
                    {
                        inputField.OpenKeyboard();
                        break;
                    }
                }

                foreach (var field in m_InputFields)
                {
                    field.CloseKeyboard(inputField == null);
                }

                if (inputField)
                    inputField.OpenKeyboard();
            }

            base.OnDragEnded(handle, eventData);
        }

        protected virtual object GetDropObjectForFieldBlock(Transform fieldBlock)
        {
            return null;
        }

        protected virtual bool CanDropForFieldBlock(Transform fieldBlock, object dropObject)
        {
            return false;
        }

        protected virtual void ReceiveDropForFieldBlock(Transform fieldBlock, object dropObject) {}

        public void ToggleExpanded()
        {
            toggleExpanded(data.index);
        }
    }
}
#endif

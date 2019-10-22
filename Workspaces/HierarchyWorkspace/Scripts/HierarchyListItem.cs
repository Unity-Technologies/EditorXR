using System;
using System.Collections.Generic;
using TMPro;
using Unity.Labs.ListView;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    sealed class HierarchyListItem : NestedDraggableListItem<HierarchyData, int>, IUsesViewerBody, IGetFieldGrabOrigin
    {
        const float k_Margin = 0.01f;
        const float k_Indent = 0.02f;

        const float k_ExpandArrowRotateSpeed = 0.4f;

#pragma warning disable 649
        [SerializeField]
        TextMeshProUGUI m_Text;

        [SerializeField]
        TextMeshProUGUI m_SceneText;

        [SerializeField]
        BaseHandle m_Lock;

        [SerializeField]
        BaseHandle m_Cube;

        [SerializeField]
        BaseHandle m_ExpandArrow;

        [SerializeField]
        Transform m_SceneIcon;

        [SerializeField]
        Renderer m_SceneIconRenderer;

        [SerializeField]
        BaseHandle m_DropZone;

        [SerializeField]
        Material m_NoClipExpandArrow;

        [SerializeField]
        Material m_NoClipBackingCube;

        [SerializeField]
        Material m_NoClipText;

        [SerializeField]
        Color m_HoverColor;

        [SerializeField]
        Color m_SelectedColor;

        [SerializeField]
        Color m_SelectedHoverColor;

        [SerializeField]
        Color m_PrefabTextColor;

        [SerializeField]
        Color m_SelectedPrefabTextColor;

        [Tooltip("The fraction of the cube height to use for stacking grabbed rows")]
        [SerializeField]
        float m_StackingFraction = 0.3f;
#pragma warning restore 649

        Color m_NormalColor;
        Color m_NormalTextColor;
        Renderer m_CubeRenderer;
        Transform m_CubeTransform;
        Transform m_DropZoneTransform;

        float m_DropZoneHighlightAlpha;

        readonly Dictionary<Graphic, Material> m_OldMaterials = new Dictionary<Graphic, Material>();
        readonly List<HierarchyListItem> m_VisibleChildren = new List<HierarchyListItem>();

        Renderer m_ExpandArrowRenderer;
        Material m_ExpandArrowMaterial;

        Renderer m_LockRenderer;
        Material m_LockIconMaterial;
        Material m_UnlockIconMaterial;

        bool m_HoveringLock;

        public bool hovering { get; private set; }
        public Transform hoveringRayOrigin { get; private set; }

        public Material cubeMaterial { get; private set; }
        public Material dropZoneMaterial { get; private set; }

        public Action<int> selectRow { private get; set; }

        public Action<int> toggleLock { private get; set; }

        public Action<int, bool> setExpanded { private get; set; }
        public Func<int, bool> isExpanded { private get; set; }

        protected override bool singleClickDrag { get { return false; } }

        public int extraSpace { get; private set; }

        public bool isStillSettling { private set; get; }

        public Func<int, HierarchyListItem> getListItem { private get; set; }

        public override void Setup(HierarchyData data, bool firstTime)
        {
            base.Setup(data, firstTime);

            // First time setup
            if (firstTime)
            {
                // Cube material might change for hover state, so we always instance it
                m_CubeRenderer = m_Cube.GetComponent<Renderer>();
                cubeMaterial = MaterialUtils.GetMaterialClone(m_CubeRenderer);
                m_NormalColor = cubeMaterial.color;

                m_LockRenderer = m_Lock.GetComponent<Renderer>();
                m_Lock.hoverStarted += (bh, ed) => { m_HoveringLock = true; };
                m_Lock.hoverEnded += (bh, ed) => { m_HoveringLock = false; };
                m_Lock.dragEnded += ToggleLock;

                m_ExpandArrowRenderer = m_ExpandArrow.GetComponent<Renderer>();
                m_ExpandArrow.dragEnded += ToggleExpanded;
                m_Cube.dragStarted += OnDragStarted;
                m_Cube.dragging += OnDragging;
                m_Cube.dragEnded += OnDragEnded;

                m_Cube.hoverStarted += OnHoverStarted;
                m_Cube.hoverEnded += OnHoverEnded;

                m_Cube.click += OnClick;

                m_Cube.getDropObject = GetDropObject;
                m_Cube.canDrop = CanDrop;
                m_Cube.receiveDrop = ReceiveDrop;

                var dropZoneRenderer = m_DropZone.GetComponent<Renderer>();
                dropZoneMaterial = MaterialUtils.GetMaterialClone(dropZoneRenderer);
                var color = dropZoneMaterial.color;
                m_DropZoneHighlightAlpha = color.a;
                color.a = 0;
                dropZoneMaterial.color = color;

                m_DropZone.dropHoverStarted += OnDropHoverStarted;
                m_DropZone.dropHoverEnded += OnDropHoverEnded;

                m_DropZone.canDrop = CanDrop;
                m_DropZone.receiveDrop = ReceiveDrop;
                m_DropZone.getDropObject = GetDropObject;

                m_NormalTextColor = m_Text.color;
            }

            m_CubeTransform = m_Cube.transform;
            m_DropZoneTransform = m_DropZone.transform;

            var name = data.name;
            if (data.gameObject == null)
            {
                m_SceneText.text = string.IsNullOrEmpty(name) ? "Untitled" : name;
                m_SceneIcon.gameObject.SetActive(true);
                m_SceneText.gameObject.SetActive(true);
                m_Text.gameObject.SetActive(false);
            }
            else
            {
                m_Text.text = name;
                m_SceneIcon.gameObject.SetActive(false);
                m_SceneText.gameObject.SetActive(false);
                m_Text.gameObject.SetActive(true);
            }

            hovering = false;
        }

        public void SetMaterials(Material textMaterial, Material expandArrowMaterial, Material lockIconMaterial, Material unlockIconMaterial,
            Material sceneIconDarkMaterial, Material sceneIconWhiteMaterial)
        {
            m_Text.fontMaterial = textMaterial;
            m_SceneText.fontMaterial = textMaterial;
            m_ExpandArrowMaterial = expandArrowMaterial;
            m_ExpandArrowRenderer.sharedMaterial = expandArrowMaterial;
            m_LockIconMaterial = lockIconMaterial;
            m_UnlockIconMaterial = unlockIconMaterial;
            m_LockRenderer.sharedMaterial = unlockIconMaterial;
            var materials = new Material[2];
            materials[0] = sceneIconDarkMaterial;
            materials[1] = sceneIconWhiteMaterial;
            m_SceneIconRenderer.sharedMaterials = materials;
        }

        public void UpdateSelf(float width, int depth, bool? expanded, bool selected, bool locked)
        {
            var cubeScale = m_CubeTransform.localScale;
            cubeScale.x = width;
            m_CubeTransform.localScale = cubeScale;

            var expandArrowTransform = m_ExpandArrow.transform;

            var arrowWidth = expandArrowTransform.localScale.x * 0.5f;
            var halfWidth = width * 0.5f;
            var indent = k_Indent * depth;
            const float doubleMargin = k_Margin * 2;
            expandArrowTransform.localPosition = new Vector3(k_Margin + indent - halfWidth, expandArrowTransform.localPosition.y, 0);

            m_LockRenderer.sharedMaterial = (!locked && m_HoveringLock) || (locked && !m_HoveringLock) ? m_LockIconMaterial : m_UnlockIconMaterial;
            var lockIconTransform = m_Lock.transform;
            var lockWidth = lockIconTransform.localScale.x * 0.5f;

            // Text is next to arrow, with a margin and indent, rotated toward camera
            var gameObject = data.gameObject;
            if (gameObject == null)
                indent = k_Indent;

            var textTransform = gameObject ? m_Text.transform : m_SceneText.transform;
            var textRectTransform = gameObject ? m_Text.rectTransform : m_SceneText.rectTransform;
            textRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (width - doubleMargin - indent) * 1 / textTransform.localScale.x);
            textTransform.localPosition = new Vector3(doubleMargin + indent + arrowWidth - halfWidth, textTransform.localPosition.y, 0);
            lockIconTransform.localPosition = new Vector3(halfWidth - lockWidth - k_Margin, lockIconTransform.localPosition.y, 0);
            var sceneIconPosition = m_SceneIcon.localPosition;
            m_SceneIcon.localPosition = new Vector3(-halfWidth + k_Margin + arrowWidth + k_Indent, sceneIconPosition.y, sceneIconPosition.z);

            var localRotation = CameraUtils.LocalRotateTowardCamera(transform.parent);
            textTransform.localRotation = localRotation;
            lockIconTransform.localRotation = localRotation;
            m_SceneIcon.localRotation = localRotation;

            var dropZoneScale = m_DropZoneTransform.localScale;
            dropZoneScale.x = width - indent;
            m_DropZoneTransform.localScale = dropZoneScale;
            var dropZonePosition = m_DropZoneTransform.localPosition;
            dropZonePosition.x = indent * 0.5f;
            m_DropZoneTransform.localPosition = dropZonePosition;

            UpdateArrow(expanded);

#if UNITY_EDITOR && ENABLE_EDITORXR
            var isPrefab = gameObject && PrefabUtility.GetPrefabInstanceStatus(gameObject) == PrefabInstanceStatus.Connected;
#else
            var isPrefab = false;
#endif

            // Set selected/hover/normal color
            if (selected)
            {
                cubeMaterial.color = hovering ? m_SelectedHoverColor : m_SelectedColor;
                m_Text.color = isPrefab ? m_SelectedPrefabTextColor : m_NormalTextColor;
            }
            else if (hovering)
            {
                cubeMaterial.color = m_HoverColor;
                m_Text.color = isPrefab ? m_PrefabTextColor : m_NormalTextColor;
            }
            else
            {
                cubeMaterial.color = m_NormalColor;
                m_Text.color = isPrefab ? m_PrefabTextColor : m_NormalTextColor;
            }
        }

        public void UpdateArrow(bool? expanded, bool immediate = false)
        {
            if (!expanded.HasValue)
            {
                m_ExpandArrow.gameObject.SetActive(false);
                return;
            }

            m_ExpandArrow.gameObject.SetActive(data.children != null);
            var expandArrowTransform = m_ExpandArrow.transform;

            // Rotate arrow for expand state
            expandArrowTransform.localRotation = Quaternion.Lerp(expandArrowTransform.localRotation,
                Quaternion.AngleAxis(90f, Vector3.right) * (expanded.Value ? Quaternion.AngleAxis(90f, Vector3.back) : Quaternion.identity),
                immediate ? 1f : k_ExpandArrowRotateSpeed);
        }

        void OnClick(BaseHandle handle, PointerEventData pointerEventData)
        {
            SelectFolder();
            ToggleExpanded();
        }

        protected override void OnDragStarted(BaseHandle handle, HandleEventData eventData, Vector3 dragStart)
        {
            // handle will be the backing cube, not the whole row object
            var row = handle.transform.parent;
            if (row)
            {
                m_DragObject = row;
                StartCoroutine(Magnetize());
                isStillSettling = true;

                m_VisibleChildren.Clear();
                OnGrabRecursive(m_VisibleChildren, eventData.rayOrigin);
                startSettling(null);
            }
            else
            {
                m_DragObject = null;
            }
        }

        void OnGrabRecursive(List<HierarchyListItem> visibleChildren, Transform rayOrigin)
        {
            m_OldMaterials.Clear();
            var graphics = GetComponentsInChildren<Graphic>(true);
            foreach (var graphic in graphics)
            {
                m_OldMaterials[graphic] = graphic.material;
                graphic.material = null;
            }

            m_ExpandArrowRenderer.sharedMaterial = m_NoClipExpandArrow;
            m_CubeRenderer.sharedMaterial = m_NoClipBackingCube;
            m_Text.transform.localRotation = Quaternion.AngleAxis(90, Vector3.right);
            m_Text.fontMaterial = m_NoClipText;

            m_DropZone.gameObject.SetActive(false);
            m_Cube.GetComponent<Collider>().enabled = false;

            setRowGrabbed(data.index, rayOrigin, true);

            if (data.children != null)
            {
                foreach (var child in data.children)
                {
                    var item = getListItem(child.index);
                    if (item)
                    {
                        visibleChildren.Add(item);
                        item.OnGrabRecursive(visibleChildren, rayOrigin);
                    }
                }
            }
        }

        protected override void OnDragging(BaseHandle handle, HandleEventData eventData, Vector3 dragStart)
        {
            if (m_DragObject)
            {
                var fieldGrabOrigin = this.GetFieldGrabOriginForRayOrigin(eventData.rayOrigin);
                MagnetizeTransform(fieldGrabOrigin, m_DragObject);
                var offset = 0f;
                foreach (var child in m_VisibleChildren)
                {
                    offset += m_CubeRenderer.bounds.size.y * m_StackingFraction;
                    MagnetizeTransform(fieldGrabOrigin, child.transform, offset);
                }
            }
        }

        void MagnetizeTransform(Transform fieldGrabOrigin, Transform transform, float stackingOffset = 0)
        {
            var rotation = MathUtilsExt.ConstrainYawRotation(CameraUtils.GetMainCamera().transform.rotation)
                * Quaternion.AngleAxis(90, Vector3.left);
            var stackingDirection = rotation * Vector3.one;
            MathUtilsExt.LerpTransform(transform, fieldGrabOrigin.position - stackingDirection * stackingOffset, rotation, m_DragLerp);
        }

        protected override void OnMagnetizeEnded()
        {
            base.OnMagnetizeEnded();
            isStillSettling = false;
        }

        protected override void OnDragEnded(BaseHandle handle, HandleEventData eventData)
        {
            if (m_DragObject)
            {
                if (this.IsOverShoulder(transform))
                {
#if UNITY_EDITOR
                    ObjectUtils.Destroy(EditorUtility.InstanceIDToObject(data.index));
#else
                    // TODO: Hierarchy indices
#endif
                }

                // OnHierarchyChanged doesn't happen until next frame--delay un-grab so the object doesn't start moving to the wrong spot
                EditorApplication.delayCall += () => { OnDragEndRecursive(eventData.rayOrigin); };

                isStillSettling = true;
                startSettling(OnDragEndAfterSettling);
            }

            base.OnDragEnded(handle, eventData);
        }

        void OnDragEndRecursive(Transform rayOrigin)
        {
            isStillSettling = false;
            setRowGrabbed(data.index, rayOrigin, false);

            foreach (var child in m_VisibleChildren)
            {
                child.OnDragEndRecursive(rayOrigin);
            }
        }

        void OnDragEndAfterSettling()
        {
            ResetAfterSettling();
            foreach (var child in m_VisibleChildren)
            {
                child.ResetAfterSettling();
            }
        }

        void ResetAfterSettling()
        {
            foreach (var kvp in m_OldMaterials)
            {
                kvp.Key.material = kvp.Value;
            }

            m_CubeRenderer.sharedMaterial = cubeMaterial;
            m_ExpandArrowRenderer.sharedMaterial = m_ExpandArrowMaterial;
            m_DropZone.gameObject.SetActive(true);
            m_Cube.GetComponent<Collider>().enabled = true;
            hovering = false;
        }

        void ToggleLock(BaseHandle handle, HandleEventData eventData)
        {
            if (toggleLock != null)
                toggleLock(data.index);
        }

        void ToggleExpanded(BaseHandle handle, HandleEventData eventData)
        {
            ToggleExpanded();
        }

        void SelectFolder()
        {
            selectRow(data.index);
        }

        void OnHoverStarted(BaseHandle handle, HandleEventData eventData)
        {
            hovering = true;
            hoveringRayOrigin = eventData.rayOrigin;
        }

        void OnHoverEnded(BaseHandle handle, HandleEventData eventData)
        {
            hovering = false;
            hoveringRayOrigin = eventData.rayOrigin;
        }

        void OnDropHoverStarted(BaseHandle handle)
        {
            var color = dropZoneMaterial.color;
            color.a = m_DropZoneHighlightAlpha;
            dropZoneMaterial.color = color;

            startSettling(null);
            extraSpace = 1;
        }

        void OnDropHoverEnded(BaseHandle handle)
        {
            var color = dropZoneMaterial.color;
            color.a = 0;
            dropZoneMaterial.color = color;

            startSettling(null);
            extraSpace = 0;
        }

        object GetDropObject(BaseHandle handle)
        {
            return m_DragObject ? data : null;
        }

        bool CanDrop(BaseHandle handle, object dropObject)
        {
            if (this.IsOverShoulder(handle.transform))
                return false;

            var dropData = dropObject as HierarchyData;
            if (dropData == null)
                return false;

            // Dropping on own zone would otherwise move object down
            if (dropObject == data)
                return false;

            if (handle == m_Cube)
                return true;

            var index = data.index;
            if (isExpanded(index))
                return true;

            var gameObject = data.gameObject;
            var dropGameObject = dropData.gameObject;
            var transform = gameObject.transform;
            var dropTransform = dropGameObject.transform;

            var siblings = transform.parent == null && dropTransform.parent == null
                || transform.parent && dropTransform.parent == transform.parent;

            // Dropping on previous sibling's zone has no effect
            if (siblings && transform.GetSiblingIndex() == dropTransform.GetSiblingIndex() - 1)
                return false;

            return true;
        }

        void ReceiveDrop(BaseHandle handle, object dropObject)
        {
            if (this.IsOverShoulder(handle.transform))
                return;

            var dropData = dropObject as HierarchyData;
            if (dropData != null)
            {
                var index = data.index;
                var gameObject = data.gameObject;
                Transform transform = null;
                if (gameObject != null) // In case we are dropping into the scene root
                    transform = gameObject.transform;

                var dropGameObject = dropData.gameObject;
                var dropTransform = dropGameObject.transform;

                // OnHierarchyChanged doesn't happen until next frame--delay removal of the extra space
                EditorApplication.delayCall += () => { extraSpace = 0; };

                if (handle == m_Cube)
                {
                    dropTransform.SetParent(transform);
                    dropTransform.SetAsLastSibling();

                    EditorApplication.delayCall += () => { setExpanded(index, true); };
                }
                else if (handle == m_DropZone)
                {
                    if (isExpanded(index))
                    {
                        dropTransform.SetParent(transform);
                        dropTransform.SetAsFirstSibling();
                    }
                    else
                    {
                        var targetIndex = transform.GetSiblingIndex() + 1;
                        if (dropTransform.parent == transform.parent && dropTransform.GetSiblingIndex() < targetIndex)
                            targetIndex--;

                        dropTransform.SetParent(transform.parent);
                        dropTransform.SetSiblingIndex(targetIndex);
                    }
                }
            }
        }

        void OnDestroy()
        {
            ObjectUtils.Destroy(cubeMaterial);
            ObjectUtils.Destroy(dropZoneMaterial);
        }
    }
}

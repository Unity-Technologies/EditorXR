using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Unity.Labs.ListView
{
    public abstract class ListViewControllerBase : MonoBehaviour, IScrollHandler
    {
        [Tooltip("Distance (in meters) we have scrolled from initial position")]
        [SerializeField]
        protected float m_ScrollOffset;

        [Tooltip("Padding (in meters) between items")]
        [SerializeField]
        protected float m_Padding = 0.01f;

        [Tooltip("How quickly scroll momentum fade")]
        [SerializeField]
        float m_ScrollDamping = 5f;

        [Tooltip("Maximum velocity for scroll momentum")]
        [SerializeField]
        float m_MaxMomentum = 2f;

        [SerializeField]
        protected float m_SettleSpeed = 0.4f;

        [SerializeField]
        float m_ScrollSpeed = 0.3f;

        [Tooltip("Item template prefabs (at least one is required)")]
        [SerializeField]
        protected GameObject[] m_Templates;

        [Tooltip("Whether to interpolate item positions")]
        [SerializeField]
        protected bool m_EnableSettling = true;

        float m_LastScrollOffset;

        protected bool m_Settling;
        event Action settlingCompleted;

        protected Vector3 m_ItemSize;

        protected Vector3 m_StartPosition;

        protected bool m_Scrolling;
        protected float m_ScrollReturn = float.MaxValue;
        protected float m_ScrollDelta;

        protected Vector3 m_Size;
        protected Vector3 m_Extents;

        protected abstract float listHeight { get; }

        public float scrollOffset
        {
            get { return m_ScrollOffset; }
            set { m_ScrollOffset = value; }
        }

        public float scrollSpeed
        {
            get { return m_ScrollSpeed; }
            set { m_ScrollSpeed = value; }
        }

        public Vector3 itemSize
        {
            get { return m_ItemSize; }
        }

        public virtual Vector3 size
        {
            set
            {
                m_Size = value;
                m_Extents = m_Size * 0.5f;
            }
        }

        protected virtual void Awake()
        {
            if (m_Templates.Length > 0)
                m_ItemSize = GetObjectSize(m_Templates[0]);
            else
                Debug.LogWarning("List View Error: At least one template is required", this);
        }

        protected virtual void Update()
        {
            UpdateView();
        }

        protected virtual void UpdateView()
        {
            ComputeConditions();
            UpdateItems();
        }

        protected virtual void ComputeConditions()
        {
            m_StartPosition = (m_Extents.z - m_ItemSize.z * 0.5f) * Vector3.forward;

            if (m_Scrolling)
            {
                m_ScrollDelta = Mathf.Clamp((m_ScrollOffset - m_LastScrollOffset) / Time.deltaTime, -m_MaxMomentum, m_MaxMomentum);
                m_LastScrollOffset = m_ScrollOffset;
            }
            else
            {
                //Apply scrolling momentum
                m_ScrollOffset += m_ScrollDelta * Time.deltaTime;
                if (m_ScrollReturn < float.MaxValue || m_ScrollOffset > 0)
                    OnScrollEnded();

                if (m_ScrollDelta > 0)
                {
                    m_ScrollDelta -= m_ScrollDamping * Time.deltaTime;
                    if (m_ScrollDelta < 0)
                    {
                        m_ScrollDelta = 0;
                        OnScrollEnded();
                    }
                }
                else if (m_ScrollDelta < 0)
                {
                    m_ScrollDelta += m_ScrollDamping * Time.deltaTime;
                    if (m_ScrollDelta > 0)
                    {
                        m_ScrollDelta = 0;
                        OnScrollEnded();
                    }
                }
            }

            m_ScrollReturn = float.MaxValue;

            const float epsilon = 1e-6f;

            // Snap back if list scrolled too far
            if (listHeight > 0 && -m_ScrollOffset >= listHeight)
                m_ScrollReturn = -listHeight + epsilon;
        }

        protected abstract void UpdateItems();

        public virtual void ScrollNext()
        {
            m_ScrollOffset -= m_ItemSize.z;
        }

        public virtual void ScrollPrevious()
        {
            m_ScrollOffset += m_ItemSize.z;
        }

        public virtual void ScrollTo(int index)
        {
            m_ScrollOffset = index * m_ItemSize.z;
        }

        protected virtual void UpdateItem(IListViewItem item, int order, float offset, ref bool doneSettling)
        {
            var targetPosition = m_StartPosition + offset * Vector3.back;
            var targetRotation = Quaternion.identity;
            UpdateItemTransform(item, order, targetPosition, targetRotation, false, ref doneSettling);
        }

        protected virtual void UpdateItemTransform(IListViewItem item, int order, Vector3 targetPosition,
            Quaternion targetRotation, bool dontSettle, ref bool doneSettling)
        {
            if (m_Settling && !dontSettle && m_EnableSettling)
            {
                var localPosition = Vector3.Lerp(item.localPosition, targetPosition, m_SettleSpeed);
                item.localPosition = localPosition;
                if (localPosition != targetPosition)
                    doneSettling = false;

                var localRotation = Quaternion.Lerp(item.localRotation, targetRotation, m_SettleSpeed);
                item.localRotation = localRotation;
                if (localRotation != targetRotation)
                    doneSettling = false;
            }
            else
            {
                item.localPosition = targetPosition;
                item.localRotation = targetRotation;
            }

            item.SetSiblingIndex(order);
        }

        protected virtual Vector3 GetObjectSize(GameObject g)
        {
            var objectSize = Vector3.one;
            var rend = g.GetComponentInChildren<Renderer>();
            if (rend)
                objectSize = Vector3.Scale(g.transform.lossyScale, rend.bounds.extents) * 2 + Vector3.one * m_Padding;

            return objectSize;
        }

        public virtual void OnScrollStarted()
        {
            m_Scrolling = true;
        }

        public virtual void OnScrollEnded()
        {
            m_Scrolling = false;

            if (m_ScrollOffset > 0)
            {
                StartSettling();
                m_ScrollOffset = 0;
                m_ScrollDelta = 0;
            }

            if (m_ScrollReturn < float.MaxValue)
            {
                StartSettling();
                m_ScrollOffset = m_ScrollReturn;
                m_ScrollReturn = float.MaxValue;
                m_ScrollDelta = 0;
            }
        }

        protected void SetMaterialClip(Material material, Matrix4x4 parentMatrix)
        {
            material.SetMatrix("_ParentMatrix", parentMatrix);
            material.SetVector("_ClipExtents", m_Extents);
        }

        public virtual void OnScroll(PointerEventData eventData)
        {
            if (m_Settling)
                return;

            m_ScrollOffset += eventData.scrollDelta.y * m_ScrollSpeed * Time.deltaTime;
        }

        protected virtual void StartSettling(Action onComplete = null)
        {
            m_Settling = true;

            if (onComplete != null)
                settlingCompleted += onComplete;
        }

        protected virtual void EndSettling()
        {
            m_Settling = false;

            if (settlingCompleted != null)
            {
                settlingCompleted();
                settlingCompleted = null;
            }
        }
    }
}

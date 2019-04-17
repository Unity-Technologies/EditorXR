using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ListView
{
    public abstract class ListViewControllerBase : MonoBehaviour, IScrollHandler
    {
#pragma warning disable 649
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

        [Tooltip("Item template prefabs (at least one is required)")]
        [SerializeField]
        protected GameObject[] m_Templates;

        [SerializeField]
        protected float m_SettleSpeed = 0.4f;

        [SerializeField]
        float m_ScrollSpeed = 0.3f;
#pragma warning restore 649

        event Action settlingCompleted;

        protected bool m_Settling;

        protected Vector3? m_ItemSize;

        protected Vector3 m_StartPosition;

        protected bool m_Scrolling;
        protected float m_ScrollReturn = float.MaxValue;
        protected float m_ScrollDelta;
        protected float m_LastScrollOffset;

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
            get
            {
                if (!m_ItemSize.HasValue && m_Templates.Length > 0)
                    m_ItemSize = GetObjectSize(m_Templates[0]);

                return m_ItemSize ?? Vector3.zero;
            }
        }

        public virtual Vector3 size
        {
            set
            {
                m_Size = value;
                m_Extents = m_Size * 0.5f;
            }
        }

        void Start()
        {
            Setup();
        }

        void Update()
        {
            UpdateView();
        }

        protected abstract void Setup();

        protected virtual void UpdateView()
        {
            ComputeConditions();
            UpdateItems();
        }

        protected virtual void ComputeConditions()
        {
            if (m_Templates.Length > 0) // Use first template to get item size
                m_ItemSize = GetObjectSize(m_Templates[0]);

            var itemSize = m_ItemSize.Value;

            m_StartPosition = (m_Extents.z - itemSize.z * 0.5f) * Vector3.forward;

            if (m_Scrolling)
            {
                m_ScrollDelta = Mathf.Clamp((m_ScrollOffset - m_LastScrollOffset) / Time.deltaTime, -m_MaxMomentum, m_MaxMomentum);
                m_LastScrollOffset = m_ScrollOffset;
            }
            else
            {
                //Apply scrolling momentum
                m_ScrollOffset += m_ScrollDelta * Time.deltaTime;
                const float kScrollMomentumShape = 2f;
                if (m_ScrollReturn < float.MaxValue || m_ScrollOffset > 0)
                    OnScrollEnded();

                if (m_ScrollDelta > 0)
                {
                    m_ScrollDelta -= Mathf.Pow(m_ScrollDamping, kScrollMomentumShape) * Time.deltaTime;
                    if (m_ScrollDelta < 0)
                    {
                        m_ScrollDelta = 0;
                        OnScrollEnded();
                    }
                }
                else if (m_ScrollDelta < 0)
                {
                    m_ScrollDelta += Mathf.Pow(m_ScrollDamping, kScrollMomentumShape) * Time.deltaTime;
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
                m_ScrollReturn = itemSize.z - listHeight + epsilon;
        }

        protected abstract void UpdateItems();

        public virtual void ScrollNext()
        {
            m_ScrollOffset += m_ItemSize.Value.z;
        }

        public virtual void ScrollPrev()
        {
            m_ScrollOffset -= m_ItemSize.Value.z;
        }

        public virtual void ScrollTo(int index)
        {
            m_ScrollOffset = index * itemSize.z;
        }

        protected virtual void UpdateItem(Transform t, int order, float offset, ref bool doneSettling)
        {
            var targetPosition = m_StartPosition + offset * Vector3.back;
            var targetRotation = Quaternion.identity;
            UpdateItemTransform(t, order, targetPosition, targetRotation, false, ref doneSettling);
        }

        protected virtual void UpdateItemTransform(Transform t, int order, Vector3 targetPosition, Quaternion targetRotation, bool dontSettle, ref bool doneSettling)
        {
            if (m_Settling && !dontSettle)
            {
                t.localPosition = Vector3.Lerp(t.localPosition, targetPosition, m_SettleSpeed);
                if (t.localPosition != targetPosition)
                    doneSettling = false;

                t.localRotation = Quaternion.Lerp(t.localRotation, targetRotation, m_SettleSpeed);
                if (t.localRotation != targetRotation)
                    doneSettling = false;
            }
            else
            {
                t.localPosition = targetPosition;
                t.localRotation = targetRotation;
            }

            if (t.GetSiblingIndex() != order)
                t.SetSiblingIndex(order);
        }

        protected virtual Vector3 GetObjectSize(GameObject g)
        {
            var itemSize = Vector3.one;
            var rend = g.GetComponentInChildren<Renderer>();
            if (rend)
            {
                itemSize.x = Vector3.Scale(g.transform.lossyScale, rend.bounds.extents).x * 2 + m_Padding;
                itemSize.y = Vector3.Scale(g.transform.lossyScale, rend.bounds.extents).y * 2 + m_Padding;
                itemSize.z = Vector3.Scale(g.transform.lossyScale, rend.bounds.extents).z * 2 + m_Padding;
            }
            return itemSize;
        }

        public virtual void OnBeginScrolling()
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

            scrollOffset += eventData.scrollDelta.y * scrollSpeed * Time.deltaTime;
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

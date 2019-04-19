using System;
using UnityEngine;

namespace Unity.Labs.ListView
{
    public abstract class ListViewItem<TData, TIndex> : MonoBehaviour, IListViewItem<TData, TIndex> where TData : IListViewItemData<TIndex>
    {
        Transform m_Transform;
        GameObject m_GameObject;

        public Vector3 localPosition
        {
            get { return m_Transform.localPosition; }
            set { m_Transform.localPosition = value; }
        }

        public Quaternion localRotation
        {
            get { return m_Transform.localRotation; }
            set { m_Transform.localRotation = value; }
        }

        public Action<Action> startSettling { get; set; }
        public Action endSettling { get; set; }
        public TData data { get; set; }

        public void SetActive(bool active)
        {
            m_GameObject.SetActive(active);
        }

        public void SetSiblingIndex(int index)
        {
            m_Transform.SetSiblingIndex(index);
        }

        public virtual void Setup(TData datum, bool firstTime = false)
        {
            data = datum;
            if (firstTime)
            {
                m_Transform = transform;
                m_GameObject = gameObject;
            }
        }
    }
}

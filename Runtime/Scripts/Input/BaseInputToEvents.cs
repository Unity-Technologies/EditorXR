using System;
using UnityEngine;

namespace Unity.Labs.EditorXR.Input
{
    abstract class BaseInputToEvents : MonoBehaviour, IInputToEvents
    {
        public bool active
        {
            get { return m_Active; }
            protected set
            {
                if (m_Active != value)
                {
                    m_Active = value;

                    if (activeChanged != null)
                        activeChanged();
                }
            }
        }

        bool m_Active;

        public event Action activeChanged;
    }
}

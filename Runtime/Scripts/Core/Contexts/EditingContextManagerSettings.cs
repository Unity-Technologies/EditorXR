using UnityEngine;

namespace Unity.Labs.EditorXR.Core
{
    class EditingContextManagerSettings : ScriptableObject
    {
        [SerializeField]
        string m_DefaultContextName;

        public string defaultContextName
        {
            get { return m_DefaultContextName; }
            set { m_DefaultContextName = value; }
        }
    }
}

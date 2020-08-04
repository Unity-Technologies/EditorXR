using Unity.XRTools.Utils;
using UnityEngine;
using UnityEngine.InputNew;

namespace Unity.EditorXR.Modules
{
    sealed class MultipleRayInputModuleSettings : ScriptableSettings<MultipleRayInputModuleSettings>
    {
#pragma warning disable 649
        [SerializeField]
        ActionMap m_UIActionMap;
#pragma warning restore 649

        internal ActionMap UIActionMap { get { return m_UIActionMap; } }
    }
}

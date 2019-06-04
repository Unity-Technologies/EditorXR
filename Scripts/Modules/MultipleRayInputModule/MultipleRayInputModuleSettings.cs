using System;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Modules
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

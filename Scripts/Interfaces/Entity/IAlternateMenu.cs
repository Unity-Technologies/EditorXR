#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// An alternate menu that shows on device proxies
    /// </summary>
    public interface IAlternateMenu : IMenu, IUsesRayOrigin
    {
    }
}
#endif

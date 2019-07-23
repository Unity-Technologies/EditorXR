using System;
using Unity.Labs.ModuleLoader;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to UI events
    /// </summary>
    public interface IProvidesUIEvents : IFunctionalityProvider
    {
        event Action<GameObject, RayEventData> rayEntered;
        event Action<GameObject, RayEventData> rayHovering;
        event Action<GameObject, RayEventData> rayExited;
        event Action<GameObject, RayEventData> dragStarted;
        event Action<GameObject, RayEventData> dragEnded;
    }
}

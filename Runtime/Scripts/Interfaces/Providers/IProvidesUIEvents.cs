using System;
using Unity.EditorXR.Modules;
using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to UI events
    /// </summary>
    interface IProvidesUIEvents : IFunctionalityProvider
    {
        event Action<GameObject, RayEventData> rayEntered;
        event Action<GameObject, RayEventData> rayHovering;
        event Action<GameObject, RayEventData> rayExited;
        event Action<GameObject, RayEventData> pointerDown;
        event Action<GameObject, RayEventData> dragStarted;
        event Action<GameObject, RayEventData> dragging;
        event Action<GameObject, RayEventData> dragEnded;
        event Action<GameObject, RayEventData> pointerUp;
        event Action<GameObject, RayEventData> clicked;
    }
}

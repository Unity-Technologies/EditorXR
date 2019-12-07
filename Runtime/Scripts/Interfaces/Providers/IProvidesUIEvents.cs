using System;
using Unity.Labs.EditorXR.Modules;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Provide access to UI events
    /// </summary>
    interface IProvidesUIEvents : IFunctionalityProvider
    {
        event Action<GameObject, RayEventData> rayEntered;
        event Action<GameObject, RayEventData> rayHovering;
        event Action<GameObject, RayEventData> rayExited;
        event Action<GameObject, RayEventData> dragStarted;
        event Action<GameObject, RayEventData> dragEnded;
    }
}

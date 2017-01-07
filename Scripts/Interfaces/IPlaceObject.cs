using System;
using UnityEngine;

/// <summary>
/// Gives decorated class the ability to place objects in the scene, or a MiniWorld
/// </summary>
public interface IPlaceObject
{
    /// <summary>
    /// Delegate used to place objects in the scene/MiniWorld
    /// Transform = Object to place
    /// Vector3 = target scale of placed object
    /// </summary>
    Action<Transform, Vector3> placeObject{ set; }
}
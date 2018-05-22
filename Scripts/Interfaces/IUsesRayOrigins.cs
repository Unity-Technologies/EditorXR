
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Provides access to all ray origins in the system
/// </summary>
interface IUsesRayOrigins
{
    /// <summary>
    /// A list of all ray origins provided by the system
    /// </summary>
    List<Transform> otherRayOrigins { set; }
}


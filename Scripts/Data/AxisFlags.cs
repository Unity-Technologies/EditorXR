
using System;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Used to specify a combination of 3D axes
    /// </summary>
    [Flags]
    public enum AxisFlags
    {
        X = 1 << 0,
        Y = 1 << 1,
        Z = 1 << 2
    }
}


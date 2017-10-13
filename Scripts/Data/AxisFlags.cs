#if UNITY_EDITOR
using System;

namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    /// Which axes are constrained
    /// </summary>
    [Flags]
    public enum AxisFlags
    {
        X = 1 << 0,
        Y = 1 << 1,
        Z = 1 << 2
    }
}
#endif

using System;

namespace Unity.Labs.EditorXR.Interfaces
{
    /// <summary>
    /// Used to specify a combination of 3D axes
    /// </summary>
    [Flags]
    public enum AxisFlags
    {
        /// <summary>
        /// The X axis
        /// </summary>
        X = 1 << 0,

        /// <summary>
        /// The Y axis
        /// </summary>
        Y = 1 << 1,

        /// <summary>
        /// The Z axis
        /// </summary>
        Z = 1 << 2
    }
}

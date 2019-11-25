using System;

namespace Unity.Labs.EditorXR.UI
{
    /// <summary>
    /// What type of interaction is considered a selection:
    /// Direct for when objects intersect with the pointer,
    /// Ray for when the pointer ray hits far-away objects
    /// </summary>
    [Flags]
    enum SelectionFlags
    {
        Ray = 1 << 0,
        Direct = 1 << 1
    }
}

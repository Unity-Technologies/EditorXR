using System;

namespace Unity.EditorXR.Input
{
    interface IInputToEvents
    {
        bool active { get; }
        event Action activeChanged;
    }
}

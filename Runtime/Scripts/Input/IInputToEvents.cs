using System;

namespace Unity.Labs.EditorXR.Input
{
    interface IInputToEvents
    {
        bool active { get; }
        event Action activeChanged;
    }
}

using System;

namespace UnityEngine.Experimental.EditorVR.Input
{
	internal interface IInputToEvents
	{
		bool active { get; }
		event Action activeChanged;
	}
}

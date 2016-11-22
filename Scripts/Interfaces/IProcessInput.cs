using System;
using UnityEngine.InputNew;

public interface IProcessInput
{
	void ProcessInput(ActionMapInput input, Action<InputControl> consumeControl);
}

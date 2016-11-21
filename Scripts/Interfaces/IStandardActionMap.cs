using System;
using UnityEngine.InputNew;

namespace UnityEngine.VR.Tools
{
	public interface IStandardActionMap
	{
		Standard standardInput { set; get; }

		void ProcessInput(Action<InputControl> consumeControl);
	}
}
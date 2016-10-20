using System.Collections.Generic;
using UnityEngine.VR.Actions;

namespace UnityEngine.VR.Tools
{
	public interface IToolActions
	{
		List<IAction> toolActions { get; }
	}
}

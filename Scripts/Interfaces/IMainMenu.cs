using System;
using System.Collections.Generic;

namespace UnityEngine.VR.Tools
{
	public interface IMainMenu
	{
		List<Type> MenuTools { set; }
		Func<IMainMenu, Type, bool> SelectTool { set; }
	}
}

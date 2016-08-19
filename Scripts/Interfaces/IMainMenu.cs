using System;
using System.Collections.Generic;

namespace UnityEngine.VR.Tools
{
	public interface IMainMenu
	{
		List<Type> menuTools { set; }
		List<Type> menuWorkspaces { set; }
		Func<IMainMenu, Type, bool> selectTool { set; }
		Action<Type> createWorkspace { set; }
	}
}
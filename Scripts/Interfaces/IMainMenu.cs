using System;
using System.Collections.Generic;

namespace UnityEngine.VR.Tools
{
	public interface IMainMenu
	{
		List<Type> menuTools { set; }
		Func<IMainMenu, Type, bool> selectTool { set; }
	}
}

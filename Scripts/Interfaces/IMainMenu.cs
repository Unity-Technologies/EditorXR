using System;
using System.Collections.Generic;
using UnityEngine.InputNew;

namespace UnityEngine.VR.Tools
{
    public interface IMainMenu
    {
        Camera eventCamera { get; set; }
        Menu menuActionInput { get; set; }
        Transform menuInputOrigin { get; set; }
        Transform menuOrigin { get; set; }
        List<Type> menuTools { set; }
        Func<IMainMenu, Type, bool> selectTool { set; }
    }
}
#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR;
using UnityEngine;
using UnityEngine.UI;

public interface ISpatialMenuElement
{
    GameObject gameObject { get; }

    bool highlighted { set; }

    bool visible { set; }

    Button button { get; }

    Action <Transform, Action, string, string> Setup { get; }

    Action selected { get; set; }

    Action<SpatialMenu.SpatialMenuData> highlightedAction { get; set; }

    SpatialMenu.SpatialMenuData parentMenuData { get; set; }

    Action correspondingFunction { get; set; }

    Action onHiddenAction { get; set; }
}
#endif

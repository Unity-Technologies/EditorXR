#if UNITY_EDITOR
using System;
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
}
#endif

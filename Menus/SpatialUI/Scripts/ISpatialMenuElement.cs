#if UNITY_EDITOR
using System;
using UnityEngine;

public interface ISpatialMenuElement
{
    GameObject gameObject { get; }

    bool highlighted { set; }

    bool visible { set; }

    Action <Transform, Action, string, string> Setup { get; }

    Action selected { get; set; }
}
#endif

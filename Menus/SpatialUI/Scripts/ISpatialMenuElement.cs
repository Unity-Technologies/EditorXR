using System;
using UnityEngine;

public interface ISpatialMenuElement
{
    GameObject gameObject { get; }

    bool highlighted { set; }

    Action <Transform, Action, string, string> Setup { get; set; }
}
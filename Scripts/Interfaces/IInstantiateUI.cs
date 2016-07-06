using UnityEngine;
using System.Collections;
using System;

public interface IInstantiateUI {

    Func<GameObject, GameObject> InstantiateUI
    {
        set;
    }
}

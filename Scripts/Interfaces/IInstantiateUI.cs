using UnityEngine;
using System.Collections;
using System;

public interface IInstantiateUI {

	Func<GameObject, GameObject> instantiateUI
	{
		set;
	}
}

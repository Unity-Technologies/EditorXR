using System;
using UnityEngine;

public interface IPlaceObject
{
	Action<Transform, Vector3> placeObject{ set; }
}
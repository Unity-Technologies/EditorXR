using System;
using UnityEngine;

public interface IPlaceObjects {
	Action<Transform, Vector3> placeObject{ set; }
}
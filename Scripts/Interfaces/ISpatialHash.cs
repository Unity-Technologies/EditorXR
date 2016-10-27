using System;
using UnityObject = UnityEngine.Object;

public interface ISpatialHash
{
	Action<UnityObject> addObjectToSpatialHash { set; }
	Action<UnityObject> removeObjectFromSpatialHash { set; }
}
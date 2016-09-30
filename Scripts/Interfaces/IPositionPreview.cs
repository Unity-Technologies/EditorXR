using System;
using UnityEngine;

public delegate void PositionPreviewDelegate(Transform preview, Transform rayOrigin, float t = 1f, Quaternion? localRotation = null);

public interface IPositionPreview
{
	PositionPreviewDelegate positionPreview { set; }
	Func<Transform, Transform> getPreviewOriginForRayOrigin { set; }
}
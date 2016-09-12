using UnityEngine;

public delegate void PositionPreviewDelegate(Transform preview, Transform rayOrigin, float t = 1f);
public interface IPositionPreview
{
	PositionPreviewDelegate positionPreview { set; }
}
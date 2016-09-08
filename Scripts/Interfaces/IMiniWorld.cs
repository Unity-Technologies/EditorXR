using UnityEngine;

public interface IMiniWorld
{
	bool IsContainedWithin(Vector3 position);
	Transform referenceTransform { get; }
}
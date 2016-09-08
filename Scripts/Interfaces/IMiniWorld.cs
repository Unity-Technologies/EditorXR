using UnityEngine;

public interface IMiniWorld
{
	Transform miniWorldTransform { get; }
	bool IsContainedWithin(Vector3 position);
	Transform referenceTransform { get; }
}
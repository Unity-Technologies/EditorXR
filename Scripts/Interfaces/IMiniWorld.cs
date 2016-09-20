using UnityEngine;

public interface IMiniWorld
{
	Transform miniWorldTransform { get; }
	bool Contains(Vector3 position);
	Transform referenceTransform { get; }
}
using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.EditorVR.Utilities;

public class ObjectPlacementModule : MonoBehaviour, IUsesSpatialHash
{
	const float kInstantiateFOVDifference = -5f;
	const float kGrowDuration = 0.5f;

	public Action<GameObject> addToSpatialHash { get; set; }
	public Action<GameObject> removeFromSpatialHash { get; set; }

	public void PlaceObject(Transform obj, Vector3 targetScale)
	{
		if (obj.localScale == targetScale)
		{
			// Remove from spatial hash in case the object is already there
			removeFromSpatialHash(obj.gameObject);
			obj.parent = null;
			Selection.activeGameObject = obj.gameObject;
			addToSpatialHash(obj.gameObject);
			return;
		}
		StartCoroutine(PlaceObjectCoroutine(obj, targetScale));
	}

	private IEnumerator PlaceObjectCoroutine(Transform obj, Vector3 targetScale)
	{
		// Don't let us direct select while placing
		removeFromSpatialHash(obj.gameObject);

		float start = Time.realtimeSinceStartup;
		var currTime = 0f;

		obj.parent = null;
		var startScale = obj.localScale;
		var startPosition = obj.position;

		//Get bounds at target scale
		var origScale = obj.localScale;
		obj.localScale = targetScale;
		var bounds = U.Object.GetBounds(obj.gameObject);
		obj.localScale = origScale;

		// We want to position the object so that it fits within the camera perspective at its original scale
		var camera = U.Camera.GetMainCamera();
		var halfAngle = camera.fieldOfView * 0.5f;
		var perspective = halfAngle + kInstantiateFOVDifference;
		var camPosition = camera.transform.position;
		var forward = obj.position - camPosition;

		var distance = bounds.size.magnitude / Mathf.Tan(perspective * Mathf.Deg2Rad);
		var destinationPosition = obj.position;
		if (distance > forward.magnitude)
			destinationPosition = camPosition + forward.normalized * distance;

		while (currTime < kGrowDuration)
		{
			currTime = Time.realtimeSinceStartup - start;
			var t = currTime / kGrowDuration;
			var tSquared = t * t;
			obj.localScale = Vector3.Lerp(startScale, targetScale, tSquared);
			obj.position = Vector3.Lerp(startPosition, destinationPosition, tSquared);
			yield return null;
		}
		obj.localScale = targetScale;
		Selection.activeGameObject = obj.gameObject;

		addToSpatialHash(obj.gameObject);
	}
}
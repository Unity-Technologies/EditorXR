using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEngine.VR.Utilities;

public class ObjectPlacementModule : MonoBehaviour
{
	public delegate void PositionPreviewDelegate(Transform preview, Transform rayOrigin, float t = 1f);

	private const float kInstantiateFOVDifference = 20f;

	private const float kGrowDuration = 0.5f;

	public void PositionPreview(Transform preview, Transform previewOrigin, float t = 1f)
	{
		preview.transform.position = Vector3.Lerp(preview.transform.position, previewOrigin.position, t);
		preview.transform.rotation = Quaternion.Lerp(preview.transform.rotation, previewOrigin.rotation, t);
	}



	private readonly Vector3 kGrabPositionOffset = new Vector3(0f, 0.02f, 0.03f);
	private readonly Quaternion kGrabRotationOffset = Quaternion.AngleAxis(30f, Vector3.left);

	public void PositionPreview(Transform preview, Transform rayOrigin, float t = 1f)
	{
		preview.transform.position = Vector3.Lerp(preview.transform.position, rayOrigin.position + rayOrigin.rotation * kGrabPositionOffset, t);
		preview.transform.rotation = Quaternion.Lerp(preview.transform.rotation, rayOrigin.rotation * kGrabRotationOffset, t);
	}

	public void PlaceObject(Transform obj, Vector3 targetScale)
	{
		StartCoroutine(PlaceObjectCoroutine(obj, targetScale));
	}
	private IEnumerator PlaceObjectCoroutine(Transform obj, Vector3 targetScale)
	{
		float start = Time.realtimeSinceStartup;
		var currTime = 0f;

		obj.parent = null;
		var startScale = obj.localScale;
		var startPosition = obj.position;
		
		var camera = U.Camera.GetMainCamera();
		var perspective = camera.fieldOfView * 0.5f + kInstantiateFOVDifference;
		var camPosition = camera.transform.position;
		var forward = obj.position - camPosition;
		forward.y = 0;

		//Get bounds at target scale
		var origScale = obj.localScale;
		obj.localScale = targetScale;
		var totalBounds = U.Object.GetTotalBounds(obj);
		obj.localScale = origScale;

		if (totalBounds != null)
		{
			var distance = totalBounds.Value.size.magnitude / Mathf.Tan(perspective * Mathf.Deg2Rad);
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
		}
		Selection.activeGameObject = obj.gameObject;
	}
}
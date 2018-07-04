using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

public class RenderCamera : MonoBehaviour
{
    public Camera cam;
	void Update ()
	{
	    var cameraTransform = cam.transform;
	    cameraTransform.localPosition = InputTracking.GetLocalPosition(XRNode.Head);
	    cameraTransform.localRotation = InputTracking.GetLocalRotation(XRNode.Head);
        Handles.DrawCamera2(cam, 0);
	}
}

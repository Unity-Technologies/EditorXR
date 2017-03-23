using UnityEngine;

namespace UnityEditor.Experimental.EditorVR
{
	public delegate void DrawRayDelegate(Vector3 origin, Vector3 direction, Color color, float rayLength = GizmoModule.rayLength);
	public delegate void DrawSphereDelegate(Vector3 center, float radius, Color color);

	public interface IUsesGizmos
	{
		DrawRayDelegate drawRay { set; }
		DrawSphereDelegate drawSphere { set; }
	}
}

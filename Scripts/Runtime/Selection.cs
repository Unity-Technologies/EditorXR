#if !UNITY_EDITOR
using UnityEngine;
using System.Collections;

// Not fully implemented yet; Exists only to allow compilation
public static class Selection
{
	public static GameObject activeGameObject { get; set; }
	public static int activeInstanceID { get; set; }
	public static Transform activeTransform { get; set; }
	public static GameObject[] gameObjects { get; set; }
	public static Object[] objects { get; set; }
	public static Transform[] transforms { get; set; }
}
#endif

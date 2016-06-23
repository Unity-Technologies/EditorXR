using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.InputNew;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor.VR;
#endif

// Useful / Utils

class U {

    public static GameObject ClonePrefab(GameObject prefab, GameObject parent = null) {
        GameObject obj = U.InstantiateAndSetActive(prefab);
        if (parent != null) U.SetParent(obj, parent);
        obj.transform.localPosition = new Vector3();
        return obj;
    }
    public static GameObject ClonePrefabByName(string resource, GameObject parent = null) {
        return U.ClonePrefab(Resources.Load<GameObject>(resource), parent);
    }

    public static float DistanceToCamera(GameObject obj) {
		// from http://forum.unity3d.com/threads/camera-to-object-distance.32643/
		Camera cam = U.GetMainCamera();
		float distance = 0f;
		if (cam)
		{
			Vector3 heading = obj.transform.position - cam.transform.position;
			distance = Vector3.Dot(heading, cam.transform.forward);
		}
        return distance;
    }

    public static float GetSizeForDistanceToCamera(GameObject obj, float minScale, float scaleAt100) {
        float dist = U.DistanceToCamera(obj);
        float scale = U.Map(dist, 0, 100, minScale, scaleAt100);
        if (scale < minScale) scale = minScale;
        return scale;
    }

    public static bool IsInLayer(GameObject o, string s) {
        return o.layer == LayerMask.NameToLayer(s);
    }

    // from http://wiki.unity3d.com/index.php?title=HexConverter
    // Note that Color32 and Color implictly convert to each other. You may pass a Color object to this method without first casting it.
    public static string ColorToHex(Color32 color) {
        string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
        return hex;
    }

    public static Color HexToColor(string hex) {
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        return new Color32(r, g, b, 255);
    }

    public static Color RandomColor() {
        float r = Random.value;
        float g = Random.value;
        float b = Random.value;
        return new Color(r, g, b);
    }

    public static void SetObjectColor(GameObject obj, Color col) {
        Material material = new Material(obj.GetComponent<Renderer>().sharedMaterial);
        material.color = col;
        obj.GetComponent<Renderer>().sharedMaterial = material;
    }

    public static Color GetObjectColor(GameObject obj) {
        return obj.GetComponent<Renderer>().sharedMaterial.color;
    }

    public static void SetObjectAlpha(GameObject obj, float alpha) {
        Color col = GetObjectColor(obj);
        col.a = alpha;
        SetObjectColor(obj, col);
    }

    public static void SetObjectEmissionColor(GameObject obj, Color col) {
        Renderer r = obj.GetComponent<Renderer>();
        if (r) {
            Material material = new Material(r.sharedMaterial);
            if (material.HasProperty("_EmissionColor")) {
                material.SetColor("_EmissionColor", col);
                obj.GetComponent<Renderer>().sharedMaterial = material;
            }
            else {
                U.Destroy(material);
            }
        }
        
    }
    public static Color GetObjectEmissionColor(GameObject obj) {
        Renderer r = obj.GetComponent<Renderer>();
        if (r) {
            Material material = r.sharedMaterial;
            if (material.HasProperty("_EmissionColor")) {
                return material.GetColor("_EmissionColor");
            }
        }
        return Color.white;
    }

    public static void RemoveAllChildren(GameObject obj) {
        var children = new List<GameObject>();
        foreach (Transform child in obj.transform) children.Add(child.gameObject);
        foreach (GameObject child in children) Object.Destroy(child);
    }

    public static void SetParent(GameObject obj, GameObject parent) {
        obj.transform.parent = parent.transform;
    }

    public static void Show(GameObject obj) {
        obj.SetActive(true);
    }
    public static void Hide(GameObject obj) {
        obj.SetActive(false);
    }

    // numbers stuff

    // snaps value to a unit. unit can be any number.
    // for example, with a unit of 0.2, 0.41 -> 0.4, and 0.52 -> 0.6
    public static float SnapValueToUnit(float value, float unit) {
        float mult = value / unit;
        // find lower and upper boundaries of snapping
        int lowerMult = Mathf.FloorToInt(mult);
        int upperMult = Mathf.CeilToInt(mult);
        float lowerBoundary = lowerMult * unit;
        float upperBoundary = upperMult * unit;
        // figure out which is closest
        float diffWithLower = value - lowerBoundary;
        float diffWithHigher = upperBoundary - value;
        return (diffWithLower < diffWithHigher) ? lowerBoundary : upperBoundary;
    }

    public static Vector3 SnapValuesToUnit(Vector3 values, float unit) {
        return new Vector3(U.SnapValueToUnit(values.x, unit),
                           U.SnapValueToUnit(values.y, unit),
                           U.SnapValueToUnit(values.z, unit));
    }

    // calculates easing
    // if snap is zero, no snapping is applied
    public static float Ease(float val, float valEnd, float easeDivider, float snap) {
        val += (valEnd - val) / easeDivider;
        if (snap != 0 && Mathf.Abs(val - valEnd) < snap) val = valEnd;
        return val;
    }

    // Like map in Processing.
    // E1 and S1 must be different, else it will break
    // val, in a, in b, out a, out b
    public static float Map(float val, float ia, float ib, float oa, float ob) {
        return oa + (ob - oa) * ((val - ia) / (ib - ia));
    }
    // Like map, but eases in.
    public static float MapInCubic(float val, float ia, float ib, float oa, float ob) {
        float t = (val - ia);
        float d = (ib - ia);
        t /= d;
        return oa + (ob - oa) * (t) * t * t;
    }
    // Like map, but eases out.
    public static float MapOutCubic(float val, float ia, float ib, float oa, float ob) {
        float t = (val - ia);
        float d = (ib - ia);
        t = (t / d) - 1;
        return oa + (ob - oa) * (t * t * t + 1);
    }
    // Like map, but eases in.
    public static float MapInSin(float val, float ia, float ib, float oa, float ob) {
        return oa + (ob - oa) * (1.0f - Mathf.Cos(((val - ia) / (ib - ia)) * Mathf.PI / 2));
    }

    // from http://wiki.unity3d.com/index.php/3d_Math_functions
    //create a vector of direction "vector" with length "size"
    public static Vector3 SetVectorLength(Vector3 vector, float size) {

        //normalize the vector
        Vector3 vectorNormalized = Vector3.Normalize(vector);

        //scale the vector
        return vectorNormalized *= size;
    }
    // from http://wiki.unity3d.com/index.php/3d_Math_functions
    //Get the intersection between a line and a plane. 
    //If the line and plane are not parallel, the function outputs true, otherwise false.
    public static bool LinePlaneIntersection(out Vector3 intersection, Vector3 linePoint, Vector3 lineVec, Vector3 planeNormal, Vector3 planePoint) {

        float length;
        float dotNumerator;
        float dotDenominator;
        Vector3 vector;
        intersection = Vector3.zero;

        //calculate the distance between the linePoint and the line-plane intersection point
        dotNumerator = Vector3.Dot((planePoint - linePoint), planeNormal);
        dotDenominator = Vector3.Dot(lineVec, planeNormal);

        //line and plane are not parallel
        if (dotDenominator != 0.0f) {
            length = dotNumerator / dotDenominator;

            //create a vector from the linePoint to the intersection point
            vector = SetVectorLength(lineVec, length);

            //get the coordinates of the line-plane intersection point
            intersection = linePoint + vector;

            return true;
        }

        //output not valid
        else {
            return false;
        }
    }
    // from http://wiki.unity3d.com/index.php/3d_Math_functions
    //This function returns a point which is a projection from a point to a line.
    //The line is regarded infinite. If the line is finite, use ProjectPointOnLineSegment() instead.
    public static Vector3 ProjectPointOnLine(Vector3 linePoint, Vector3 lineVec, Vector3 point) {
        //get vector from point on line to point in space
        Vector3 linePointToPoint = point - linePoint;
        float t = Vector3.Dot(linePointToPoint, lineVec);
        return linePoint + lineVec * t;
    }

	public static void Destroy(Object o, float t = 0f)
	{
		if (Application.isPlaying)
		{
			Object.Destroy(o, t);
		}
		else
		{
			if (Mathf.Approximately(t, 0f))
				Object.DestroyImmediate(o);
			else
			{	
                EditorVRView.StartCoroutine(DestroyInSeconds(o, t));
			}			
		}
	}

	private static IEnumerator DestroyInSeconds(Object o, float t)
	{
		float startTime = Time.realtimeSinceStartup;
		while (Time.realtimeSinceStartup <= startTime + t)
			yield return null;

		Object.DestroyImmediate(o);
	}

	/// <summary>
	/// Get a material clone; IMPORTANT: Make sure to call U.Destroy() on this material when done!
	/// </summary>
	/// <param name="renderer"></param>
	/// <returns>Material</returns>
	public static Material GetMaterialClone(Renderer renderer)
	{
		// The following is equivalent to renderer.material, but gets rid of the error messages in edit mode
		return renderer.material = Object.Instantiate(renderer.sharedMaterial);
	}

	public static Camera GetMainCamera()
	{
		Camera camera = Camera.main;
#if UNITY_EDITOR
		if (!Application.isPlaying && EditorVRView.viewerCamera)
		{
			camera = EditorVRView.viewerCamera;
		}
#endif

		return camera;
	}

	public static Transform GetViewerPivot()
	{
		Transform pivot = Camera.main ? Camera.main.transform.parent : null;
#if UNITY_EDITOR
		if (!Application.isPlaying)
		{
			if (EditorVRView.viewerCamera)
				pivot = EditorVRView.viewerCamera.transform.parent;
		}
#endif
		return pivot;
	}

	public static GameObject InstantiateAndSetActive(GameObject prefab, Transform parent = null, bool worldPositionStays = true, bool runInEditMode = true)
	{
		GameObject go = Object.Instantiate<GameObject>(prefab);
		go.transform.SetParent(parent, worldPositionStays);
		go.SetActive(true);
#if UNITY_EDITOR
		if (!Application.isPlaying && runInEditMode)
		{
			U.SetRunInEditModeRecursively(go, runInEditMode);
			go.hideFlags = EditorVR.kDefaultHideFlags;
		}
#endif
		return go;
	}

	public static T CreateGameObjectWithComponent<T>(Transform parent = null) where T : MonoBehaviour
	{
	    return (T) CreateGameObjectWithComponent(typeof(T), parent);
	}

    public static Component CreateGameObjectWithComponent(Type type, Transform parent = null)
    {
#if UNITY_EDITOR
        Component component = EditorUtility.CreateGameObjectWithHideFlags(type.Name, EditorVR.kDefaultHideFlags, type).GetComponent(type);
        if (!Application.isPlaying)
            SetRunInEditModeRecursively(component.gameObject, true);
#else
        Component component = new GameObject(type.Name).AddComponent(type);
#endif
        component.transform.parent = parent;
        return component;
    }

    public static void SetLayerRecursively(GameObject root, int layer)
	{
		Transform[] transforms = root.GetComponentsInChildren<Transform>();
		for (int i = 0; i < transforms.Length; i++)
			transforms[i].gameObject.layer = layer;
	}

	public static Bounds GetBounds(GameObject obj)
	{
		Bounds b = new Bounds(obj.transform.position, Vector3.zero);
		Renderer[] childrenR = obj.GetComponentsInChildren<Renderer>();
		foreach (Renderer childR in childrenR)
		{
			b.Encapsulate(childR.bounds);
		}
		return b;
	}

	public static void SetRunInEditModeRecursively(GameObject go, bool enabled)
	{
		MonoBehaviour[] monoBehaviours = go.GetComponents<MonoBehaviour>();
		foreach (MonoBehaviour mb in monoBehaviours)
		{
			mb.runInEditMode = enabled;
		}

		foreach (Transform child in go.transform)
		{
			SetRunInEditModeRecursively(child.gameObject, enabled);
		}
	}

	public static bool IsEditModeActive(MonoBehaviour mb)
	{
		return !Application.isPlaying && mb.runInEditMode;
    }

    public static T AddComponent<T>(GameObject go) where T : Component
    {
        return (T)AddComponent(typeof(T), go);
    }

    public static Component AddComponent(Type type, GameObject go)
    {
        Component component = go.AddComponent(type);
        SetRunInEditModeRecursively(go, true);
        return component;
    }

    public static IEnumerable<Type> GetImplementationsOfInterface(Type type)
    {
        if (type.IsInterface)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && !p.IsInterface);
        }
        return new List<Type>();
    }

	public static HashSet<InputDevice> CollectInputDevicesFromActionMaps(List<ActionMap> maps)
	{
		var inputDevices = new HashSet<InputDevice>();
		var systemDevices = InputSystem.devices;

		foreach (var map in maps)
		{
			foreach (var scheme in map.controlSchemes)
			{
				foreach (var deviceType in scheme.serializableDeviceTypes)
				{
					foreach (var systemDevice in systemDevices)
					{
						if (systemDevice.GetType() == deviceType.value &&
							(deviceType.TagIndex == -1 || deviceType.TagIndex == systemDevice.TagIndex))
						{
							inputDevices.Add(systemDevice);
						}
					}
				}
			}	
		}
		return inputDevices;
	}

	public static void CollectSerializableTypesFromActionMapInput(ActionMapInput actionMapInput, ref HashSet<SerializableType> types)
	{
		foreach (var deviceType in actionMapInput.controlScheme.serializableDeviceTypes)
		{
			types.Add(deviceType);
		}	
	}


	public static GameObject SpawnGhostWireframe(GameObject obj, Material ghostMaterial, bool enableRenderers = true)
	{
		// spawn ghost
		GameObject ghostObj = U.InstantiateAndSetActive(obj, obj.transform.parent);
		// generate wireframe for objects in tree containing renderers
		Renderer[] children = ghostObj.GetComponentsInChildren<Renderer>();
		foreach (Renderer r in children)
		{
			generateWireframe(r, ghostMaterial);
			r.enabled = enableRenderers;
		}
		ghostObj.transform.position = obj.transform.position;
		ghostObj.transform.rotation = obj.transform.rotation;
		ghostObj.transform.localScale = obj.transform.localScale;

		// remove colliders if there are any
		Collider[] colliders = ghostObj.GetComponents<Collider>();
		foreach (Collider c in colliders)
			U.Destroy(c);

		return ghostObj;
	}

	// generates wireframe if contains a renderer 
	private static void generateWireframe(Renderer r, Material ghostMaterial)
	{
		if (r)
		{
			Material[] materials = r.sharedMaterials;
			for (int i = 0; i < materials.Length; i++)
				materials[i] = ghostMaterial;
			r.sharedMaterials = materials;

			// generate wireframe
			MeshFilter mf = r.GetComponent<MeshFilter>();
			if (mf)
			{
                // TODO: Replace with new wireframe generator
                //Mesh mesh = mf.sharedMesh;
                // mf.mesh = WireframeGenerator.Generate(ref mesh, WIRE_INSIDE.Color);                
            }
        }
	}
}
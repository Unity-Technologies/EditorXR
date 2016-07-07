namespace UnityEditor.VR.Utilities
{
    using System;
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine.InputNew;
    using UObject = UnityEngine.Object;
    using Random = UnityEngine.Random;
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.VR;
#endif

    /// <summary>
    /// EditorVR Utilities
    /// </summary>
    public partial class U
    {
        /// <summary>
        /// Object related EditorVR utilities
        /// </summary>
        public class Object
        {
            public static GameObject ClonePrefab(GameObject prefab, GameObject parent = null)
            {
                GameObject obj = InstantiateAndSetActive(prefab);
                if (parent != null) SetParent(obj, parent);
                obj.transform.localPosition = new Vector3();
                return obj;
            }

            public static GameObject ClonePrefabByName(string resource, GameObject parent = null)
            {
                return ClonePrefab(Resources.Load<GameObject>(resource), parent);
            }

            public static GameObject InstantiateAndSetActive(GameObject prefab, Transform parent = null, bool worldPositionStays = true, bool runInEditMode = true)
            {
                GameObject go = UObject.Instantiate(prefab);
                go.transform.SetParent(parent, worldPositionStays);
                go.SetActive(true);
#if UNITY_EDITOR
                if (!Application.isPlaying && runInEditMode)
                {
                    SetRunInEditModeRecursively(go, runInEditMode);
                    go.hideFlags = EditorVR.kDefaultHideFlags;
                }
#endif
                return go;
            }

            public static void SetParent(GameObject obj, GameObject parent)
            {
                obj.transform.parent = parent.transform;
            }

            public static void Show(GameObject obj)
            {
                obj.SetActive(true);
            }

            public static void Hide(GameObject obj)
            {
                obj.SetActive(false);
            }

            public static void RemoveAllChildren(GameObject obj)
            {
                var children = new List<GameObject>();
                foreach (Transform child in obj.transform) children.Add(child.gameObject);
                foreach (GameObject child in children) UObject.Destroy(child);
            }

            public static bool IsInLayer(GameObject o, string s)
            {
                return o.layer == LayerMask.NameToLayer(s);
            }

            public static T CreateGameObjectWithComponent<T>(Transform parent = null) where T : MonoBehaviour
            {
                return (T)CreateGameObjectWithComponent(typeof(T), parent);
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
                    if (mb)
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
                        .Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);
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

            public static void Destroy(UObject o, float t = 0f)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(o, t);
                }
                else
                {
                    if (Mathf.Approximately(t, 0f))
                        UObject.DestroyImmediate(o);
                    else
                    {
                        EditorVRView.StartCoroutine(DestroyInSeconds(o, t));
                    }
                }
            }

            private static IEnumerator DestroyInSeconds(UObject o, float t)
            {
                float startTime = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup <= startTime + t)
                    yield return null;

                UObject.DestroyImmediate(o);
            }
            
            /// <summary>
            /// Get a material clone; IMPORTANT: Make sure to call U.Destroy() on this material when done!
            /// </summary>
            /// <param name="renderer"></param>
            /// <returns>Material</returns>
            public static Material GetMaterialClone(Renderer renderer)
            {
                // The following is equivalent to renderer.material, but gets rid of the error messages in edit mode
                return renderer.material = UObject.Instantiate(renderer.sharedMaterial);
            }

            // from http://wiki.unity3d.com/index.php?title=HexConverter
            // Note that Color32 and Color implictly convert to each other. You may pass a Color object to this method without first casting it.
            public static string ColorToHex(Color32 color)
            {
                string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
                return hex;
            }

            public static Color HexToColor(string hex)
            {
                byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                return new Color32(r, g, b, 255);
            }

            public static Color RandomColor()
            {
                float r = Random.value;
                float g = Random.value;
                float b = Random.value;
                return new Color(r, g, b);
            }

            public static void SetObjectColor(GameObject obj, Color col)
            {
                Material material = new Material(obj.GetComponent<Renderer>().sharedMaterial);
                material.color = col;
                obj.GetComponent<Renderer>().sharedMaterial = material;
            }

            public static Color GetObjectColor(GameObject obj)
            {
                return obj.GetComponent<Renderer>().sharedMaterial.color;
            }

            public static void SetObjectAlpha(GameObject obj, float alpha)
            {
                Color col = GetObjectColor(obj);
                col.a = alpha;
                SetObjectColor(obj, col);
            }

            public static void SetObjectEmissionColor(GameObject obj, Color col)
            {
                Renderer r = obj.GetComponent<Renderer>();
                if (r)
                {
                    Material material = new Material(r.sharedMaterial);
                    if (material.HasProperty("_EmissionColor"))
                    {
                        material.SetColor("_EmissionColor", col);
                        obj.GetComponent<Renderer>().sharedMaterial = material;
                    }
                    else
                    {
                        Destroy(material);
                    }
                }

            }
            public static Color GetObjectEmissionColor(GameObject obj)
            {
                Renderer r = obj.GetComponent<Renderer>();
                if (r)
                {
                    Material material = r.sharedMaterial;
                    if (material.HasProperty("_EmissionColor"))
                    {
                        return material.GetColor("_EmissionColor");
                    }
                }
                return Color.white;
            }
            
            public static GameObject SpawnGhostWireframe(GameObject obj, Material ghostMaterial, bool enableRenderers = true)
            {
                // spawn ghost
                GameObject ghostObj = InstantiateAndSetActive(obj, obj.transform.parent);
                // generate wireframe for objects in tree containing renderers
                Renderer[] children = ghostObj.GetComponentsInChildren<Renderer>();
                foreach (Renderer r in children)
                {
                    GenerateWireframe(r, ghostMaterial);
                    r.enabled = enableRenderers;
                }
                ghostObj.transform.position = obj.transform.position;
                ghostObj.transform.rotation = obj.transform.rotation;
                ghostObj.transform.localScale = obj.transform.localScale;

                // remove colliders if there are any
                Collider[] colliders = ghostObj.GetComponents<Collider>();
                foreach (Collider c in colliders)
                    Destroy(c);

                return ghostObj;
            }

            // generates wireframe if contains a renderer 
            private static void GenerateWireframe(Renderer r, Material ghostMaterial)
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
    }
}
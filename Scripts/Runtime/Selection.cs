#if !UNITY_EDITOR
using System.Linq;
using UnityEngine;

// Not fully implemented yet; Exists only to allow compilation
public static class Selection
{
    public static Object activeObject
    {
        get {return s_Objects != null && s_Objects.Length > 0 ? s_Objects[0] : null; }
        set { s_Objects = value ? new[] { value } : null; }
    }

    public static GameObject activeGameObject
    {
        get {return s_Objects != null && s_Objects.Length > 0 ? s_Objects[0] as GameObject : null; }
        set { s_Objects = value ? new[] { value } : null; }
    }

    public static int activeInstanceID
    {
        get
        {
            var ao = activeObject;
            return ao ? ao.GetInstanceID() : -1;
        }
    }

    public static Transform activeTransform
    {
        get
        {
            var go = activeGameObject;
            return go ? go.transform : null;
        }
    }

    public static GameObject[] gameObjects
    {
        get
        {
            return s_Objects != null ? s_Objects.Where(o => o as GameObject).Select(o => (GameObject)o).ToArray()
                : new GameObject[0];
        }
    }

    public static Object[] objects
    {
        get { return s_Objects ?? new Object[0]; }
        set { s_Objects = value; }
    }

    public static Transform[] transforms
    {
        get
        {
            return s_Objects != null ? s_Objects.Where(o => o as GameObject).Select(o => ((GameObject)o).transform).ToArray()
                : new Transform[0];
        }
    }


    static Object[] s_Objects;
}
#endif

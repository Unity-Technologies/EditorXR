#if !UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    public static class Selection
    {
        static Object[] s_Objects;

        public static event Action selectionChanged;

        public static Object activeObject
        {
            get { return s_Objects != null && s_Objects.Length > 0 ? s_Objects[0] : null; }
            set { s_Objects = value ? new[] { value } : null; }
        }

        public static GameObject activeGameObject
        {
            get { return s_Objects != null && s_Objects.Length > 0 ? s_Objects[0] as GameObject : null; }
            set
            {
                var oldObjects = s_Objects;
                s_Objects = value ? new[] { value } : null;
                CheckSelectionChanged(oldObjects);
            }
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
            set
            {
                var oldObjects = s_Objects;
                s_Objects = value ? new[] { value.gameObject } : null;
                CheckSelectionChanged(oldObjects);
            }
        }

        public static GameObject[] gameObjects
        {
            get
            {
                return s_Objects != null
                    ? s_Objects.Where(o => o as GameObject).Select(o => (GameObject)o).ToArray()
                    : new GameObject[0];
            }
        }

        public static Object[] objects
        {
            get { return s_Objects ?? new Object[0]; }
            set
            {
                var oldObjects = s_Objects;
                s_Objects = value;
                CheckSelectionChanged(oldObjects);
            }
        }

        public static Transform[] transforms
        {
            get
            {
                return s_Objects != null
                    ? s_Objects.Where(o => o as GameObject).Select(o => ((GameObject)o).transform).ToArray()
                    : new Transform[0];
            }
        }

        static void CheckSelectionChanged(Object[] oldObjects)
        {
            if (s_Objects == null)
            {
                if (oldObjects == null)
                    return;

                if (selectionChanged != null)
                    selectionChanged();

                return;
            }

            if (oldObjects == null)
            {
                if (selectionChanged != null)
                    selectionChanged();

                return;
            }

            var length = s_Objects.Length;
            if (length != oldObjects.Length)
            {
                if (selectionChanged != null)
                    selectionChanged();

                return;
            }

            for (int i = 0; i < length; i++)
            {
                if (s_Objects[i] != oldObjects[i])
                {
                    if (selectionChanged != null)
                        selectionChanged();

                    return;
                }
            }
        }
    }
}
#endif

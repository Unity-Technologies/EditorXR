
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Utilities
{
    /// <summary>
    /// Special utility class for getting components in the editor without allocations
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class ComponentUtils<T>
    {
        static readonly List<T> k_RetrievalList = new List<T>();

        public static T GetComponent(GameObject go)
        {
            var foundComponent = default(T);
            go.GetComponents(k_RetrievalList);
            if (k_RetrievalList.Count > 0)
            {
                foundComponent = k_RetrievalList[0];
                k_RetrievalList.Clear();
            }

            return foundComponent;
        }
    }

    public static class ComponentUtils
    {
        public static T GetOrAddIf<T>(GameObject go, bool option) where T : Component
        {
            T component = go.GetComponent<T>();
#if UNITY_EDITOR
            if (option && component == null)
                component = Undo.AddComponent<T>(go);
#endif
            
            return component;
        }
    }
}



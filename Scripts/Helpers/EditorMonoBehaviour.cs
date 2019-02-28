using System.Collections;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Helpers
{
    /// <summary>
    /// Used for launching co-routines
    /// </summary>
    sealed class EditorMonoBehaviour : MonoBehaviour
    {
        public static EditorMonoBehaviour instance;

        void Awake()
        {
            instance = this;
        }

        public static Coroutine StartEditorCoroutine(IEnumerator routine)
        {
            // Avoid null-coalescing operator for UnityObject
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (instance)
                return instance.StartCoroutine(routine);

            return null;
        }
    }
}

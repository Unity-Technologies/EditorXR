#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    /// <summary>
    /// Implementors can expose visibility callbacks to UI components when they are added to the same GameObject
    /// </summary>
    public interface IWillRender
    {
        /// <summary>
        /// The RectTransform that represents this object
        /// </summary>
        RectTransform rectTransform { get; }

        /// <summary>
        /// Called when the object becomes visible
        /// </summary>
        void OnBecameVisible();

        /// <summary>
        /// Called when the object becomes invisible
        /// </summary>
        void OnBecameInvisible();
    }
}
#endif

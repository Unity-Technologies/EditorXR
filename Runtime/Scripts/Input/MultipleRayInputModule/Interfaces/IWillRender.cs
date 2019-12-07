using System;
using UnityEngine;

namespace Unity.Labs.EditorXR.Modules
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
        /// An action supplied to implementors which allows them to remove themselves from the visible list in case they are pooled
        /// </summary>
        Action<IWillRender> removeSelf { set; }

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

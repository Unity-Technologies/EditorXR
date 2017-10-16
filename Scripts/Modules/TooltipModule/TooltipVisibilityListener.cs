#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Modules
{
    public class TooltipVisibilityListener : MonoBehaviour
    {
        public event Action becameVisible;

        void OnBecameVisible()
        {
            if (becameVisible != null)
                becameVisible();
        }
    }
}
#endif

using System;

namespace UnityEditor.Experimental.EditorVR
{
    sealed class RequiresLayerAttribute : Attribute
    {
        public string layer;

        public RequiresLayerAttribute(string layer)
        {
            this.layer = layer;
        }
    }
}

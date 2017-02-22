using System;

namespace UnityEditor.Experimental.EditorVR
{
	internal sealed class RequiresLayerAttribute : Attribute
	{
		public string layer;

		public RequiresLayerAttribute(string layer)
		{
			this.layer = layer;
		}
	}
}
using System;

namespace UnityEngine.Experimental.EditorVR
{
	public class RequiresLayerAttribute : Attribute
	{
		public string layer;

		public RequiresLayerAttribute(string layer)
		{
			this.layer = layer;
		}
	}
}
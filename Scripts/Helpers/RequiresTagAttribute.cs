using System;

namespace UnityEngine.Experimental.EditorVR
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	internal class RequiresTagAttribute : Attribute
	{
		public string tag;

		public RequiresTagAttribute(string tag)
		{
			this.tag = tag;
		}
	}
}
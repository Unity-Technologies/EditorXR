using System;

namespace UnityEditor.Experimental.EditorVR
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	internal sealed class RequiresTagAttribute : Attribute
	{
		public string tag;

		public RequiresTagAttribute(string tag)
		{
			this.tag = tag;
		}
	}
}
using System;

namespace UnityEngine.Experimental.EditorVR
{
	/// <summary>
	/// Decorates classes with a Tooltip string
	/// </summary>
	public class TooltipAttribute : Attribute
	{
		public string text;

		public TooltipAttribute(string text)
		{
			this.text = text;
		}
	}
}

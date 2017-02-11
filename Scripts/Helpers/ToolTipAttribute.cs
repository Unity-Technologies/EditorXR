using System;

namespace UnityEngine.Experimental.EditorVR
{
	/// <summary>
	/// Decorates classes with a Tooltip string
	/// </summary>
	public class TooltipAttribute : Attribute
	{
		internal string text { get; private set; }

		public TooltipAttribute(string text)
		{
			this.text = text;
		}
	}
}

using System;

namespace UnityEngine.Experimental.EditorVR
{
	public class TooltipAttribute : Attribute
	{
		public string text;

		public TooltipAttribute(string text)
		{
			this.text = text;
		}
	}
}

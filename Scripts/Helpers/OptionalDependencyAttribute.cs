using System;
using System.Diagnostics;

namespace UnityEngine.Experimental.EditorVR
{
	[Conditional("UNITY_CCU")]
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	public class OptionalDependencyAttribute : Attribute
	{
		public string dependentClass;
		public string define;

		public OptionalDependencyAttribute(string dependentClass, string define)
		{
			this.dependentClass = dependentClass;
			this.define = define;
		}
	}
}

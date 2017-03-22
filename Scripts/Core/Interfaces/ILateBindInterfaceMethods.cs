using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	public interface ILateBindInterfaceMethods<T> where T : class
	{
		void LateBindInterfaceMethods(T provider);
	}
}

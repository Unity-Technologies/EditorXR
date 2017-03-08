using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	delegate void BindDelegate();
	
	public interface IBinding<T> where T : class
	{
		void Bind();
		void ConnectInterface(T obj, Transform rayOrigin = null);
	}
}

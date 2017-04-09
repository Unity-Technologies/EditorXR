#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	[CreateAssetMenu(menuName = "EditorVR/Empty Context")]
	class EmptyContext : ScriptableObject, IEditingContext
	{
		public void Setup()
		{
		}

		public void Teardown()
		{
		}

		public void OnSuspendContext()
		{
		}

		public void OnResumeContext()
		{
		}
	}
}
#endif

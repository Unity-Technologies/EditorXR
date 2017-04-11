#if UNITY_EDITOR
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.EditorVR.Core
{
	class EditingContextManagerSettings : ScriptableObject
	{
		[SerializeField]
		string m_DefaultContextName;

		public string defaultContextName
		{
			get { return m_DefaultContextName; }
			set { m_DefaultContextName = value; }
		}
	}
}
#endif

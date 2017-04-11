#if UNITY_EDITOR && UNITY_EDITORVR
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	[CreateAssetMenu(menuName = "EditorVR/EditorVR Context")]
	class EditorVRContext : ScriptableObject, IEditingContext
	{
		[SerializeField]
		List<MonoScript> m_DefaultToolStack;

		EditorVR m_Instance;

		public void Setup()
		{
			EditorVR.defaultTools = m_DefaultToolStack.Select(ms => ms.GetClass()).ToArray();
			m_Instance = ObjectUtils.CreateGameObjectWithComponent<EditorVR>();
		}

		public void Dispose()
		{
			m_Instance.Shutdown(); // Give a chance for dependent systems (e.g. serialization) to shut-down before destroying
			ObjectUtils.Destroy(m_Instance.gameObject);
			m_Instance = null;
		}
	}
}
#endif

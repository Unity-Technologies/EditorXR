#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Experimental.EditorVR.Helpers;
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
			m_Instance = ObjectUtils.CreateGameObjectWithComponent<EditorVR>();
		}

		public void Teardown()
		{
			m_Instance.Shutdown(); // Give a chance for dependent systems (e.g. serialization) to shut-down before destroying
			ObjectUtils.Destroy(m_Instance.gameObject);
			m_Instance = null;
		}

		public void OnSuspendContext()
		{
			m_Instance.gameObject.SetActive(false);
		}

		public void OnResumeContext()
		{
			m_Instance.gameObject.SetActive(true);
		}
	}
}
#endif

#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputNew;
using UnityEngine.Serialization;

namespace UnityEditor.Experimental.EditorVR.Core
{
	[CreateAssetMenu(menuName = "EditorVR/EXR Proxy Affordance Map", fileName = "NewProxyAffordanceMap.asset")]
	public class ProxyAffordanceMap : ScriptableObject
	{
		// TODO REMOVE - 5.6 HACK that remedies items not appearing in the create menu
#if UNITY_EDITOR
		[MenuItem("Assets/Create/EditorVR/EditorVR Proxy Affordance Map")]
		public static void Create()
		{
			var path = AssetDatabase.GetAssetPath(Selection.activeObject);

			if (string.IsNullOrEmpty(path))
				path = "Assets";

			if (!Directory.Exists(path))
				path = Path.GetDirectoryName(path);

			var affordanceMap = ScriptableObject.CreateInstance<ProxyAffordanceMap>();
			path = AssetDatabase.GenerateUniqueAssetPath(path + "/NewProxyAffordanceMap.asset");
			AssetDatabase.CreateAsset(affordanceMap, path);
		}
#endif

		public enum VisibilityControlType
		{
			colorProperty,
			//alphaProperty, // TODO: Support
			//materialSwap // TODO: Support
		}

		[Serializable]
		public class ButtonObject
		{
			[SerializeField]
			VRInputDevice.VRControl m_Control;

			[SerializeField]
			VisibilityControlType m_VisibilityType;

			//TODO Expose each visibility control type field set based on the selected visibility control type

			[SerializeField] // colorProperty field
			string m_ColorVisibilityProperty; // Consider custom inspector that only displays this if "shaderProperty" is chosen

			//[SerializeField] // alphaProperty field
			//string m_AlphaVisibilityProperty; // Consider custom inspector that only displays this if "shaderProperty" is chosen

			//[SerializeField] // materialSwap field
			//Material m_SwapMaterial;

			//[SerializeField]
			//float m_PropertyHiddenValue;

			public VRInputDevice.VRControl control { get { return m_Control; } }
			public VisibilityControlType visibilityControlType { get { return m_VisibilityType; } }
			public string colorVisibilityProperty { get { return m_ColorVisibilityProperty; } }
			//public string alphaVisibilityProperty { get { return m_AlphaVisibilityProperty; } }
			//public Material swapMaterial { get { return m_SwapMaterial; } }
		}

		[SerializeField]
		VisibilityControlType m_BodyVisibilityType;

		[SerializeField]
		[FormerlySerializedAs("m_Buttons")]
		ButtonObject[] m_AffordanceDefinitions;

		public ButtonObject[] AffordanceDefinitions { get { return m_AffordanceDefinitions; } }
		public VisibilityControlType BodyVisibilityControlType { get { return m_BodyVisibilityType; } }
	}
}
#endif

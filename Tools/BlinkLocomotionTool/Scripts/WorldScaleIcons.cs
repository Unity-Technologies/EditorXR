using System.IO;
using UnityEditor;
using UnityEngine;

public class WorldScaleIcons : ScriptableObject
{
#if UNITY_EDITOR
	[MenuItem("Assets/Create/ScriptableObjects/WorldScaleIcons")]
	public static void Create()
	{
		var path = AssetDatabase.GetAssetPath(Selection.activeObject);

		if (string.IsNullOrEmpty(path))
			path = "Assets";

		if (!Directory.Exists(path))
			path = Path.GetDirectoryName(path);

		var proxyExtras = CreateInstance<WorldScaleIcons>();
		path = AssetDatabase.GenerateUniqueAssetPath(path + "/WorldScaleIcons.asset");
		AssetDatabase.CreateAsset(proxyExtras, path);
	}
#endif

	public Sprite[] icons { get { return m_Icons; } }

	[SerializeField]
	Sprite[] m_Icons;
}

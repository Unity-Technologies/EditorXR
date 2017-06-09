#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Core
{
	public class CreateHapticPulse
	{
		[MenuItem("Assets/Create/EditorVR/HapticPulse")]
		public static void CreateMyAsset()
		{
			HapticPulse asset = ScriptableObject.CreateInstance<HapticPulse>();

			string path = AssetDatabase.GetAssetPath(Selection.activeObject);
			if (path == string.Empty)
				path = "Assets";
			else if (Path.GetExtension(path) != string.Empty)
				path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), string.Empty);

			string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/NewHapticPulse.asset");

			AssetDatabase.CreateAsset(asset, assetPathAndName);
			AssetDatabase.SaveAssets();
			EditorUtility.FocusProjectWindow();
			Selection.activeObject = asset;
		}
	}
}
#endif

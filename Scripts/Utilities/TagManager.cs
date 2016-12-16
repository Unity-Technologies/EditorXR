using UnityEditor;

namespace UnityEngine.Experimental.EditorVR.Utilities
{
	public static class TagManager
	{
		const int kMaxLayer = 31;
		const int kMinLayer = 8;

		/// <summary>
		/// Add a tag to the tag manager if it doesn't already exist
		/// </summary>
		/// <param name="tag">Tag to add</param>
		public static void AddTag(string tag)
		{
			var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
			if ((asset != null) && (asset.Length > 0))
			{
				var so = new SerializedObject(asset[0]);
				var tags = so.FindProperty("tags");

				var found = false;
				for (var i = 0; i < tags.arraySize; i++)
				{
					if (tags.GetArrayElementAtIndex(i).stringValue == tag)
					{
						found = true;
						break;
					}
				}

				if (!found)
				{
					var arraySize = tags.arraySize;
					tags.InsertArrayElementAtIndex(arraySize);
					tags.GetArrayElementAtIndex(arraySize - 1).stringValue = tag;
				}
				so.ApplyModifiedProperties();
				so.Update();
			}
		}

		/// <summary>
		/// Add a layer to the tag manager if it doesn't already exist
		/// Start at layer 31 (max) and work down
		/// </summary>
		/// <param name="layerName"></param>
		public static void AddLayer(string layerName)
		{
			var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
			if ((asset != null) && (asset.Length > 0))
			{
				var so = new SerializedObject(asset[0]);
				var layers = so.FindProperty("layers");
				var found = false;
				for (var i = 0; i < layers.arraySize; i++)
				{
					if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
					{
						found = true;
						break;
					}
				}

				if (!found)
				{
					var added = false;
					for (var i = kMaxLayer; i >= kMinLayer; i--)
					{
						var layer = layers.GetArrayElementAtIndex(i);
						if (!string.IsNullOrEmpty(layer.stringValue))
							continue;

						layer.stringValue = layerName;
						added = true;
						break;
					}

					if (!added)
						Debug.LogWarning("Could not add layer " + layerName + " because there are no free layers");
				}
				so.ApplyModifiedProperties();
				so.Update();
			}
		}
	}
}
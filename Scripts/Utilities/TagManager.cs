using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.EditorVR;
using UnityEngine.Experimental.EditorVR.Utilities;

namespace UnityEditor.Experimental.EditorVR.Utilities
{
#if UNITY_EDITOR
	internal static class TagManager
	{
		const int kMaxLayer = 31;
		const int kMinLayer = 8;

		/// <summary>
		/// Add a tag to the tag manager if it doesn't already exist
		/// </summary>
		/// <param name="tag">Tag to add</param>
		public static void AddTag(string tag)
		{
			SerializedObject so;
			var tags = GetTagManagerProperty("tags", out so);
			if (tags != null)
			{
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
			SerializedObject so;
			var layers = GetTagManagerProperty("layers", out so);
			if (layers != null)
			{
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

		public static SerializedProperty GetTagManagerProperty(string name, out SerializedObject so)
		{
			var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
			if ((asset != null) && (asset.Length > 0))
			{
				so = new SerializedObject(asset[0]);
				return so.FindProperty(name);
			}

			so = null;
			return null;
		}

		public static List<string> GetRequiredTags()
		{
			var requiredTags = new List<string>();
			U.Object.ForEachType(t =>
			{
				var tagAttributes = (RequiresTagAttribute[])t.GetCustomAttributes(typeof(RequiresTagAttribute), true);
				foreach (var attribute in tagAttributes)
					requiredTags.Add(attribute.tag);
			});
			return requiredTags;
		}

		public static List<string> GetRequiredLayers()
		{
			var requiredLayers = new List<string>();
			U.Object.ForEachType(t =>
			{
				var layerAttributes = (RequiresLayerAttribute[])t.GetCustomAttributes(typeof(RequiresLayerAttribute), true);
				foreach (var attribute in layerAttributes)
					requiredLayers.Add(attribute.layer);
			});
			return requiredLayers;
		}
	}
#endif
}

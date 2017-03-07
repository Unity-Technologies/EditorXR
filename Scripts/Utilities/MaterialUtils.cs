#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using UnityMaterial = UnityEngine.Material;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.Experimental.EditorVR.Utilities
{
	/// <summary>
	/// Material related EditorVR utilities
	/// </summary>
	static class MaterialUtils
	{
		/// <summary>
		/// Get a material clone; IMPORTANT: Make sure to call U.Destroy() on this material when done!
		/// </summary>
		/// <param name="renderer">Renderer that will have its material clone and replaced</param>
		/// <returns>Cloned material</returns>
		public static UnityMaterial GetMaterialClone(Renderer renderer)
		{
			// The following is equivalent to renderer.material, but gets rid of the error messages in edit mode
			return renderer.material = UnityObject.Instantiate(renderer.sharedMaterial);
		}

		/// <summary>
		/// Get a material clone; IMPORTANT: Make sure to call U.Destroy() on this material when done!
		/// </summary>
		/// <param name="graphic">Graphic that will have its material cloned and replaced</param>
		/// <returns>Cloned material</returns>
		public static UnityMaterial GetMaterialClone(Graphic graphic)
		{
			// The following is equivalent to graphic.material, but gets rid of the error messages in edit mode
			return graphic.material = UnityObject.Instantiate(graphic.material);
		}

		/// <summary>
		/// Clone all materials within a renderer; IMPORTANT: Make sure to call U.Destroy() on this material when done!
		/// </summary>
		/// <param name="renderer">Renderer that will have its materials cloned and replaced</param>
		/// <returns>Cloned materials</returns>
		public static UnityMaterial[] CloneMaterials(Renderer renderer)
		{
			var sharedMaterials = renderer.sharedMaterials;
			for (var i = 0; i < sharedMaterials.Length; i++)
			{
				sharedMaterials[i] = UnityObject.Instantiate(sharedMaterials[i]);
			}
			renderer.sharedMaterials = sharedMaterials;
			return sharedMaterials;
		}

		// from http://wiki.unity3d.com/index.php?title=HexConverter
		// Note that Color32 and Color implictly convert to each other. You may pass a Color object to this method without first casting it.
		public static string ColorToHex(Color32 color)
		{
			var hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
			return hex;
		}

		public static Color HexToColor(string hex)
		{
			hex = hex.Replace("0x", "").Replace("#", "");
			var r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
			var g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
			var b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
			var a = hex.Length == 8 ? byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) : (byte)255;

			return new Color32(r, g, b, a);
		}

		public static Color RandomColor()
		{
			var r = Random.value;
			var g = Random.value;
			var b = Random.value;

			return new Color(r, g, b);
		}

		public static void SetObjectColor(GameObject obj, Color col)
		{
			var material = new UnityMaterial(obj.GetComponent<Renderer>().sharedMaterial);
			material.color = col;
			obj.GetComponent<Renderer>().sharedMaterial = material;
		}

		public static Color GetObjectColor(GameObject obj)
		{
			return obj.GetComponent<Renderer>().sharedMaterial.color;
		}

		public static void SetObjectAlpha(GameObject obj, float alpha)
		{
			var col = GetObjectColor(obj);
			col.a = alpha;
			SetObjectColor(obj, col);
		}

		public static void SetObjectEmissionColor(GameObject obj, Color col)
		{
			var r = obj.GetComponent<Renderer>();
			if (r)
			{
				var material = new UnityMaterial(r.sharedMaterial);
				if (material.HasProperty("_EmissionColor"))
				{
					material.SetColor("_EmissionColor", col);
					obj.GetComponent<Renderer>().sharedMaterial = material;
				}
				else
				{
					ObjectUtils.Destroy(material);
				}
			}
		}

		public static Color GetObjectEmissionColor(GameObject obj)
		{
			var r = obj.GetComponent<Renderer>();
			if (r)
			{
				var material = r.sharedMaterial;
				if (material.HasProperty("_EmissionColor"))
					return material.GetColor("_EmissionColor");
			}

			return Color.white;
		}
	}
}
#endif

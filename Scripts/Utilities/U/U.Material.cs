namespace UnityEngine.Experimental.EditorVR.Utilities
{
	using UnityEngine;
	using Random = UnityEngine.Random;
	using UMaterial = UnityEngine.Material;
	using UObject = UnityEngine.Object;

	/// <summary>
	/// EditorVR Utilities
	/// </summary>
	public static partial class U
	{
		/// <summary>
		/// Material related EditorVR utilities
		/// </summary>
		public static class Material
		{
			/// <summary>
			/// Get a material clone; IMPORTANT: Make sure to call U.Destroy() on this material when done!
			/// </summary>
			/// <param name="renderer">Renderer that will have its material clone and replaced</param>
			/// <returns>Cloned material</returns>
			public static UMaterial GetMaterialClone(Renderer renderer)
			{
				// The following is equivalent to renderer.material, but gets rid of the error messages in edit mode
				return renderer.material = UObject.Instantiate(renderer.sharedMaterial);
			}

			/// <summary>
			/// Clone all materials within a renderer; IMPORTANT: Make sure to call U.Destroy() on this material when done!
			/// </summary>
			/// <param name="renderer">Renderer that will have its materials cloned and replaced</param>
			/// <returns>Cloned materials</returns>
			public static UMaterial[] CloneMaterials(Renderer renderer)
			{
				var sharedMaterials = renderer.sharedMaterials;
				for (var i = 0; i < sharedMaterials.Length; i++)
				{
					sharedMaterials[i] = UObject.Instantiate(sharedMaterials[i]);
				}
				renderer.sharedMaterials = sharedMaterials;
				return sharedMaterials;
			}

			// from http://wiki.unity3d.com/index.php?title=HexConverter
			// Note that Color32 and Color implictly convert to each other. You may pass a Color object to this method without first casting it.
			public static string ColorToHex(Color32 color)
			{
				string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
				return hex;
			}

			public static Color HexToColor(string hex)
			{
				hex = hex.Replace("0x", "").Replace("#", "");
				byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
				byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
				byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
				byte a = hex.Length == 8 ? byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) : (byte)255;

				return new Color32(r, g, b, a);
			}

			public static Color RandomColor()
			{
				float r = Random.value;
				float g = Random.value;
				float b = Random.value;
				return new Color(r, g, b);
			}

			public static void SetObjectColor(GameObject obj, Color col)
			{
				UMaterial material = new UMaterial(obj.GetComponent<Renderer>().sharedMaterial);
				material.color = col;
				obj.GetComponent<Renderer>().sharedMaterial = material;
			}

			public static Color GetObjectColor(GameObject obj)
			{
				return obj.GetComponent<Renderer>().sharedMaterial.color;
			}

			public static void SetObjectAlpha(GameObject obj, float alpha)
			{
				Color col = GetObjectColor(obj);
				col.a = alpha;
				SetObjectColor(obj, col);
			}

			public static void SetObjectEmissionColor(GameObject obj, Color col)
			{
				Renderer r = obj.GetComponent<Renderer>();
				if (r)
				{
					UMaterial material = new UMaterial(r.sharedMaterial);
					if (material.HasProperty("_EmissionColor"))
					{
						material.SetColor("_EmissionColor", col);
						obj.GetComponent<Renderer>().sharedMaterial = material;
					}
					else
					{
						U.Object.Destroy(material);
					}
				}
			}

			public static Color GetObjectEmissionColor(GameObject obj)
			{
				Renderer r = obj.GetComponent<Renderer>();
				if (r)
				{
					UMaterial material = r.sharedMaterial;
					if (material.HasProperty("_EmissionColor"))
					{
						return material.GetColor("_EmissionColor");
					}
				}
				return Color.white;
			}
		}
	}
}
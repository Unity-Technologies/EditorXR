#if UNITY_EDITOR
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
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
        /// Get a material clone; IMPORTANT: Make sure to call ObjectUtils.Destroy() on this material when done!
        /// </summary>
        /// <param name="renderer">Renderer that will have its material clone and replaced</param>
        /// <returns>Cloned material</returns>
        public static UnityMaterial GetMaterialClone(Renderer renderer)
        {
            // The following is equivalent to renderer.material, but gets rid of the error messages in edit mode
            return renderer.material = UnityObject.Instantiate(renderer.sharedMaterial);
        }

        /// <summary>
        /// Get a material clone; IMPORTANT: Make sure to call ObjectUtils.Destroy() on this material when done!
        /// </summary>
        /// <param name="graphic">Graphic that will have its material cloned and replaced</param>
        /// <returns>Cloned material</returns>
        public static UnityMaterial GetMaterialClone(Graphic graphic)
        {
            // The following is equivalent to graphic.material, but gets rid of the error messages in edit mode
            return graphic.material = UnityObject.Instantiate(graphic.material);
        }

        /// <summary>
        /// Clone all materials within a renderer; IMPORTANT: Make sure to call ObjectUtils.Destroy() on this material when done!
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

        public static Color HexToColor(string hex)
        {
            hex = hex.Replace("0x", "").Replace("#", "");
            var r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
            var a = hex.Length == 8 ? byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber) : (byte)255;

            return new Color32(r, g, b, a);
        }

        public static Color PrefToColor(string pref)
        {
            var split = pref.Split(';');
            if (split.Length != 5)
            {
                Debug.LogError("Parsing PrefColor failed");
                return default(Color);
            }

            split[1] = split[1].Replace(',', '.');
            split[2] = split[2].Replace(',', '.');
            split[3] = split[3].Replace(',', '.');
            split[4] = split[4].Replace(',', '.');
            float r, g, b, a;
            var success = float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out r);
            success &= float.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out g);
            success &= float.TryParse(split[3], NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out b);
            success &= float.TryParse(split[4], NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out a);

            if (success)
                return new Color(r, g, b, a);

            Debug.LogError("Parsing PrefColor failed");
            return default(Color);
        }

        public static Color HueShift(Color color, float shift)
        {
            Vector3 hsv;
            Color.RGBToHSV(color, out hsv.x, out hsv.y, out hsv.z);
            hsv.x = Mathf.Repeat(hsv.x + shift, 1f);
            return Color.HSVToRGB(hsv.x, hsv.y, hsv.z);
        }

        /// <summary>
        /// Add a material to this renderer's shared materials
        /// </summary>
        /// <param name="material">The material to be added</param>
        public static void AddMaterial(this Renderer @this, UnityMaterial material)
        {
            var materials = @this.sharedMaterials;
            var length = materials.Length;
            var newMaterials = new UnityMaterial[length + 1];
            Array.Copy(materials, newMaterials, length);
            newMaterials[length] = material;
            @this.sharedMaterials = newMaterials;
        }
    }
}
#endif

#if UNITY_EDITOR
namespace UnityEditor.Experimental.EditorVR
{
    /// <summary>
    ///
    /// </summary>
    public interface IControlInputIntersection
    {
    }

    public static class IControlInputIntersectionMethods
    {
        internal delegate void PreventInputIntersectionDelegate(IControlInputIntersection caller, bool blockStandardInput = true);

        internal static PreventInputIntersectionDelegate preventStandardInputIntersection { private get; set; }

        /// <summary>
        ///
        /// </summary>
        public static void PreventInputIntersection(this IControlInputIntersection obj, bool blockStandardInput = true)
        {
            preventStandardInputIntersection(obj, blockStandardInput);
        }
    }
}
#endif

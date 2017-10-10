#if UNITY_EDITOR
namespace UnityEditor.Experimental.EditorVR.Core
{
    interface ISerializePreferences
    {
        object OnSerializePreferences();
        void OnDeserializePreferences(object obj);
    }
}
#endif

namespace Unity.EditorXR.Core
{
    interface ISerializePreferences
    {
        object OnSerializePreferences();
        void OnDeserializePreferences(object obj);
    }
}

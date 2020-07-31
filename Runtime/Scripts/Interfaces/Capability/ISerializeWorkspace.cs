namespace Unity.EditorXR
{
    interface ISerializeWorkspace
    {
        object OnSerializeWorkspace();
        void OnDeserializeWorkspace(object obj);
    }
}

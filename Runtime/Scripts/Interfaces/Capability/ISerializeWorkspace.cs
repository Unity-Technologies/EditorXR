namespace Unity.Labs.EditorXR
{
    interface ISerializeWorkspace
    {
        object OnSerializeWorkspace();
        void OnDeserializeWorkspace(object obj);
    }
}

namespace UnityEditor.Experimental.EditorVR
{
    interface ISerializeWorkspace
    {
        object OnSerializeWorkspace();
        void OnDeserializeWorkspace(object obj);
    }
}

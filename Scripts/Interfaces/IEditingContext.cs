#if UNITY_EDITOR
namespace UnityEditor.Experimental.EditorVR
{
	/// <summary>
	/// Implement this interface on a ScriptableObject to create an editing context. You can also specify your own custom
	/// settings in the ScriptableObject to be applied to the specified VR editor (e.g. EditorVR) that you will be using
	/// for the editing context.
	/// 
	/// Editing Contexts are applied to a stack, which means a context can be suspended by a newer context pushed to the top
	/// of the stack.  When a context is popped from the stack, the next context on the stack will be resumed.
	/// </summary>
	public interface IEditingContext
	{
		string name { get; }

		/// <summary>
		/// Perform one-time setup for the context when pushed to the stack.
		/// </summary>
		void Setup();

		/// <summary>
		/// Allow the context to dispose of any created objects when popped from the stack.
		/// </summary>
		void Dispose();

		/// <summary>
		/// Execute cleanup before this context is suspended. You can assume the context will be resumed before it will be destroyed.
		/// </summary>
		void OnSuspendContext();

		/// <summary>
		/// Undo whatever was cleaned up when suspending. You can assume this context was previously suspended.
		/// </summary>
		void OnResumeContext();
	}
}
#endif

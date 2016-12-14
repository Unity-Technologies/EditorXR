namespace UnityEngine.VR.Tools
{
	/// <summary>
	/// Designates a tool as a Transform tool
	/// </summary>
	public interface ITransformer
	{
		/// <summary>
		/// Set to true to hide the manipulator, if there is one
		/// </summary>
		bool hideManipulator { set; }
	}
}
namespace UnityEngine.VR.Actions
{
	/// <summary>
	/// Interface that mandates the properties & methods that must be implemented for EditorVR Actions
	/// </summary>
	public interface IToggleAction : IAction
	{
		string groupName {  get; set; }
	}	
}

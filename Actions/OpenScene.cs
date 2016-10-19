namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("OpenScene", "Assets/EditorVR/Actions/Icons/OpenSceneIcon.png", "Scene")]
	public class OpenScene : MonoBehaviour, IAction
	{
		public bool ExecuteAction()
		{
			Debug.LogError("ExecuteAction Action should open a sub-panel showing available scenes to open, if any are found");

			return true;
		}
	}
}
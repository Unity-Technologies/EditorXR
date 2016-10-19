namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("SaveScene", "Assets/EditorVR/Actions/Icons/SaveSceneIcon.png", "Scene")]
	public class SaveScene : MonoBehaviour, IAction
	{
		public bool ExecuteAction()
		{
			Debug.LogError("ExecuteAction Action should save a scene here");
			return true;
		}
	}
}
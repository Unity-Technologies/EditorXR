namespace UnityEngine.Experimental.EditorVR.Actions
{
	[ActionMenuItem("SaveScene", "Scene")]
	public class SaveScene : BaseAction
	{
		public override void ExecuteAction()
		{
			Debug.LogError("ExecuteAction Action should save a scene here");
		}
	}
}
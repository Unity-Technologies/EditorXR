using UnityEngine.VR.Tools;

namespace UnityEngine.VR.Actions
{
	[VRMenuItem("OpenScene", "Actions", "Open a saved scene")]
	public class OpenScene : MonoBehaviour, IAction
	{
		public Sprite icon { get; set; }

		public void Execute()
		{
			Debug.LogError("Execute Action should open a sub-panel showing available scenes to open, if any are found");
		}
	}
}
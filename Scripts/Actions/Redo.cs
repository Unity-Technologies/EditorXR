using UnityEngine.VR.Tools;

namespace UnityEngine.VR.Actions
{
	[VRMenuItem("Redo", "Actions", "Redo a previously undone action")]
	public class Redo : MonoBehaviour, IAction
	{
		public Sprite icon { get; set; }

		public void Execute()
		{
			Debug.LogError("Execute Action should redo here");
		}
	}
}
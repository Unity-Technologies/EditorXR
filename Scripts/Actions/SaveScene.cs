using UnityEngine.VR.Tools;

namespace UnityEngine.VR.Actions
{
	[VRMenuItem("SaveScene", "Actions", "Save the scene")]
	public class SaveScene : MonoBehaviour, IAction
	{
		public Sprite icon { get; set; }

		public void Execute()
		{
			Debug.LogError("Execute Action should save a scehe here");
		}
	}
}
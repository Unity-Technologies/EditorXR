using UnityEngine.VR.Tools;

namespace UnityEngine.VR.Actions
{
	[VRMenuItem("Clone", "Actions", "Clone selected Object")]
	public class Clone : MonoBehaviour, IAction
	{
		public Sprite icon { get; set; }

		public void Execute()
		{
			Debug.LogError("Execute Action should clone content here");
		}
	}
}
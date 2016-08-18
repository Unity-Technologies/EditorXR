using UnityEngine.VR.Tools;

namespace UnityEngine.VR.Actions
{
	[VRMenuItem("Paste", "Actions", "Paste a copied object")]
	public class Paste : MonoBehaviour, IAction
	{
		public Sprite icon { get; set; }

		public void Execute()
		{
			Debug.LogError("Execute Action should paste content here");
		}
	}
}
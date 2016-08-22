using UnityEngine.VR.Tools;

namespace UnityEngine.VR.Actions
{
	[VRMenuItem("Play", "Actions", "Enable Play-Mode")]
	public class Play : MonoBehaviour, IAction
	{
		[SerializeField]
		private Sprite m_Icon;

		public Sprite icon { get; set; }

		public bool Execute()
		{
			Debug.LogError("Execute Action should play the scene here");

			return true;
		}
	}
}
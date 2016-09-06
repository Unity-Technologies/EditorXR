namespace UnityEngine.VR.Actions
{
	[ActionItemAttribute("Play", "ActionIcons/PlayIcon")]
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
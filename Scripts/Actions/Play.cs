using UnityEditor;

namespace UnityEngine.VR.Actions
{
	[ActionItem("Play", "ActionIcons/PlayIcon")]
	public class Play : MonoBehaviour, IAction
	{
		[SerializeField]
		private Sprite m_Icon;
		public Sprite icon { get; set; }

		public int indexPosition { get; set; }
		public string sectionName { get; set; }

		public bool Execute()
		{
			Debug.LogError("Execute Action should play the scene here");

			// We should handle for resuming EVR when exiting
			EditorApplication.isPlaying = true;

			return true;
		}
	}
}
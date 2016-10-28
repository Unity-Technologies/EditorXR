using UnityEditor;

namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("Play")]
	public class Play : MonoBehaviour, IAction
	{
		public Sprite icon { get { return m_Icon; } }
		[SerializeField]
		private Sprite m_Icon;

		public bool ExecuteAction()
		{
			EditorApplication.isPlaying = true;
			return EditorApplication.isPlayingOrWillChangePlaymode;
		}
	}
}
using UnityEditor;

namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("Play", "Assets/EditorVR/Actions/Icons/PlayIcon.png")]
	public class Play : MonoBehaviour, IAction
	{
		public bool ExecuteAction()
		{
			EditorApplication.isPlaying = true;
			return EditorApplication.isPlayingOrWillChangePlaymode;
		}
	}
}
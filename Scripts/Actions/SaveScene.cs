using UnityEngine.VR.Tools;

namespace UnityEngine.VR.Actions
{
	[ActionItem("SaveScene", "ActionIcons/SaveSceneIcon", "Scene")]
	public class SaveScene : MonoBehaviour, IAction
	{
		[SerializeField]
		private Sprite m_Icon;

		public Sprite icon { get; set; }

		public bool Execute()
		{
			Debug.LogError("Execute Action should save a scehe here");

			return true;
		}
	}
}
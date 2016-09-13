namespace UnityEngine.VR.Actions
{
	[ActionItem("OpenScene", "ActionIcons/OpenSceneIcon", "Scene")]
	public class OpenScene : MonoBehaviour, IAction
	{
		[SerializeField]
		private Sprite m_Icon;
		public Sprite icon { get; set; }

		public int indexPosition { get; set; }
		public string sectionName { get; set; }

		public bool Execute()
		{
			Debug.LogError("Execute Action should open a sub-panel showing available scenes to open, if any are found");

			return true;
		}
	}
}
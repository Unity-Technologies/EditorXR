using UnityEditor;
using UnityEngine.VR.Tools;

namespace UnityEngine.VR.Actions
{
	[ActionItem("Delete", "ActionIcons/DeleteIcon")]
	public class Delete : MonoBehaviour, IAction
	{
		[SerializeField]
		private Sprite m_Icon;

		public Sprite icon { get; set; }

		public bool Execute()
		{
			Debug.LogError("<color=yellow>Attempting to destroy an object</color>");
			var selection = UnityEditor.Selection.GetTransforms(SelectionMode.Editable);
			foreach (var trans in selection)
			{
				Debug.LogError("Destroying selected object : " + trans.name);
				GameObject.DestroyImmediate(trans.gameObject);
			}

			return true;
		}
	}
}
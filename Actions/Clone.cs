using UnityEditor;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("Clone", "Assets/EditorVR/Actions/Icons/CloneIcon.png", ActionMenuItemAttribute.kDefaultActionSectionName, 3)]
	public class Clone : MonoBehaviour, IAction
	{
		public bool ExecuteAction()
		{
			const float range = 4f;
			var selection = Selection.GetTransforms(SelectionMode.Editable);
			foreach (var s in selection)
			{
				var clone = U.Object.Instantiate(s.gameObject);
				Vector3 cloneOffset = new Vector3(s.position.x + Random.Range(-range, range), s.position.y + Random.Range(-range, range), s.position.z + Random.Range(-range, range)) + (Vector3.one * 0.5f);
				clone.transform.position = s.position + cloneOffset;
			}

			return true;
		}
	}
}
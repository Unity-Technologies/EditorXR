using UnityEditor;
using UnityEngine.VR.Utilities;

namespace UnityEngine.VR.Actions
{
	[ActionMenuItem("Clone", ActionMenuItemAttribute.kDefaultActionSectionName, 3)]
	public class Clone : BaseAction
	{
		public override void ExecuteAction()
		{
			const float range = 4f;
			var selection = Selection.GetTransforms(SelectionMode.Editable);
			foreach (var s in selection)
			{
				var clone = U.Object.Instantiate(s.gameObject);
				var cloneOffset = new Vector3(s.position.x + Random.Range(-range, range), 
					s.position.y + Random.Range(-range, range), 
					s.position.z + Random.Range(-range, range)) + (Vector3.one * 0.5f);
				clone.transform.position = s.position + cloneOffset;
			}
		}
	}
}
using UnityEngine;
using UnityEngine.VR.Handles;
using Button = UnityEngine.VR.UI.Button;

public class InspectorArrayHeaderItem : InspectorPropertyItem
{
	const float kExpandArrowRotateSpeed = 0.4f;
	static readonly Quaternion kExpandedRotation = Quaternion.AngleAxis(90f, Vector3.forward);
	static readonly Quaternion kNormalRotation = Quaternion.identity;

	[SerializeField]
	Button m_ExpandArrow;

	public override void UpdateSelf(float width, int depth)
	{
		base.UpdateSelf(width, depth);

		// Rotate arrow for expand state
		m_ExpandArrow.transform.localRotation = Quaternion.Lerp(m_ExpandArrow.transform.localRotation,
												data.expanded ? kExpandedRotation : kNormalRotation,
												kExpandArrowRotateSpeed);
	}

	public void ToggleExpanded()
	{
		data.expanded = !data.expanded;
	}

	protected override void OnDragStarted(BaseHandle baseHandle, HandleEventData eventData)
	{
		// Arrays cannot be dragged and dropped (yet)
	}

	protected override object GetDropObject(Transform fieldBlock)
	{
		// Arrays cannot be dragged and dropped (yet)
		return null;
	}

	public override bool TestDrop(GameObject target, object droppedObject)
	{
		return false;
	}

	public override bool ReceiveDrop(GameObject target, object droppedObject)
	{
		return false;
	}
}
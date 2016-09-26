using UnityEngine;
using SnappingModes = UnityEngine.VR.Utilities.U.Snapping.SnappingModes;

[ExecuteInEditMode]
public class SnappingToolUI : MonoBehaviour
{

	public SnappingTool snappingTool { set { m_SnappingTool = value; } }
	private SnappingTool m_SnappingTool;

	public void OnTogglePressed(int ToggleId)
	{
		if (m_SnappingTool)
		{
			SnappingModes mode = (SnappingModes)ToggleId;
			m_SnappingTool.snappingMode = mode;
		}
	}

}

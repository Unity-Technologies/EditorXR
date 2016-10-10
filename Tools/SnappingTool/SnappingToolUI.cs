using UnityEngine;
using SnappingModes = UnityEngine.VR.Utilities.U.Snapping.SnappingModes;
using Snapping = UnityEngine.VR.Utilities.U.Snapping;

[ExecuteInEditMode]
public class SnappingToolUI : MonoBehaviour
{

	public void OnTogglePressed(int ToggleId)
	{
		SnappingModes mode = (SnappingModes)ToggleId;
		if ((Snapping.currentSnappingMode & mode) != 0)
			Snapping.currentSnappingMode = Snapping.currentSnappingMode & ~mode;
		else
			Snapping.currentSnappingMode = Snapping.currentSnappingMode | mode;
	}

}

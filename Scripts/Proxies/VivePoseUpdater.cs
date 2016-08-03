using UnityEngine;
using System.Collections;
using Valve.VR;

public class VivePoseUpdater : MonoBehaviour {
	public ViveInputToEvents eventBridge;

	void OnPreCull()
	{
		if (eventBridge)
		{
			var compositor = OpenVR.Compositor;
			if (compositor != null)
			{
				var render = SteamVR_Render.instance;
				compositor.GetLastPoses(render.poses, render.gamePoses);
				eventBridge.OnNewPoses(render.poses);		  
			}
		}
	}
}

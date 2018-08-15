using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class TimeMachineBehaviour : PlayableBehaviour
{
	public TimeMachineAction action;
	public Condition condition;
	public string markerToJumpTo, markerLabel;
	public float timeToJumpTo;

	[HideInInspector]
	public bool clipExecuted = false; //the user shouldn't author this, the Mixer does

	public bool ConditionMet()
	{
		switch(condition)
		{
			case Condition.Always:
				return true;
				
			case Condition.PlatoonIsAlive:
				//The Timeline will jump to the label or time if a specific Platoon still has at least 1 unit alive
				if(false)//if(platoon != null)
				{
				    return true;
				    //return !platoon.CheckIfAllDead();
				}
				else
				{
					return false;
				}

			case Condition.Never:
			default:
				return false;
		}
	}

	public enum TimeMachineAction
	{
		Marker,
		JumpToTime,
		JumpToMarker,
		Pause,
	}

	public enum Condition
	{
		Always,
		Never,
		PlatoonIsAlive,
	}
}

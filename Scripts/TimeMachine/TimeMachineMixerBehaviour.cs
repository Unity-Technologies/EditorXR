using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class TimeMachineMixerBehaviour : PlayableBehaviour
{
	public Dictionary<string, double> markerClips;
	private PlayableDirector director;

	public override void OnPlayableCreate(Playable playable)
	{
		director = (playable.GetGraph().GetResolver() as PlayableDirector);
	}

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
		//ScriptPlayable<TimeMachineBehaviour> inputPlayable = (ScriptPlayable<TimeMachineBehaviour>)playable.GetInput(i);
		//Debug.Log(PlayableExtensions.GetTime<ScriptPlayable<TimeMachineBehaviour>>(inputPlayable));

		if(!Application.isPlaying)
		{
			return;
		}

        int inputCount = playable.GetInputCount();

        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            ScriptPlayable<TimeMachineBehaviour> inputPlayable = (ScriptPlayable<TimeMachineBehaviour>)playable.GetInput(i);
            TimeMachineBehaviour input = inputPlayable.GetBehaviour();
            
			if(inputWeight > 0f)
			{
				if(!input.clipExecuted)
				{
					switch(input.action)
					{
						case TimeMachineBehaviour.TimeMachineAction.Pause:
							if(input.ConditionMet())
							{
								//GameManager.Instance.PauseTimeline(director);
								input.clipExecuted = true; //this prevents the command to be executed every frame of this clip
							}
							break;
							
						case TimeMachineBehaviour.TimeMachineAction.JumpToTime:
						case TimeMachineBehaviour.TimeMachineAction.JumpToMarker:
							if(input.ConditionMet())
							{
								//Rewind
								if(input.action == TimeMachineBehaviour.TimeMachineAction.JumpToTime)
								{
									//Jump to time
									(playable.GetGraph().GetResolver() as PlayableDirector).time = (double)input.timeToJumpTo;
								}
								else
								{
									//Jump to marker
									double t = markerClips[input.markerToJumpTo];
									(playable.GetGraph().GetResolver() as PlayableDirector).time = t;
								}
								input.clipExecuted = false; //we want the jump to happen again!
							}
							break;
							
					}

				}
			}
        }
    }
}

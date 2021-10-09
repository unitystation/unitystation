using UnityEngine;
using System.Collections.Generic;
using Messages.Server;
using Mirror;

namespace Managers
{
	public class FPSMonitor : SingletonManager<FPSMonitor>
	{
		private List<float> avgSamples = new List<float>();

	    public float Current { get; private set; }
	    public float Average { get; private set; }
	    public float Min { get; private set; }
	    public float Max { get; private set; }

	    float timeInMax = 0f;
	    float timeInMin = 0f;

	    private float updateClientsTimer;

	    void Update()
	    {
	        timeInMax += Time.unscaledDeltaTime;
	        timeInMin += Time.unscaledDeltaTime;

	        Current = 1 / Time.unscaledDeltaTime;
	        Average = 0;

	        if (avgSamples.Count >= 200)
	        {
	            avgSamples.Add(Current);
	            avgSamples.RemoveAt(0);
	        }
	        else
	        {
	            avgSamples.Add(Current);
	        }

	        for (int i = 0; i < avgSamples.Count; i++)
	        {
	            Average += avgSamples[i];
	        }

	        Average /= 200;

	        if (timeInMin > 10f)
	        {
	            Min = -1;
	            timeInMin = 0f;
	        }

	        if (timeInMax > 10f)
	        {
	            Max = -1;
	            timeInMax = 0f;
	        }

	        if (Current < Min || Min < 0)
	        {
	            Min = Current;
	        }

	        if (Current > Max || Max < 0)
	        {
	            Max = Current;
	        }

	        updateClientsTimer += Time.unscaledDeltaTime;
	        if (updateClientsTimer > 0.5f)
	        {
		        if (NetworkServer.active)
		        {
			        UpdateServerFPS.Send(Current, Average);
		        }
		        updateClientsTimer = 0;
	        }
	    }
	}
}
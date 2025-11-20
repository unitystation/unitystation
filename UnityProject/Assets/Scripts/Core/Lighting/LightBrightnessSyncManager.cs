using System;
using System.Collections.Generic;
using Messages.Server;
using Objects.Lighting;
using Shared.Managers;
using UnityEngine;

public class LightBrightnessSyncManager : SingletonManager<LightBrightnessSyncManager>
{
	//TODO Re-log/fresh join synchronise At some point
	//synchronising the state for people who freshly Joined the round
	//Since it only sends a message every time it updates
	//should be fine since if it's fully powered the lights look exactly the same as they normally do
	//the other times its lower power the power is unstable so therefore you will get an update after a second or two and it will be fine

	//The only scenario where it becomes an issue is where a mapper has set a static Voltage where the lights are dim

	private static readonly Stack<PooledLights> _pool = new Stack<PooledLights>();

	public class PooledLights
	{
		public List<LightSource> LightSources = new List<LightSource>();


		public void Reset()
		{
			LightSources.Clear();
		}
	}


	public static PooledLights Get()
	{
		if (_pool.Count > 0)
			return _pool.Pop();

		return new PooledLights();
	}

	public static void Return(PooledLights pooled)
	{
		if (pooled == null)
			return;

		pooled.Reset();
		_pool.Push(pooled);
	}


	public static Dictionary<int, PooledLights> Updates = new Dictionary<int, PooledLights>();

	public static void EvaluateLightSource(LightSource LightSource, int Voltage)
	{
		PooledLights PooledLights = null;
		if (Updates.TryGetValue(Voltage, out PooledLights) == false)
		{
			PooledLights = Get();
			Updates[Voltage] = PooledLights;
		}

		PooledLights.LightSources.Add(LightSource);


	}

	public void UpdateMe()
	{
		if (Updates.Count > 0)
		{
			foreach (var VoltageLevel in Updates)
			{
				UpdateLightBrightness.Send(VoltageLevel.Key, VoltageLevel.Value.LightSources);
				LightBrightnessSyncManager.Return(VoltageLevel.Value);
			}
			Updates.Clear();
		}
	}

	public void OnEnable()
	{
		UpdateManager.Add(CallbackType.LATE_UPDATE, UpdateMe);
	}

	public void OnDisable()
	{
		UpdateManager.Remove(CallbackType.LATE_UPDATE, UpdateMe);
	}

}

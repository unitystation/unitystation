using System.Collections.Generic;
using UnityEngine;

public static class WaitFor
{
	public static readonly WaitForEndOfFrame EndOfFrame = new WaitForEndOfFrame();
	public static readonly WaitForFixedUpdate FixedUpdate = new WaitForFixedUpdate();

	private static Dictionary<float, WaitForSeconds> cachedWaitForSeconds = new Dictionary<float, WaitForSeconds>();
	private static Dictionary<float, WaitForSecondsRealtime> cachedWaitForSecondsRealtime = new Dictionary<float, WaitForSecondsRealtime>();

	static WaitFor()
	{
		EventManager.AddHandler(Event.RoundStarted, RoundStarted);
	}

	private static void RoundStarted()
	{
		Clear();
	}

	public static WaitForSeconds Minutes(float Minutes)
	{
		return Seconds(Minutes * 60);
	}

	public static WaitForSeconds Seconds(float seconds){
		if(!cachedWaitForSeconds.ContainsKey(seconds)){
			cachedWaitForSeconds[seconds] = new WaitForSeconds(seconds);
		}
		return cachedWaitForSeconds[seconds];
	}

	public static WaitForSecondsRealtime SecondsRealtime(float seconds){
		if(!cachedWaitForSecondsRealtime.ContainsKey(seconds)){
			cachedWaitForSecondsRealtime[seconds] = new WaitForSecondsRealtime(seconds);
		}
		return cachedWaitForSecondsRealtime[seconds];
	}

	public static void Clear(){
		cachedWaitForSeconds.Clear();
		cachedWaitForSecondsRealtime.Clear();
	}
}
using System;
using System.Collections;
using System.Collections.Generic;
using UI.Core.NetUI;
using UnityEngine;

public class NetCountdownTimer : MonoBehaviour
{

	public int RemainingSeconds = 0;

	public int StartingSeconds = 0;

	public bool Completed = true;

	public NetSlider Indicator;

	public void StartCountdown(int TimeSeconds)
	{
		Completed = false;
		RemainingSeconds = TimeSeconds;
		StartingSeconds = TimeSeconds;
		UpdateManager.Add(Refresh, 1);
	}


	private void Refresh()
	{
		RemainingSeconds -= 1;
		UpdateDisplay();



		if (RemainingSeconds > 0 == false)
		{
			Completed = true;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, Refresh);
		}
	}

	public void OnDestroy()
	{
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, Refresh);
	}

	public void UpdateDisplay()
	{
		var Value =  Mathf.Lerp(1, 0, (float) RemainingSeconds / (float) StartingSeconds);

		Indicator.SetValue(((int) (Value *100)).ToString());
	}
}
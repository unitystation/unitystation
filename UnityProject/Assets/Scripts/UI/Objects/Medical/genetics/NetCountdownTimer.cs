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
		StartCoroutine(Refresh());
	}


	private IEnumerator Refresh()
	{
		while (RemainingSeconds > 0)
		{
			UpdateDisplay();
			yield return WaitFor.Seconds(1f);
			RemainingSeconds -= 1;
		}

		Completed = true;

	}

	public void UpdateDisplay()
	{
		var Value =  Mathf.Lerp(1, 0, (float) RemainingSeconds / (float) StartingSeconds);

		Indicator.SetValue(((int) (Value *100)).ToString());
	}
}
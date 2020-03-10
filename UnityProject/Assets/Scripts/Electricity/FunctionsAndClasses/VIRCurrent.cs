using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Current 
{
	public double current = 0; 
}

public class WrapCurrent
{
	public Current Current;
	public double SendingCurrent;

	public void CombineCurrent(WrapCurrent addSendingCurrent ) {
		if (Current == addSendingCurrent.Current)
		{
			SendingCurrent = SendingCurrent + addSendingCurrent.SendingCurrent;
		}
		else {
			Logger.Log("HELp Trying to combine current with a different current ");
		}
	
	}

	public void SetUp(WrapCurrent _Current) {
		Current = _Current.Current;
		SendingCurrent = _Current.SendingCurrent;
	}

	public override string ToString()
	{
		return string.Format("(" + SendingCurrent + ")");
	} 
}


public class VIRCurrent
{
	public HashSet<WrapCurrent> CurrentSources = new HashSet<WrapCurrent>();

	public void addCurrent(WrapCurrent NewWrapCurrent) { 
		foreach (var wrapCurrent in CurrentSources)
		{
			CurrentSources.Add(NewWrapCurrent);
			return;
			if (wrapCurrent.Current == NewWrapCurrent.Current)
			{
				wrapCurrent.CombineCurrent(NewWrapCurrent);
				return;
			}
		}
		CurrentSources.Add(NewWrapCurrent);

	}

	public double Current() {		double Current = 0;
		foreach (var wrapCurrent in CurrentSources)
		{
			Current = Current + wrapCurrent.SendingCurrent;
		}
		return (Current);
	}

	public override string ToString()
	{
		return string.Format(Current().ToString() + "[" + string.Join(",", CurrentSources) + "]");
	}
}
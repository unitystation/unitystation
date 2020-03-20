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
	public double Strength;

	public void CombineCurrent(WrapCurrent addSendingCurrent ) {
		if (Current == addSendingCurrent.Current)
		{
			Strength = Strength + addSendingCurrent.Strength;
		}
		else {
			Logger.Log("HELp Trying to combine current with a different current ");
		}
	}

	public void Multiply(float Multiply) { 
		Strength = Strength*Multiply;
	}
	 
	public void SetUp(WrapCurrent _Current) {
		Current = _Current.Current;
		Strength = _Current.Strength;
	}
	public double GetCurrent()
	{
		return (Current.current * Strength);
	}
	public override string ToString()
	{
		return string.Format("(" + Strength + ")");
	} 
}


public class VIRCurrent
{
	public HashSet<WrapCurrent> CurrentSources = new HashSet<WrapCurrent>();

	public void addCurrent(WrapCurrent NewWrapCurrent) { 
		foreach (var wrapCurrent in CurrentSources)
		{
			if (wrapCurrent.Current == NewWrapCurrent.Current)
			{
				wrapCurrent.CombineCurrent(NewWrapCurrent);
				return;
			}
		}
		CurrentSources.Add(NewWrapCurrent);
	}

	public void addCurrent(VIRCurrent NewWrapCurrent)
	{		foreach (var inCurrent in NewWrapCurrent.CurrentSources)
		{
			foreach (var wrapCurrent in CurrentSources)
			{
				if (wrapCurrent.Current == inCurrent.Current)
				{
					wrapCurrent.CombineCurrent(inCurrent);
					return;
				}
			}
			CurrentSources.Add(inCurrent);
		}

	}

	public VIRCurrent SplitCurrent(float Multiplier)
	{
		var newVIRCurrent = new VIRCurrent(); //pool
		foreach (var CurrentS in CurrentSources) {
			var newWCurrent = new WrapCurrent();
			newWCurrent.SetUp(CurrentS);
			newVIRCurrent.addCurrent(newWCurrent);
		}

		foreach (var CurrentS in newVIRCurrent.CurrentSources)
		{
			CurrentS.Multiply(Multiplier);
		}
		return (newVIRCurrent);
	}

	public double Current() {		double Current = 0;
		foreach (var wrapCurrent in CurrentSources)
		{
			Current = Current + wrapCurrent.GetCurrent();
		}
		return (Current);
	}

	public override string ToString()
	{
		return string.Format(Current().ToString() + "[" + string.Join(",", CurrentSources) + "]");
	}
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public class Resistance
{ //a cheeky way to get pointers instead of copies without pinning anything 
	public float Ohms = 0;
	public bool ResistanceAvailable = true; // if false this resistance is not calculated

}

[System.Serializable]
public class ResistanceWrap
{
	public Resistance resistance;

	public float Strength = 1;

	public float Resistance()
	{
		return (resistance.Ohms * (1 / Strength));
	}

	public void SplitResistance(int Split)
	{
		Strength = Strength / Split;
	}

	public void SetUp(ResistanceWrap _ResistanceWrap) {
		resistance = _ResistanceWrap.resistance;
		Strength = _ResistanceWrap.Strength;
	}

	public void Multiply(float Multiplyer)
	{
		Strength = Strength *Multiplyer;
	}


	//public override string ToString()
	//{
	//	return string.Format("(" + "(" + resistance.GetHashCode() + ")" + resistance.Ohms  + "*" + (1 / Strength) + ")");
	//}

	//public override string ToString()
	//{
	//	return string.Format("(" + "(" + resistance.GetHashCode() + ")" + resistance.Ohms * (1 / Strength) + ")");
	//}

	public override string ToString()
	{
		return string.Format("("  + resistance.Ohms * (1 / Strength) + ")");
	}

}


[System.Serializable]
public class VIRResistances
{
	public HashSet<ResistanceWrap> ResistanceSources = new HashSet<ResistanceWrap>();

	public void AddResistance(ResistanceWrap resistance) {
		//return;
		foreach (var Resistancewrap in ResistanceSources) {
			//ResistanceSources.Add(resistance);
			//return;
			if (Resistancewrap.resistance == resistance.resistance) {
				
				Resistancewrap.Strength = Resistancewrap.Strength + resistance.Strength;
				return;
			}
		}
		ResistanceSources.Add(resistance);
	}


	public void AddResistance(VIRResistances resistance)
	{
		//return;
		foreach (var inResistancewrap in resistance.ResistanceSources)
		{
			foreach (var Resistancewrap in ResistanceSources)
			{
				//ResistanceSources.Add(resistance);
				//return;
				if (Resistancewrap.resistance == inResistancewrap.resistance)
				{

					Resistancewrap.Strength = Resistancewrap.Strength + inResistancewrap.Strength;
					return;
				}
			}
			ResistanceSources.Add(inResistancewrap);
		}
	}

	public float Resistance()
	{
		//Logger.Log("WorkOutResistance!");
		float ResistanceXAll = 0;
		foreach (ResistanceWrap Source in ResistanceSources)
		{
			//Logger.Log(Source.Value + "< Source.Value");//1 /
			ResistanceXAll += 1 / (Source.resistance.Ohms * (1/Source.Strength));
		}
		//Logger.Log((1 / ResistanceXAll)+ "< Return");
		return ((float)(1 / ResistanceXAll)); //1 / 
	}

	public VIRResistances Multiply(float Multiplier)
	{

		var newVIRResistances = new VIRResistances(); //pool
		foreach (var ResistanceS in ResistanceSources)
		{
			var newResistanceWrap = new ResistanceWrap();
			newResistanceWrap.SetUp(ResistanceS);
			newVIRResistances.AddResistance(newResistanceWrap);
		}

		foreach (var ResistanceS in newVIRResistances.ResistanceSources)
		{
			ResistanceS.Multiply(Multiplier);
		}
		return (newVIRResistances);
	}


	public override string ToString()
	{
		return string.Format(Resistance().ToString() + "[" + string.Join(",", ResistanceSources) + "]" );
	}

}
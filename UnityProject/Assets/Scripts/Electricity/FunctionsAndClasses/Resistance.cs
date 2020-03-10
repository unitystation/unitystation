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
				
			//	Resistancewrap.Strength = Resistancewrap.Strength + resistance.Strength;
				return;
			}
		}
		ResistanceSources.Add(resistance);
	}

	public void OverwriteResistance(ResistanceWrap resistance)
	{
		foreach (var Resistancewrap in ResistanceSources)
		{
			if (Resistancewrap.resistance == resistance.resistance)
			{
				Resistancewrap.Strength = resistance.Strength;
				return;
			}
		}
		ResistanceSources.Add(resistance);
	}

	public float Resistance()
	{
		//Logger.Log("WorkOutResistance!");
		decimal ResistanceXAll = 0;
		foreach (ResistanceWrap Source in ResistanceSources)
		{
			//Logger.Log(Source.Value + "< Source.Value");//1 /
			ResistanceXAll += 1 / ((decimal)Source.resistance.Ohms * (1/(decimal)Source.Strength));
		}
		//Logger.Log((1 / ResistanceXAll)+ "< Return");
		return ((float)(1 / ResistanceXAll)); //1 / 
	}
	//

	public float Resistance(Dictionary<Resistance, int> Weightdictionary)
	{
		//Logger.Log("WorkOutResistance!");
		decimal ResistanceXAll = 0;
		foreach (ResistanceWrap Source in ResistanceSources)
		{
			if (Weightdictionary.ContainsKey(Source.resistance))
			{ //TODO needs Ways for handling different amounts of strength which resistance
				ResistanceXAll += 1 / ((decimal)Source.resistance.Ohms * (1 / (decimal)Source.Strength) * Weightdictionary[Source.resistance]);
			}
			else { 
				ResistanceXAll += 1 / ((decimal)Source.resistance.Ohms * (1 / (decimal)Source.Strength));
			}
			//Logger.Log(Source.Value + "< Source.Value");//1 /

		}
		//Logger.Log((1 / ResistanceXAll)+ "< Return");
		return ((float)(1 / ResistanceXAll)); //1 / 
	}

	public HashSet<Resistance> ReturnBearResistances() {
		var reunt = new HashSet<Resistance>();
		foreach (var sour in ResistanceSources) {
			reunt.Add(sour.resistance);
		}
		return (reunt);
	}


	public override string ToString()
	{
		return string.Format(Resistance().ToString() + "[" + string.Join(",", ResistanceSources) + "]" );
	}

}
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

	public bool inPool;

	public float Resistance()
	{//(1 / )
		return (resistance.Ohms *  Strength);
	}

	//public void SplitResistance(int Split)
	//{
	//	Strength = Strength / Split;
	//}

	public void SetUp(ResistanceWrap _ResistanceWrap) {
		resistance = _ResistanceWrap.resistance;
		Strength = _ResistanceWrap.Strength;
	}

	public void Multiply(float Multiplyer)
	{
		Strength = Strength * Multiplyer;
	}


	public override string ToString()
	{//1/
		return string.Format("("  + resistance.Ohms  + "*" + ( Strength) ); //+ " ||  "+ "(" + resistance.GetHashCode() + ")" + "||)"
	}//

	//public override string ToString()
	//{
	//	return string.Format("(" + "(" + resistance.GetHashCode() + ")" + resistance.Ohms * (1 / Strength) + ")");
	//}

	//public override string ToString()
	//{
	//	return string.Format("("  + resistance.Ohms * (1 / Strength) + ")");
	//}

	public void Pool()
	{
		if (!inPool)
		{
			resistance = null;
			Strength = 1;
			ElectricalPool.PooledResistanceWraps.Add(this);
			inPool = true;
		}
	}

}


[System.Serializable]
public class VIRResistances
{
	public bool inPool;
	public List<ResistanceWrap> ResistanceSources = new List<ResistanceWrap>();

	public void AddResistance(ResistanceWrap resistance) {
		//return;
		/*foreach (var Resistancewrap in ResistanceSources) {
			//ResistanceSources.Add(resistance);
			//return;
			if (Resistancewrap.resistance == resistance.resistance) {

				Resistancewrap.Strength = resistance.Strength;
				return;
			}
		}*/
		ResistanceSources.Add(resistance);
	}

	public void AddResistance(VIRResistances resistance)
	{
		//return;
		foreach (var inResistancewrap in resistance.ResistanceSources)
		{
			bool pass = true;
			foreach (var Resistancewrap in ResistanceSources)
			{
				if (Resistancewrap.resistance == inResistancewrap.resistance)
				{
					pass = false;
					Resistancewrap.Strength = inResistancewrap.Strength;
					break;
				}
			}

			if (pass)
			{
				ResistanceSources.Add(inResistancewrap);
			}
		}
	}

	public float Resistance()
	{
		//Logger.Log("WorkOutResistance!");
		float ResistanceXAll = 0;
		foreach (var Source in ResistanceSources)
			//Logger.Log(Source.Value + "< Source.Value");//1 /
			ResistanceXAll += 1 / Source.Resistance();
		//Logger.Log((1 / ResistanceXAll)+ "< Return");
		return 1 / ResistanceXAll; //1 /
	}

	public VIRResistances Multiply(float Multiplier)
	{
		var newVIRResistances = ElectricalPool.GetVIRResistances();
		foreach (var ResistanceS in ResistanceSources)
		{
			var newResistanceWrap = ElectricalPool.GetResistanceWrap();
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
		return string.Format(Resistance().ToString()  + "[" + string.Join(",", ResistanceSources) + "]" );
	}

	public void Pool()
	{
		if (!inPool)
		{
			foreach (var ResistanceSource in ResistanceSources)
			{
				ResistanceSource.Pool();
			}
			ResistanceSources.Clear();
			ElectricalPool.PooledVIRResistances.Add(this);
			inPool = true;
		}
	}

}
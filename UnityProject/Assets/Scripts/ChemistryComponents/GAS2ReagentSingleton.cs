using System;
using System.Collections;
using System.Collections.Generic;
using Systems.Atmospherics;
using Chemistry;
using ScriptableObjects;
using ScriptableObjects.Atmospherics;
using UnityEngine;


[CreateAssetMenu(fileName = "GAS2ReagentSinWhogleton", menuName = "Singleton/GAS2ReagentSinWhogleton")]
public class GAS2ReagentSingleton : SingletonScriptableObject<GAS2ReagentSingleton>
{
	public Reagent Oxygen;
	public Reagent Nitrogen;
	public Reagent CarbonDioxide;

	public Reagent Plasma;
	public Reagent NitrousOxide;
	public Reagent Hydrogen;
	public Reagent WaterVapor;
	public Reagent BZ;
	public Reagent Miasma;
	public Reagent Nitryl;
	public Reagent Tritium;
	public Reagent HyperNoblium;
	public Reagent Stimulum;
	public Reagent Pluoxium;
	public Reagent Freon;
	
	private static Dictionary<Reagent, GasSO> ReagentToGas;
	private static Dictionary<GasSO, Reagent> GasToReagent;


	public Dictionary<Reagent, GasSO> DictionaryReagentToGas
	{
		get
		{
			CheckState();
			return ReagentToGas;
		}
	}

	public Dictionary<GasSO, Reagent> DictionaryGasToReagent
	{
		get
		{
			CheckState();
			return GasToReagent;
		}
	}

	private void CheckState()
	{
		if (ReagentToGas == null || GasToReagent == null)
		{
			ReagentToGas = new Dictionary<Reagent, GasSO>();
			GasToReagent = new Dictionary<GasSO, Reagent>();
			foreach (var _ReagentToGas in InitialReagentToGas)
			{
				ReagentToGas[IntToReagent(_ReagentToGas.Key)] = _ReagentToGas.Value;
				GasToReagent[_ReagentToGas.Value] = IntToReagent(_ReagentToGas.Key);
			}
		}
	}


	public Reagent GetGasToReagent(GasSO InGas)
	{
		CheckState();
		return GasToReagent[InGas];
	}

	public GasSO GetReagentToGas(Reagent InReagent)
	{
		CheckState();
		return ReagentToGas[InReagent];
	}

	private void OnEnable()
	{
		InitialReagentToGas = new Dictionary<int, GasSO>()
		{
			{1, Gas.Oxygen},
			{2, Gas.Nitrogen},
			{3, Gas.CarbonDioxide},
			{4, Gas.Plasma},
			{5, Gas.NitrousOxide},
			{6, Gas.Hydrogen},
			{7, Gas.WaterVapor},
			{8, Gas.BZ},
			{9, Gas.Miasma},
			{10, Gas.Nitryl},
			{11, Gas.Tritium},
			{12, Gas.HyperNoblium},
			{13, Gas.Stimulum},
			{14, Gas.Pluoxium},
			{15, Gas.Freon},
		};
	}

	private Dictionary<int, GasSO> InitialReagentToGas;

	private Reagent IntToReagent(int ToGET)
	{
		switch (ToGET)
		{
			case 1:
				return Oxygen;
			case 2:
				return Nitrogen;
			case 3:
				return CarbonDioxide;
			case 4:
				return Plasma;
			case 5:
				return NitrousOxide;
			case 6:
				return Hydrogen;
			case 7:
				return WaterVapor;
			case 8:
				return BZ;
			case 9:
				return Miasma;
			case 10:
				return Nitryl;
			case 11:
				return Tritium;
			case 12:
				return HyperNoblium;
			case 13:
				return Stimulum;
			case 14:
				return Pluoxium;
			case 15:
				return Freon;
		}
		return null;
	}


	private int ReagenToInt(Reagent ToGET)
	{
		if (Oxygen == ToGET) { return 1; }
		if (Nitrogen == ToGET) { return 2; }
		if (CarbonDioxide == ToGET) { return 3; }
		if (Plasma == ToGET) { return 4; }
		if (NitrousOxide == ToGET) { return 5; }
		if (Hydrogen == ToGET) { return 6; }
		if (WaterVapor == ToGET) { return 7; }
		if (BZ == ToGET) { return 8; }
		if (Miasma == ToGET) { return 9; }
		if (Nitryl == ToGET) { return 10; }
		if (Tritium == ToGET) { return 11; }
		if (HyperNoblium == ToGET) { return 12; }
		if (Stimulum == ToGET) { return 13; }
		if (Pluoxium == ToGET) { return 14; }
		if (Freon == ToGET) { return 15; }
		return -1;
	}
}
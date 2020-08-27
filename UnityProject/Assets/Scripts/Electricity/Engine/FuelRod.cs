using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FuelRod : ReactorChamberRod
{
	public float EditorPresentAtomsfuel;
	public float EditorPresentAtomsDecayProducts;
	public float EditorPresentAtomsXenon;
	public float EditorOutputtingEnergy;


	public decimal energyPerAtom = 53.333333333M;

	public decimal PresentAtoms = 100000000000000000;
	public decimal fuelNeutronGeneration = 2.5M;
	public decimal PresentAtomsfuel = 100000000000000000;
	public decimal PresentAtomsDecayProducts = 0;
	public decimal PresentAtomsXenon = 0;

	private const decimal DecayProductsHalfLife = 30;

	private const decimal
		DecayProductsOneSecondDecay = 0.9771599M; //  (decimal) Math.Pow(0.5D, (double) (1 / DecayProductsHalfLife)); /

	public const decimal DecayProductsNeutronGeneration = 0.45M;

	private const decimal XenonAbsorptionPower = 1000;
	private const decimal XenonGenerationProbability = 0.063M;
	private const decimal XenonHalfLife = 210;
	private const decimal XenonOneSecondDecay = 0.9967047m; //(decimal) Math.Pow(0.5D, (double) (1 / XenonHalfLife))

	public Tuple<decimal, decimal> ProcessRodHit(decimal AbsorbedNeutrons)
	{
		//Logger.Log(Time.time + "," + this.name + ", " + "PresentAtomsfuel , " + PresentAtomsfuel);
		//Logger.Log(Time.time + "," + this.name + ", " + "PresentAtomsDecayProducts , " + PresentAtomsDecayProducts);
		//Logger.Log(Time.time + "," + this.name + ", " + "PresentAtomsXenon , " + PresentAtomsXenon);
		//Logger.Log(Time.time + "," + this.name + ", " + "AbsorbedNeutrons , " + AbsorbedNeutrons);


		PresentAtomsXenon *= XenonOneSecondDecay;
		decimal DestroyedFuelAtoms = (PresentAtomsfuel / (PresentAtoms + (PresentAtomsXenon * XenonAbsorptionPower))) *
		                             AbsorbedNeutrons;
		//Logger.Log(Time.time + "," + this.name + ", " + "DestroyedFuelAtoms , " + DestroyedFuelAtoms);
		PresentAtomsXenon -= ((PresentAtomsXenon * XenonAbsorptionPower) / PresentAtoms) * AbsorbedNeutrons;
		if (PresentAtomsXenon < 0)
		{
			PresentAtomsXenon = 0;
		}

		PresentAtomsDecayProducts += DestroyedFuelAtoms;
		PresentAtomsfuel -= DestroyedFuelAtoms;

		PresentAtomsXenon += XenonGenerationProbability * PresentAtomsDecayProducts *
		                     (1 - DecayProductsOneSecondDecay);
		decimal CurrentAtomsDecayedProducts = (PresentAtomsDecayProducts * DecayProductsOneSecondDecay);

		decimal GeneratedNeutrons =
			((PresentAtomsDecayProducts - CurrentAtomsDecayedProducts) * DecayProductsNeutronGeneration) +
			(DestroyedFuelAtoms * fuelNeutronGeneration);

		PresentAtomsDecayProducts = CurrentAtomsDecayedProducts;
		//Logger.Log(Time.time + "," + this.name + ", " + "GeneratedNeutrons , " + GeneratedNeutrons);
		//Logger.Log(Time.time + "," + this.name + ", " + "Energy generated , " + (DestroyedFuelAtoms * energyPerAtom));
		SetEditerVariables(DestroyedFuelAtoms * energyPerAtom);
		return (new Tuple<decimal, decimal>((DestroyedFuelAtoms * energyPerAtom), GeneratedNeutrons));
	}

	public void SetEditerVariables(decimal OutputtingEnergy)
	{
		EditorPresentAtomsfuel =  (float)PresentAtomsfuel;
		EditorPresentAtomsDecayProducts =  (float) PresentAtomsDecayProducts ;
		EditorPresentAtomsXenon =  (float) PresentAtomsXenon;
		EditorOutputtingEnergy  =  (float) OutputtingEnergy;
	}

	public override RodType GetRodType()
	{
		return (RodType.Fuel);
	}
}
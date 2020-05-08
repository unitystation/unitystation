using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FuelRod : ReactorChamberRod
{
	public decimal energyPerAtom = 0.000000000032M ;

	private const decimal PresentAtoms = 100000000000000000;
	public decimal fuelNeutronGeneration = 1.65M;
	public decimal PresentAtomsfuel = 100000000000000000;
	public decimal PresentAtomsDecayProducts = 0;
	public decimal PresentAtomsXenon = 0;

	private const decimal DecayProductsHalfLife = 30;
	private const decimal DecayProductsOneSecondDecay = 0.02284M; // (1 - (decimal) Math.Pow(0.5D, (double) (1 / DecayProductsHalfLife)));
	public const  decimal DecayProductsNeutronGeneration = 0.45M;

	private const decimal XenonAbsorptionPower = 100000;
	private const decimal XenonGenerationProbability = 0.063M;
	private const decimal XenonHalfLife = 210;
	private const decimal XenonOneSecondDecay = 0.9967047m; //(decimal) Math.Pow(0.5D, (double) (1 / XenonHalfLife))

	public Tuple<decimal, decimal> ProcessRodHit(decimal AbsorbedNeutrons)
	{

		Logger.Log(Time.time + "," + this.name + ", " + "PresentAtomsfuel , " + PresentAtomsfuel);
		Logger.Log(Time.time + "," + this.name + ", " +  "PresentAtomsDecayProducts , " + PresentAtomsDecayProducts);
		Logger.Log(Time.time + "," + this.name + ", " + "PresentAtomsXenon , " + PresentAtomsXenon);
		Logger.Log(Time.time + "," + this.name + ", " + "ProcessRodHit , " + AbsorbedNeutrons);


		PresentAtomsXenon *= XenonOneSecondDecay;
		decimal DestroyedFuelAtoms = (PresentAtomsfuel/(PresentAtoms+(PresentAtomsXenon*XenonAbsorptionPower))) * AbsorbedNeutrons;
		Logger.Log(Time.time + "," + this.name + ", " + "DestroyedFuelAtoms , " + DestroyedFuelAtoms);
		PresentAtomsXenon -= ((PresentAtomsXenon * XenonAbsorptionPower) / PresentAtoms)* AbsorbedNeutrons;
		PresentAtomsDecayProducts += DestroyedFuelAtoms;

		PresentAtomsXenon += XenonGenerationProbability *PresentAtomsDecayProducts *
		                     DecayProductsOneSecondDecay;
		decimal CurrentAtomsDecayedProducts = (PresentAtomsDecayProducts* DecayProductsOneSecondDecay);

		decimal GeneratedNeutrons =
			((PresentAtomsDecayProducts - CurrentAtomsDecayedProducts) * DecayProductsNeutronGeneration) + (DestroyedFuelAtoms*fuelNeutronGeneration);

		PresentAtomsDecayProducts = CurrentAtomsDecayedProducts;
		Logger.Log(Time.time + "," + this.name + ", " + "GeneratedNeutrons , " + GeneratedNeutrons);
		Logger.Log(Time.time + "," + this.name + ", " + "Energy generated , " + (DestroyedFuelAtoms * energyPerAtom));
		return (new Tuple<decimal, decimal>((DestroyedFuelAtoms * energyPerAtom),GeneratedNeutrons));
	}
}

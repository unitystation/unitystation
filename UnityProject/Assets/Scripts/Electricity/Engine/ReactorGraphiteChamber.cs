using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;

public class ReactorGraphiteChamber : MonoBehaviour, IInteractable<HandApply>
{
	[SerializeField] private float tickRate = 1;
	private float tickCount;

	private ItemStorage RodStorage;

	private decimal NeutronLeakingChance = 0.0397M;

	public decimal EnergyReleased = 0; //Wattsec

	public float LikelihoodOfSpontaneousNeutron = 0.1f;

	public System.Random RNG = new System.Random();

	public decimal PresentNeutrons = 0;

	public ReactorChamberRod[] ReactorRods = new ReactorChamberRod[16];
	public List<FuelRod> ReactorFuelRods = new List<FuelRod>();

	public decimal ControlRodDepthPercentage = 1M;

	public decimal KFactor
	{
		get { return (CalculateKFactor()); }
	}

	public decimal CalculateKFactor()
	{
		decimal K = 0.85217022M * NeutronAbsorptionProbability();
		return (K);
	}

	public decimal NeutronAbsorptionProbability()
	{
		decimal AbsorptionPower = 0;
		decimal NumberOfRods = 0;
		foreach (var Rod in ReactorRods)
		{
			var controlRod = Rod as ControlRod;
			if (controlRod != null)
			{
				AbsorptionPower += controlRod.AbsorptionPower;
			}
			else
			{
				NumberOfRods++;
			}
		}

		return (NumberOfRods / 16 + (AbsorptionPower * ControlRodDepthPercentage));
		return (0.71M);
	}

	/*public decimal NeutronGenerationProbability()
	{
		decimal NumberOfRods = 0;
		foreach (var Rod in ReactorRods)
		{
			var fuelRod = Rod as FuelRod;
			if (fuelRod != null)
			{
				NumberOfRods++;
				fuelRod.energyPerAtom
			}
		}
		//Depends on the material Being input
		return (1.65M);
	}*/


	public decimal ExternalNeutronGeneration()
	{
		//Depends on local radiation
		return (0);
	}

	private void OnEnable()
	{
		RodStorage = this.GetComponent<ItemStorage>();
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	public void UpdateMe()
	{
		//Only update at set rate
		tickCount += Time.deltaTime;
		if (tickCount < tickRate)
		{
			return;
		}

		tickCount = 0;

		CycleUpdate();
	}

	public void CycleUpdate()
	{
		int SpontaneousNeutronProbability = RNG.Next(0, 10001);
		if ((decimal)LikelihoodOfSpontaneousNeutron > (SpontaneousNeutronProbability / 1000M))
		{
			PresentNeutrons += 1;
		}

		PresentNeutrons += ExternalNeutronGeneration();
		PresentNeutrons *= KFactor;
		Logger.Log(Time.time + ", CycleUpdatePresentNeutrons ," + PresentNeutrons);
		GenerateExternalRadiation();
		PowerOutput();
	}

	public void GenerateExternalRadiation()
	{
		//PresentNeutrons* NeutronLeakingChance;
	}

	public void PowerOutput()
	{
		EnergyReleased = ProcessRodsHits(PresentNeutrons);
		//200 MeV * (PresentNeutrons * NeutronAbsorptionProbability());
		//Temperature = Temperature + (EnergyReleased * Conversion Calculations With thermal mass)
	}

	public decimal ProcessRodsHits(decimal AbsorbedNeutrons)
	{
		decimal TotalEnergy = 0;
		decimal GeneratedNeutrons = 0;
		foreach (var Rod in ReactorFuelRods)
		{
			Tuple<decimal, decimal> Output = Rod.ProcessRodHit(AbsorbedNeutrons / ReactorFuelRods.Count);
			TotalEnergy += Output.Item1;
			GeneratedNeutrons += Output.Item2;
		}

		PresentNeutrons = GeneratedNeutrons;
		//Percentage into whatever purity
		//degrade Fuel rods
		//purity = 100
		//(EnergyPerAtom  * AbsorbedNeutrons * purity);
		//ProcessRodHit

		return (TotalEnergy);
	}
	//EnergyReleased = 200000000 eV * (PresentNeutrons*NeutronAbsorptionProbability())
	//0.000000000032 = Wsec
	//1 eV = 0.00000000000000000016 Wsec

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (interaction.UsedObject != null)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.ReactorRod))
			{
				var Rod = interaction.UsedObject.gameObject.GetComponent<ReactorChamberRod>();
				int pos = Array.IndexOf(ReactorRods, null);
				if (pos > -1)
				{
					ReactorRods[pos] = Rod;
					var EmptySlot = RodStorage.GetIndexedItemSlot(pos);
					Inventory.ServerTransfer(interaction.HandSlot, EmptySlot);
					var fuelRod = Rod as FuelRod;
					if (fuelRod != null)
					{
						ReactorFuelRods.Add(fuelRod);
					}
				}
			}
			else
			{
				//slam control rods in Depending on size
				ControlRodDepthPercentage = 1M;
			}
		}
		else
		{
			//pull out rod
			for (int i = 0; i < ReactorRods.Length; i++)
			{
				if (ReactorRods[i] != null)
				{
					var Rod = ReactorRods[i];
					var fuelRod = Rod as FuelRod;
					if (fuelRod != null)
					{
						ReactorFuelRods.Remove(fuelRod);
					}
					var EmptySlot = RodStorage.GetIndexedItemSlot(i);
					Inventory.ServerTransfer(EmptySlot , interaction.HandSlot);
				}
			}
		}
	}
}
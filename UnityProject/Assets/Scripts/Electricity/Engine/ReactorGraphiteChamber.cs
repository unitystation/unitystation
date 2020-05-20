using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry;
using Chemistry.Components;
using Light2D;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;
using Lighting;
using Radiation;

public class ReactorGraphiteChamber : MonoBehaviour, IInteractable<HandApply>
{
	public float EditorPresentNeutrons;
	public float EditorEnergyReleased;


	[SerializeField] private float tickRate = 1;
	private float tickCount;

	private ItemStorage RodStorage;

	private decimal NeutronLeakingChance = 0.0397M;

	public decimal EnergyReleased = 0; //Wattsec
	public ReactorBoiler reactorBoiler;

	public float LikelihoodOfSpontaneousNeutron = 0.1f;

	public System.Random RNG = new System.Random();

	public decimal PresentNeutrons = 0;

	public RadiationProducer radiationProducer;
	private RegisterObject registerObject;
	public ReagentContainer reagentContainer;
	public Chemistry.Reagent Water;

	private float WaterEnergyDensityPer1 = 6.5f;
	private float RodDensityPer1 = 7.5f;


	public ReactorChamberRod[] ReactorRods = new ReactorChamberRod[16];
	public List<FuelRod> ReactorFuelRods = new List<FuelRod>();

	public float ControlRodDepthPercentage = 1;
	private float EnergyToEvaporateWaterPer1 = 200;
	private float RodLockingTemperatureK = 448.15f;
	private float RodMeltingTemperatureK = 1273.15f;
	private float RodTemperatureK = 293.15f;
	private float BoilingPoint = 373.15f;

	public decimal MaximumOutputPressure = 5000M;
	public bool MeltedDown = false;

	public decimal NeutronSingularity = 7648830000000M;

	public decimal KFactor
	{
		get { return (CalculateKFactor()); }
	}

	public decimal CalculateKFactor()
	{
		decimal K = 0.85217022M * NeutronAbsorptionProbability();
		return (K);
	}

	public void SetControlRodDepth(float RequestedDepth)
	{
		if (RodTemperatureK < RodLockingTemperatureK)
		{
			ControlRodDepthPercentage = RequestedDepth;
		}
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

		if (MeltedDown == false)
		{
			return (NumberOfRods / ReactorRods.Length + (AbsorptionPower * (decimal) ControlRodDepthPercentage));
		}
		else
		{

			//Logger.Log("M > " + ((decimal)(100f / (100f + reagentContainer[Water])) * (NumberOfRods / ReactorRods.Length)));
			return ((decimal) (100f / (100f + reagentContainer[Water])) * (NumberOfRods / ReactorRods.Length));
		}

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


	private void OnEnable()
	{
		UpdateManager.Add(CycleUpdate, 1);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CycleUpdate);
	}

	private void Awake()
	{
		RodStorage = this.GetComponent<ItemStorage>();
		radiationProducer = this.GetComponent<RadiationProducer>();
		registerObject = this.GetComponent<RegisterObject>();
		reagentContainer = this.GetComponent<ReagentContainer>();
	}


	public void CycleUpdate()
	{
		if (RodTemperatureK > RodMeltingTemperatureK && !MeltedDown)
		{
			Logger.LogError("MeltedDown!!" , Category.Electrical);
			MeltedDown = true;
		}

		if (reactorBoiler == null) //Its blown up so not connected so vent to steam
		{
			Logger.LogError("steam!!" , Category.Electrical);
			if (reagentContainer[Water] > 0)
			{
				if (reagentContainer.Temperature > BoilingPoint)
				{
					var ExcessEnergy = (reagentContainer.Temperature - BoilingPoint);
					reagentContainer.Subtract(new ReagentMix(Water, (EnergyToEvaporateWaterPer1 * ExcessEnergy*(WaterEnergyDensityPer1*reagentContainer[Water]))));
				}
			}
		}


		int SpontaneousNeutronProbability = RNG.Next(0, 10001);
		if ((decimal) LikelihoodOfSpontaneousNeutron > (SpontaneousNeutronProbability / 1000M))
		{
			PresentNeutrons += 1;
		}

		PresentNeutrons += ExternalNeutronGeneration();
		GenerateExternalRadiation();
		PresentNeutrons *= KFactor;
		Logger.Log("NeutronSingularity " + (NeutronSingularity - PresentNeutrons));
		if (NeutronSingularity < PresentNeutrons)
		{
			Logger.LogError("DDFFR booommmm!!", Category.Electrical);
		}

		EditorPresentNeutrons = (float) PresentNeutrons;
		PowerOutput();


		//power Emergency shunts kicking on the transformer after 10 seconds to divert  power for 2 Minutes (Have to be manually reset after that)
		//add control system for power / some what

		//Otherwise water panic, as it turned into a Malton
		//If Fail explode
		//Otherwise you got a controlled mess you have to clean up
		//cleanup time, disassemble Throw radioactive fit into space
		//Build new chambers And hook up
		//when add cool

		//################################  Radiation
		// Stacks amount of radiation on player then slowly degrades it into Toxin
	}

	public void GenerateExternalRadiation()
	{
		if (PresentNeutrons > 0)
		{
			var LeakedNeutrons = PresentNeutrons * NeutronLeakingChance;
			LeakedNeutrons =
				(((LeakedNeutrons / (LeakedNeutrons + ((decimal) Math.Pow((double) LeakedNeutrons, (double) 0.82M)))) -
				  0.5M) * 2 * 36000);
			radiationProducer.UpdateValues((float) LeakedNeutrons);
		}
	}

	public decimal ExternalNeutronGeneration()
	{
		return ((decimal) registerObject.Matrix.GetMetaDataNode(registerObject.LocalPosition)
			.RadiationNode
			.CalculateRadiationLevel(radiationProducer.ObjectID));
	}

	public void PowerOutput()
	{
		EnergyReleased = ProcessRodsHits(PresentNeutrons);
		EditorEnergyReleased = (float) EnergyReleased;

		uint rods = 0;
		foreach (var rod in ReactorRods)
		{
			if (rod != null)
			{
				rods++;
			}
		}

		var ExtraEnergyGained   = (float) EnergyReleased / ((RodDensityPer1 * rods)+  (WaterEnergyDensityPer1 * reagentContainer[Water])) ; //when add cool

		RodTemperatureK = RodTemperatureK + ExtraEnergyGained;
		//Logger.Log("ExtraEnergyGained " +  (ExtraEnergyGained) );
		//Logger.Log("RodTemperatureK " +  (RodTemperatureK) );
		reagentContainer.Temperature = RodTemperatureK;
		decimal CurrentPressureInput = (decimal) ((reagentContainer.Temperature-293.15f) * reagentContainer[Water]);
		//Logger.Log("CurrentPressureInput " + CurrentPressureInput );
		if (CurrentPressureInput > MaximumOutputPressure)
		{
			Logger.LogError("BOOOMMMS MaximumOutputPressure", Category.Electrical);
			reactorBoiler = null;
		}
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
				if (MeltedDown == false)
				{
					ControlRodDepthPercentage = 1;
				}
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
					Inventory.ServerTransfer(EmptySlot, interaction.HandSlot);
				}
			}
		}
	}
}
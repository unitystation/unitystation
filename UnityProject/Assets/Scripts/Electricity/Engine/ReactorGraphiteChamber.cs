using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Chemistry;
using Chemistry.Components;
using Light2D;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;
using Lighting;
using Pipes;
using Radiation;
using UnityEngine.Serialization;

public class ReactorGraphiteChamber : MonoBehaviour, IInteractable<HandApply>, ISetMultitoolMaster, IServerDespawn, IServerSpawn
{
	public float EditorPresentNeutrons;
	public float EditorEnergyReleased;
	public GameObject UraniumOre;
	public GameObject MetalOre;
	public GameObject PlasSteel;

	private float tickCount;

	[SerializeField] private ItemStorage RodStorage;
	[SerializeField] private ItemStorage PipeStorage;

	private decimal NeutronLeakingChance = 0.0397M;

	public decimal EnergyReleased = 0; //Wattsec

	public ItemTrait PipeItemTrait = null;

	public float LikelihoodOfSpontaneousNeutron = 0.1f;

	public System.Random RNG = new System.Random();

	public decimal PresentNeutrons = 0;

	public RadiationProducer radiationProducer;
	private RegisterObject registerObject;
	public ReactorPipe ReactorPipe;

	private float WaterEnergyDensityPer1 = 10f;
	private float RodDensityPer1 = 7.5f;


	public ReactorChamberRod[] ReactorRods = new ReactorChamberRod[16];
	public List<FuelRod> ReactorFuelRods = new List<FuelRod>();
	public List<EngineStarter> ReactorEngineStarters = new List<EngineStarter>();


	public float ControlRodDepthPercentage = 1;

	private float EnergyToEvaporateWaterPer1 = 2000;

	//public float RodLockingTemperatureK = 700f;
	public float RodMeltingTemperatureK = 1100;
	private float BoilingPoint = 373.15f;

	public bool MeltedDown = false;
	public bool PoppedPipes = false;

	public decimal NeutronSingularity = 76488300000M;
	public decimal CurrentPressure = 0;

	public decimal MaxPressure = 120000;


	public decimal KFactor
	{
		get { return (CalculateKFactor()); }
	}

	public decimal CalculateKFactor()
	{
		decimal K = 0.85217022M * NonNeutronAbsorptionProbability();
		return (K);
	}

	public void SetControlRodDepth(float RequestedDepth)
	{
		ControlRodDepthPercentage = Mathf.Clamp(RequestedDepth, 0.1f, 1f);;
	}

	public float Temperature => GetTemperature();

	public float GetTemperature()
	{
		return (ReactorPipe.pipeData.mixAndVolume.Mix.Temperature);
	}

	public decimal NonNeutronAbsorptionProbability()
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
			return (NumberOfRods / (ReactorRods.Length + (AbsorptionPower * (decimal) ControlRodDepthPercentage)));
		}
		else
		{
			return ((decimal) (100f / (100f + ReactorPipe.pipeData.mixAndVolume.Mix.Total)) *
			        (NumberOfRods / ReactorRods.Length));
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
		if (CustomNetworkManager.Instance._isServer == false ) return;

		UpdateManager.Add(CycleUpdate, 1);
	}

	private void OnDisable()
	{
		if (CustomNetworkManager.Instance._isServer == false ) return;

		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CycleUpdate);
	}

	private void Awake()
	{
		radiationProducer = this.GetComponent<RadiationProducer>();
		registerObject = this.GetComponent<RegisterObject>();
		ReactorPipe = this.GetComponent<ReactorPipe>();
	}


	public void CycleUpdate()
	{
		if (GetTemperature() > RodMeltingTemperatureK && !MeltedDown)
		{
			MeltedDown = true;
		}


		if (PoppedPipes) //Its blown up so not connected so vent to steam
		{
			if (ReactorPipe.pipeData.mixAndVolume.Mix.Total > 0 &&
			    ReactorPipe.pipeData.mixAndVolume.Mix.Temperature > BoilingPoint)
			{
				var ExcessEnergy = (ReactorPipe.pipeData.mixAndVolume.Mix.Temperature - BoilingPoint);
				ReactorPipe.pipeData.mixAndVolume.Mix.TransferTo(new ReagentMix(), (EnergyToEvaporateWaterPer1
				                                                                    * ExcessEnergy *
				                                                                    (WaterEnergyDensityPer1 *
				                                                                     ReactorPipe.pipeData
					                                                                     .mixAndVolume.Mix.Total)));
			}
		}


		int SpontaneousNeutronProbability = RNG.Next(0, 10001);
		if ((decimal) LikelihoodOfSpontaneousNeutron > (SpontaneousNeutronProbability / 1000M))
		{
			PresentNeutrons += 1;
		}

		foreach (var Starter in ReactorEngineStarters)
		{
			PresentNeutrons += (decimal) Starter.NeutronGenerationPerSecond;
		}

		PresentNeutrons += ExternalNeutronGeneration();
		GenerateExternalRadiation();
		PresentNeutrons *= KFactor;
		//Logger.Log("NeutronSingularity " + (NeutronSingularity - PresentNeutrons));
		if (NeutronSingularity < PresentNeutrons)
		{
			//Logger.LogError("DDFFR booommmm!!", Category.Electrical);
			Explosions.Explosion.StartExplosion(registerObject.LocalPosition, 120000, registerObject.Matrix);
			PresentNeutrons = 0;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CycleUpdate);
			OnDespawnServer(null);
			Despawn.ServerSingle(this.gameObject);
		}

		EditorPresentNeutrons = (float) PresentNeutrons;
		PowerOutput();

		//Sprites
		//Reduce  sound of geiger counter
		//Coloring numbers in UI with red - bad, green - good.
		//2) Tooltips when hovering on buttons/slider, like foma did with action buttons.
		//1) Damage for RWalls and players from explosions
		//2)Nerf easy sabotage for Reactor or people will be blowing it too fast
		//Synchronise radiation
	}

	public void GenerateExternalRadiation()
	{
		if (PresentNeutrons > 0)
		{
			var LeakedNeutrons = PresentNeutrons * NeutronLeakingChance;
			LeakedNeutrons =
				(((LeakedNeutrons / (LeakedNeutrons + ((decimal) Math.Pow((double) LeakedNeutrons, (double) 0.82M)))) -
				  0.5M) * 2 * 36000);
			radiationProducer.Setlevel((float) LeakedNeutrons);
		}
	}


	public float RadiationAboveCore()
	{
		return (registerObject.Matrix.GetMetaDataNode(registerObject.LocalPosition)
			.RadiationNode
			.CalculateRadiationLevel());
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

		var ExtraEnergyGained = (float) EnergyReleased /
		                        ((RodDensityPer1 * rods) +
		                         (WaterEnergyDensityPer1 *
		                          ReactorPipe.pipeData.mixAndVolume.Mix.Total)); //when add cool

		if (ReactorPipe.pipeData.mixAndVolume.Mix.WholeHeatCapacity == 0)
		{
			ReactorPipe.pipeData.mixAndVolume.Mix.Temperature += ExtraEnergyGained / 90000;
		}
		else
		{
			ReactorPipe.pipeData.mixAndVolume.Mix.InternalEnergy =
				ReactorPipe.pipeData.mixAndVolume.Mix.InternalEnergy + ExtraEnergyGained;
		}



		CurrentPressure = (decimal) ((ReactorPipe.pipeData.mixAndVolume.Mix.Temperature - 293.15f) *
		                             ReactorPipe.pipeData.mixAndVolume.Mix.Total);
		if (CurrentPressure > MaxPressure)
		{
			PoppedPipes = true;
			var EmptySlot = PipeStorage.GetIndexedItemSlot(0);
			Inventory.ServerDrop(EmptySlot);
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


	public bool TryInsertRod(HandApply interaction)
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

				var engineStarter = Rod as EngineStarter;
				if (engineStarter != null)
				{
					ReactorEngineStarters.Add(engineStarter);
				}
			}

			return true;
		}

		return false;
	}

	public bool TryInsertPipe(HandApply interaction)
	{
		if (Validations.HasItemTrait(interaction.UsedObject, PipeItemTrait))
		{
			var EmptySlot = PipeStorage.GetIndexedItemSlot(0);
			if (EmptySlot.Item == null)
			{
				Inventory.ServerTransfer(interaction.HandSlot, EmptySlot);
				PoppedPipes = false;
			}

			return true;
		}

		return false;
	}

	public bool TryDeconstructCore(HandApply interaction)
	{
		if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Welder) &&
		    MeltedDown == false)
		{
			if (ReactorRods.All(x => x == null))
			{
				ToolUtils.ServerUseToolWithActionMessages(interaction, 10,
					"You start to deconstruct the empty core...",
					$"{interaction.Performer.ExpensiveName()} starts to deconstruct the empty core...",
					"You deconstruct the empty core.",
					$"{interaction.Performer.ExpensiveName()} deconstruct the empty core.",
					() => { Despawn.ServerSingle(gameObject); });
			}
			else
			{
				Chat.AddExamineMsgFromServer(interaction.Performer,
					"The inserted rods make it impossible to deconstruct");
			}

			return true;
		}

		return false;
	}


	public bool TryAxeCore(HandApply interaction)
	{
		if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Pickaxe) &&
		    MeltedDown == true)
		{
			ToolUtils.ServerUseToolWithActionMessages(interaction, 10,
				"You start to hack away at the molten core...",
				$"{interaction.Performer.ExpensiveName()} starts to hack away at the molten core...",
				"You break the molten core to pieces.",
				$"{interaction.Performer.ExpensiveName()} breaks the molten core to pieces.",
				() => { Despawn.ServerSingle(gameObject); });
			return true;
		}

		return false;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (interaction.UsedObject != null)
		{
			if (TryInsertRod(interaction)) return;
			if (TryInsertPipe(interaction)) return;
			if (TryDeconstructCore(interaction)) return;
			if (TryAxeCore(interaction)) return;

			//slam control rods in
			if (MeltedDown == false)
			{
				ControlRodDepthPercentage = 1;
			}
		}
		else
		{
			//pull out rod

			for (int i = ReactorRods.Length; i-- > 0;)
			{
				if (ReactorRods[i] != null)
				{
					var Rod = ReactorRods[i];
					var fuelRod = Rod as FuelRod;
					if (fuelRod != null)
					{
						ReactorFuelRods.Remove(fuelRod);
					}

					var engineStarter = Rod as EngineStarter;
					if (engineStarter != null)
					{
						ReactorEngineStarters.Remove(engineStarter);
					}

					ReactorRods[i] = null;
					var EmptySlot = RodStorage.GetIndexedItemSlot(i);
					Inventory.ServerTransfer(EmptySlot, interaction.HandSlot);
					return;
				}
			}
		}
	}

	/// <summary>
	/// is the function to denote that it will be pooled or destroyed immediately after this function is finished, Used for cleaning up anything that needs to be cleaned up before this happens
	/// </summary>
	public void OnDespawnServer(DespawnInfo info)
	{
		if (MeltedDown)
		{
			foreach (var Rod in ReactorRods)
			{
				if (Rod != null)
				{
					switch (Rod.GetRodType())
					{
						case RodType.Fuel:
							Spawn.ServerPrefab(UraniumOre, registerObject.WorldPositionServer);
							break;
						case RodType.Control:
							Spawn.ServerPrefab(MetalOre, registerObject.WorldPositionServer);
							break;
					}
				}
			}
		}
		else
		{
			foreach (var Rod in RodStorage.GetItemSlots())
			{
				Inventory.ServerDespawn(Rod);
			}

			Spawn.ServerPrefab(CommonPrefabs.Instance.Plasteel, registerObject.WorldPositionServer, count: 40);
		}


		MeltedDown = false;
		PoppedPipes = false;
		PresentNeutrons = 0;
		Array.Clear(ReactorRods, 0, ReactorRods.Length);
		ReactorFuelRods.Clear();
		ReactorEngineStarters.Clear();
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CycleUpdate);
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		UpdateManager.Add(CycleUpdate, 1);
	}


	//######################################## Multitool interaction ##################################
	private MultitoolConnectionType conType = MultitoolConnectionType.ReactorChamber;
	public MultitoolConnectionType ConType => conType;
	private bool multiMaster = false;
	public bool MultiMaster => multiMaster;

	public void AddSlave(object SlaveObjectThis)
	{
	}
}
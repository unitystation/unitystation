using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Systems.Atmospherics;
using Systems.ElectricalArcs;
using Systems.Explosions;
using Systems.Radiation;
using AddressableReferences;
using AdminTools;
using Core.Lighting;
using HealthV2;
using Light2D;
using Mirror;
using ScriptableObjects.Atmospherics;
using Systems.Score;
using UnityEngine;
using Weapons.Projectiles;
using Weapons.Projectiles.Behaviours;
using Random = UnityEngine.Random;
using Communications;
using Objects.Machines.ServerMachines.Communications;
using ScriptableObjects.Communications;
using Systems.Communications;
using InGameEvents;
using Items;


namespace Objects.Engineering
{
	/// <summary>
	/// Supermatter script, controls supermatter effects
	/// Script originated from Tg DM code, which has been modified for UnityStation
	/// </summary>
	public class SuperMatter : SignalEmitter, IOnHitDetect, IExaminable, IBumpableObject, ICheckedInteractable<HandApply>, IChatInfluencer
	{
		#region lightSpriteDefines

		[SerializeField]
		private LightSprite lightSprite;

		[SerializeField]
		private LightPulser lightPulser;

		#endregion

		#region OverlaySprite

		[SerializeField]
		private SpriteHandler mainSprite = null;

		[SerializeField]
		private SpriteHandler overlaySpriteHandler = null;

		#endregion

		#region HeatPenaltyDefines

		// Higher == Bigger heat and waste penalty from having the crystal surrounded by this gas. Negative numbers reduce penalty.
		private Dictionary<GasSO, float> heatPenaltyDefines;

		#endregion

		#region TransmitDefines

		//All of these get divided by 10-bzcomp * 5 before having 1 added and being multiplied with power to determine rads
		//Keep the negative values here above -10 and we won't get negative rads
		//Higher == Bigger bonus to power generation.
		private Dictionary<GasSO, float> transmitDefines;

		#endregion

		#region HeatResistanceDefines

		//Higher == Gas makes the crystal more resistant against heat damage.
		private Dictionary<GasSO, float> heatResistanceDefines;

		#endregion

		#region WarningDefines

		//If integrity percent remaining is less than these values, the monitor sets off the relevant alarm.
		private const int SupermatterDelamPercent =  5;
		private const int SupermatterEmergencyPercent = 25;
		private const int SupermatterDangerPercent = 50;
		private const int SupermatterWarningPercent = 100;

		private string safeAlertText = "Crystalline hyperstructure returning to safe operating parameters.\n";
		private const int WarningPoint = 950;
		private string warningAlertText = "Danger! Crystal hyperstructure integrity faltering!\n";
		private const int DamagePenaltyPoint = 450;
		private const int EmergencyPoint = 300;
		private string emergencyAlertText = "CRYSTAL DELAMINATION IMMINENT.\n";
		private const int ExplosionPoint = 50;

		#endregion

		#region AnomalyDefines

		[SerializeField]
		private GameObject singularity = null;

		[SerializeField] private int scoreForReleasingSingularity = -500;
		private const string LOOSE_SCORE = "gooseisloose";

		[SerializeField]
		private GameObject energyBall = null;

		[SerializeField]
		private GameObject gravitationalAnomaly = null;

		[SerializeField]
		private GameObject fluxAnomaly = null;

		[SerializeField]
		private GameObject pyroAnomaly = null;

		#endregion

		#region PowerDefines

		private const float PowerlossInhibitionGasThreshold = 0.20f;         //Higher == Higher percentage of inhibitor gas needed before the charge inertia chain reaction effect starts.
		private const int PowerlossInhibitioMoleThreshold = 20;        //Higher == More moles of the gas are needed before the charge inertia chain reaction effect starts.        //Scales powerloss inhibition down until this amount of moles is reached
		private const int PowerlossInhibitionMoleBoostThreshold = 500;  //bonus powerloss inhibition boost if this amount of moles is reached

		private const int MolePenaltyThreshold = 1800;           //Above this value we can get lord singulo and independent mol damage, below it we can heal damage
		private const int MoleHeatPenalty = 350;                 //Heat damage scales around this. Too hot setups with this amount of moles do regular damage, anything above and below is scaled
																//Along with damage_penalty_point, makes flux anomalies.
		private const int PowerPenaltyThreshold = 5000;          //The cutoff on power properly doing damage, pulling shit around, and delamming into a tesla. Low chance of pyro anomalies, +2 bolts of electricity
		private const int SeverePowerPenaltyThreshold = 7000;   //+1 bolt of electricity, allows for gravitational anomalies, and higher chances of pyro anomalies
		private const int CriticalPowerPenaltyThreshold = 9000; //+1 bolt of electricity.
		private const int HeatPenaltyThreshold = 40;             //Higher == Crystal safe operational temperature is higher.
		private const float DamageHardcap = 0.004f;

		[SerializeField]
		private const float DamageIncreaseMultiplier = 0.25f;

		private const int ThermalReleaseModifier = 5;         //Higher == less heat released during reaction, not to be confused with the above values
		private const int PlasmaReleaseModifier = 750;        //Higher == less plasma released by reaction
		private const int OxygenReleaseModifier = 325;        //Higher == less oxygen released at high temperature/power

		private const float ReactionPowerModifier = 0.55f;       //Higher == more overall power

		private const int MatterPowerConversion = 10;         //Crystal converts 1/this value of stored matter into energy.

		#endregion

		#region OtherDefines

		private const int CriticalTemperature = 10000;

		[SerializeField]
		private float detonationRads = 2000;

		[SerializeField]
		private GameObject emitterBulletPrefab = null;

		[SerializeField]
		private GameObject superMatterShard = null;

		[SerializeField]
		private ItemTrait superMatterScalpel = null;

		[SerializeField]
		private ItemTrait superMatterTongs = null;

		[SerializeField]
		private ItemTrait superMatterSliver = null;

		[SerializeField]
		private GameObject nuclearParticlePrefab = null;

		private string emitterBulletName;

		#endregion

		#region CompositionDefines

		// raw composition of each gas in the chamber, ranges from 0 to 1
		private float n2Compositon = 0;
		private float plasmaCompositon = 0;
		private float o2Compositon = 0;
		private float co2Compositon = 0;
		private float n2oCompositon = 0;
		private float tritiumCompositon = 0;
		private float bzCompositon = 0;
		private float pluoxiumCompositon = 0;
		private float h2oCompositon = 0;
		private float freonCompositon = 0;

		///Determines the rate of positive change in gas comp values
		private float gasChangeRate = 0.05f;

		///The last air sample's total molar count, will always be above or equal to 0
		private float combinedGas = 0;

		private float dynamicHeatResistance = 1;
		private float powerlossInhibitor = 1;
		private float powerlossDynamicScaling = 0;
		private float moleHeatPenalty = 0;
		private float matterPower = 0;

		#endregion

		#region LightningDefines

		[SerializeField]
		[Tooltip("arc effect")]
		private GameObject arcEffect = null;

		[Tooltip("How many secondary targets there are.")]
		[SerializeField, Range(1, 5)]
		private int secondaryArcCount = 3;
		[SerializeField, Range(0.5f, 10)]
		private float duration = 2;
		private int primaryRange;
		[SerializeField, Range(3, 12)]
		private int secondaryRange = 4;

		[SerializeField]
		private AddressableAudioSource lightningSound = null;

		private int mask;

		#endregion

		#region SoundDefines

		private string loopingSoundGuid = "";

		[SerializeField]
		private AddressableAudioSource normalLoopSound = null;
		[SerializeField]
		private AddressableAudioSource delamLoopSound = null;

		[SerializeField]
		private AddressableAudioSource blobAlarm = null;

		[SerializeField]
		private AddressableAudioSource engineAlert1 = null;

		[SerializeField]
		private AddressableAudioSource engineAlert2 = null;

		[SerializeField]
		private AddressableAudioSource terminalAlert = null;

		#endregion

		[SerializeField]
		private bool canTakeIntegrityDamage = true;

		[SerializeField]
		private bool isHugBox;

		private float superMatterMaxIntegrity = 1000f;
		private float superMatterIntegrity = 1000f;
		private float previousIntegrity;

		private float power;
		// = 29 so when it first reaches the warning level there will be an alert
		private float warningTimer = 29;

		private RegisterTile registerTile;

		private GasMix removeMix = new GasMix();

		private bool finalCountdown; //uh oh
		private int finalCountdownTime = 30; //30 seconds

		private float updateTime = 1f; //Every second do check

		[SyncVar(hook = nameof(SyncIsDelam))]
		private bool isDelam;

		[SerializeField] private int explosionStrength = 55000;
		[SerializeField] private SignalDataSO radioSO;

		#region LifeCycle

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			emitterBulletName = emitterBulletPrefab.GetComponent<Bullet>().visibleName;
			mask = LayerMask.GetMask("Machines", "WallMounts", "Objects", "Players", "NPC");

			heatResistanceDefines = new Dictionary<GasSO, float>
			{
				{Gas.NitrousOxide, 6},
				{Gas.Pluoxium, 3},
				{Gas.WaterVapor, 10}
			};

			transmitDefines = new Dictionary<GasSO, float>
			{
				{Gas.Oxygen, 1.5f},
				{Gas.Plasma, 4},
				{Gas.BZ, -2},
				{Gas.Tritium, 30},
				{Gas.Pluoxium, -5},
				{Gas.WaterVapor, -9}
			};

			heatPenaltyDefines = new Dictionary<GasSO, float>
			{
				{Gas.Plasma, 15},
				{Gas.Oxygen, 1},
				{Gas.Pluoxium, -1},
				{Gas.Tritium, 10},
				{Gas.CarbonDioxide, 0.1f},
				{Gas.Nitrogen, -1.5f},
				{Gas.BZ, 5},
				{Gas.WaterVapor, 8},
				{Gas.Freon, -10}
			};
		}

		public override void OnStartClient()
		{
			SyncIsDelam(isDelam, isDelam);
		}

		private void Start()
		{
			if(CustomNetworkManager.IsServer == false) return;

			overlaySpriteHandler.PushClear();
			isDelam = false;
		}

		private void OnEnable()
		{
			UpdateManager.Add(SuperMatterUpdate, updateTime);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, SuperMatterUpdate);
			SoundManager.Stop(loopingSoundGuid);
		}

		[Client]
		private void SyncIsDelam(bool oldVar, bool newVar)
		{
			if (newVar)
			{
				//Delam state
				SoundManager.Stop(loopingSoundGuid);
				loopingSoundGuid = Guid.NewGuid().ToString();
				_ = SoundManager.PlayAtPosition(delamLoopSound, registerTile.WorldPositionServer, gameObject, loopingSoundGuid);

				lightSprite.transform.localScale = new Vector3(9, 9, 9);
				lightPulser.SetPulseSpeed(1);
			}
			else
			{
				//Normal state
				SoundManager.Stop(loopingSoundGuid);
				loopingSoundGuid = Guid.NewGuid().ToString();
				_ = SoundManager.PlayAtPosition(normalLoopSound, registerTile.WorldPositionServer, gameObject, loopingSoundGuid);

				lightSprite.transform.localScale = new Vector3(3, 3, 3);
				lightPulser.SetPulseSpeed(0.5f);
			}
		}

		#endregion

		#region UpdateLoop

		private void SuperMatterUpdate()
		{
			if(CustomNetworkManager.IsServer == false) return;

			if(isHugBox) return;

			CheckPower();

			CheckEffects();

			CheckWarnings();
		}

		#endregion

		#region CheckPower

		private void CheckPower()
		{
			var gasNode = registerTile.Matrix.GetMetaDataNode(registerTile.LocalPositionServer, false);
			if(gasNode == null) return;

			var gasMix = gasNode.GasMix;

			GasMix.TransferGas(removeMix, gasMix, 0.15f * gasMix.Moles);

			previousIntegrity = superMatterIntegrity;

			if (removeMix.Moles == 0 || registerTile.Matrix.IsSpaceAt(registerTile.WorldPositionServer, true))
			{
				//Always does at least some integrity damage if allowed
				if (canTakeIntegrityDamage)
				{
					superMatterIntegrity -= Mathf.Max((power / 1000) * DamageIncreaseMultiplier, 0.1f);
				}
			}
			else
			{
				//If allowed to take integrity damage, calculate it
				if (canTakeIntegrityDamage)
				{
					CalculateDamage();
				}

				combinedGas = Mathf.Max(removeMix.Moles, 0);

				//Lets get the proportions of the gasses in the mix and then slowly move our comp to that value
				//Can cause an overestimation of mol count, should stabilize things though.
				//Prevents huge bursts of gas/heat when a large amount of something is introduced
				//They range between 0 and 1
				plasmaCompositon += Mathf.Clamp(Mathf.Max(removeMix.GetMoles(Gas.Plasma) / combinedGas, 0) - plasmaCompositon, -1,gasChangeRate);
				o2Compositon += Mathf.Clamp(Mathf.Max(removeMix.GetMoles(Gas.Oxygen) / combinedGas, 0) - o2Compositon, -1, gasChangeRate);
				co2Compositon += Mathf.Clamp(Mathf.Max(removeMix.GetMoles(Gas.CarbonDioxide) / combinedGas, 0) - co2Compositon, -1, gasChangeRate);
				pluoxiumCompositon += Mathf.Clamp(Mathf.Max(removeMix.GetMoles(Gas.Pluoxium) / combinedGas, 0) - pluoxiumCompositon, -1, gasChangeRate);
				tritiumCompositon += Mathf.Clamp(Mathf.Max(removeMix.GetMoles(Gas.Tritium) / combinedGas, 0) - tritiumCompositon, -1, gasChangeRate);
				bzCompositon += Mathf.Clamp(Mathf.Max(removeMix.GetMoles(Gas.BZ) / combinedGas, 0) - bzCompositon, -1, gasChangeRate);
				n2oCompositon += Mathf.Clamp(Mathf.Max(removeMix.GetMoles(Gas.NitrousOxide) / combinedGas, 0) - n2oCompositon, -1, gasChangeRate);
				n2Compositon += Mathf.Clamp(Mathf.Max(removeMix.GetMoles(Gas.Nitrogen) / combinedGas, 0) - n2Compositon, -1, gasChangeRate);
				h2oCompositon += Mathf.Clamp(Mathf.Max(removeMix.GetMoles(Gas.WaterVapor) / combinedGas, 0) - h2oCompositon, -1, gasChangeRate);
				freonCompositon += Mathf.Clamp(Mathf.Max(removeMix.GetMoles(Gas.Freon) / combinedGas, 0) - freonCompositon, -1, gasChangeRate);

				var pluoxiumBonus = 0f;
				var waterBonus = 0f;
				var freonBonus = 0f;

				var waterMalus = 1f;
				var waterFixed = 1f;

				//We're concerned about pluoxium being too easy to abuse at low percents, so we make sure there's a substantial amount.
				if (pluoxiumCompositon >= 0.15)
				{
					//makes pluoxium only work at 15%+
					pluoxiumBonus = 1;
				}

				//heat protection from h2o only works when it's between 35 and 45%
				if (h2oCompositon >= 0.35 && h2oCompositon <= 0.45)
				{
					waterBonus = 1;
				}

				//Too much freon will stop power generation, only below 0.03 is allowed
				if (freonCompositon <= 0.03)
				{
					freonBonus = 1;
				}

				if (h2oCompositon > 0.45)
				{
					//Too much water will cause very bad problems
					waterMalus = 2;
				}
				else if (h2oCompositon >= 0.05)
				{
					//Engine will produce less power between 0.05 and 0.45 moles of water
					waterMalus = 0.05f;
				}

				//variability in the h2o penalty calculation depending on the %
				if (h2oCompositon > 0.5)
				{
					waterFixed = 2;
				}
				else if (h2oCompositon >= 0.3)
				{
					waterFixed = 0.5f;
				}

				//No less then zero, and no greater then one, we use this to do explosions and heat to power transfer
				var gasmixPowerRatio = Mathf.Clamp(
					(plasmaCompositon + o2Compositon + co2Compositon + h2oCompositon + tritiumCompositon + bzCompositon
					 - pluoxiumCompositon - n2Compositon - freonCompositon) * waterMalus, 0, 1);

				//Minimum value of 1.5, maximum value of 23
				var dynamicHeatModifier = plasmaCompositon * heatPenaltyDefines[Gas.Plasma];
				dynamicHeatModifier += o2Compositon * heatPenaltyDefines[Gas.Oxygen];
				dynamicHeatModifier += co2Compositon * heatPenaltyDefines[Gas.CarbonDioxide];
				dynamicHeatModifier += tritiumCompositon * heatPenaltyDefines[Gas.Tritium];
				dynamicHeatModifier += pluoxiumCompositon * heatPenaltyDefines[Gas.Pluoxium] * pluoxiumBonus;
				dynamicHeatModifier += n2Compositon * heatPenaltyDefines[Gas.Nitrogen];
				dynamicHeatModifier += bzCompositon * heatPenaltyDefines[Gas.BZ];
				dynamicHeatModifier += freonCompositon * heatPenaltyDefines[Gas.Freon];
				dynamicHeatModifier *= waterMalus;
				dynamicHeatModifier += h2oCompositon * heatPenaltyDefines[Gas.WaterVapor] * waterFixed;
				dynamicHeatModifier = Mathf.Max(dynamicHeatModifier, 0.5f);

				//Value between 1 and 10
				dynamicHeatResistance =
					Mathf.Max(
						(n2oCompositon * heatResistanceDefines[Gas.NitrousOxide]) + ((h2oCompositon * heatResistanceDefines[Gas.WaterVapor] * waterBonus)) +
						((pluoxiumCompositon * heatResistanceDefines[Gas.Pluoxium] * pluoxiumBonus)), 1);

				//Value between 30 and -5, used to determine radiation output as it concerns things like collectors
				var powerTransmissionBonus = plasmaCompositon * transmitDefines[Gas.Plasma];
				powerTransmissionBonus += o2Compositon * transmitDefines[Gas.Oxygen];
				powerTransmissionBonus += (h2oCompositon * transmitDefines[Gas.WaterVapor] * waterBonus);
				powerTransmissionBonus += bzCompositon * transmitDefines[Gas.BZ];
				powerTransmissionBonus += tritiumCompositon * transmitDefines[Gas.Tritium];
				powerTransmissionBonus += (pluoxiumCompositon * transmitDefines[Gas.Pluoxium] * pluoxiumBonus);
				powerTransmissionBonus *= waterMalus;

				//more moles of gases are harder to heat than fewer, so let's scale heat damage around them
				moleHeatPenalty = Mathf.Max(combinedGas / MoleHeatPenalty, 0.25f);

				//Ramps up or down in increments of 0.02 up to the proportion of co2
				//Given infinite time, powerloss_dynamic_scaling = co2comp
				//Some value between 0 and 1
				if (combinedGas > PowerlossInhibitioMoleThreshold && co2Compositon > PowerlossInhibitionGasThreshold)
				{
					//If there are more then 20 mols, or more then 20% co2
					powerlossDynamicScaling = Mathf.Clamp(powerlossDynamicScaling +
															Mathf.Clamp(co2Compositon - powerlossDynamicScaling, -0.02f, 0.02f), 0, 1);
				}
				else
				{
					powerlossDynamicScaling = Mathf.Clamp(powerlossDynamicScaling - 0.05f, 0f, 1);
				}

				//Ranges from 0 to 1(1-(value between 0 and 1 * ranges from 1 to 1.5(mol / 500)))
				//We take the mol count, and scale it to be our inhibitor
				powerlossInhibitor =
					Mathf.Clamp(
						1 - (powerlossDynamicScaling *
							 Mathf.Clamp(combinedGas / PowerlossInhibitionMoleBoostThreshold, 1, 1.5f)), 0, 1);

				//Releases stored power into the general pool
				//We get this by consuming shit or being scalpeled
				if (matterPower > 0)
				{
					//We base our removed power off one 10th of the matter_power.
					var removedMatter = Mathf.Max(matterPower / MatterPowerConversion, 40);

					//Adds at least 40 power
					power = Mathf.Max(power + removedMatter, 0);

					//Removes at least 40 matter power
					matterPower = Mathf.Max(matterPower - removedMatter, 0);
				}

				var tempFactor = 50;
				if (gasmixPowerRatio > 0.8)
				{
					//with a perfect gas mix, make the power more based on heat
					mainSprite.ChangeSprite(1);
				}
				else
				{
					//in normal mode, power is less effected by heat
					tempFactor = 30;
					mainSprite.ChangeSprite(0);
				}

				//if there is more pluox and n2 then anything else, we receive no power increase from heat
				power = Mathf.Max((removeMix.Temperature * tempFactor / 273.15f) * gasmixPowerRatio + power, 0f);

				if (DMMath.Prob(50))
				{
					var strength = power * Mathf.Max(0,
						(1 + (powerTransmissionBonus / (10 - (bzCompositon * 5))) * freonBonus));
					RadiationManager.Instance.RequestPulse( registerTile.WorldPositionServer, strength, GetInstanceID());
				}

				if (bzCompositon >= 0.4 && DMMath.Prob(30 * bzCompositon))
				{
					// Start to emit radballs at a maximum of 30% chance per tick
					FireNuclearParticle();
				}

				//Power * 0.55
				var deviceEnergy = power * ReactionPowerModifier;

				//To figure out how much temperature to add each tick, consider that at one atmosphere's worth
				//of pure oxygen, with all four lasers firing at standard energy and no N2 present, at room temperature
				//that the device energy is around 2140. At that stage, we don't want too much heat to be put out
				//Since the core is effectively "cold"

				//Also keep in mind we are only adding this temperature to (efficiency)% of the one tile the rock
				//is on. An increase of 4*C @ 25% efficiency here results in an increase of 1*C / (#tilesincore) overall.
				//Power * 0.55 * (some value between 1.5 and 23) / 5
				removeMix.ChangeTemperature((deviceEnergy * dynamicHeatModifier) / ThermalReleaseModifier);
				//We can only emit so much heat, that being 57500
				//TODO this removes heat if it is above 57500, is that what we want?
				removeMix.SetTemperature(Mathf.Max(0, Mathf.Min(removeMix.Temperature, 2500 * dynamicHeatModifier)));

				//Calculate how much gas to release
				//Varies based on power and gas content
				removeMix.AddGas(Gas.Plasma, Mathf.Max((deviceEnergy * dynamicHeatModifier) / PlasmaReleaseModifier, 0));

				//Varies based on power, gas content, and heat
				removeMix.AddGas(Gas.Oxygen, Mathf.Max(((deviceEnergy + removeMix.Temperature * dynamicHeatModifier) - 273.15f) / OxygenReleaseModifier,
						0));

				//Return gas to tile
				GasMix.TransferGas(gasMix, removeMix, removeMix.Moles);
			}

			//Transitions between one function and another, one we use for the fast initial startup, the other is used to prevent errors with fusion temperatures.
			//Use of the second function improves the power gain imparted by using co2
			power -= Mathf.Min(Mathf.Pow(power / 500, 3f) * powerlossInhibitor, power * 0.83f * powerlossInhibitor);

			//Reset to 0, just in case
			if (power < 0)
			{
				power = 0;
			}
		}

		private void CalculateDamage()
		{
			//Due to DAMAGE_INCREASE_MULTIPLIER, we only deal one 4th of the damage the statements otherwise would cause
			//((((some value between 0.5 and 1 * temp - ((273.15 + 40) * some values between 1 and 6)) * some number between 0.25 and knock your socks off / 150) * 0.25
			//Heat and moles account for each other, a lot of hot moles are more damaging then a few
			//Moles start to have a positive effect on damage after 350
			superMatterIntegrity -= (Mathf.Max(Mathf.Clamp(removeMix.Moles / 200f, 0.5f, 1f) * removeMix.Temperature -
											   ((273.15f + HeatPenaltyThreshold) * dynamicHeatResistance), 0f)
										* moleHeatPenalty / 150f) * DamageIncreaseMultiplier;

			//Power only starts affecting damage when it is above 5000
			superMatterIntegrity -= (Mathf.Max(power - PowerPenaltyThreshold, 0) / 500) * DamageIncreaseMultiplier;

			//Molar count only starts affecting damage when it is above 1800
			superMatterIntegrity -= (Mathf.Max(combinedGas - MolePenaltyThreshold, 0) / 80) * DamageIncreaseMultiplier;

			//Only heals damage when the temp is below 313.15
			var healingAmount = (273.15f + HeatPenaltyThreshold) - removeMix.Temperature;

			if (combinedGas < MolePenaltyThreshold && healingAmount > 0)
			{
				superMatterIntegrity += healingAmount / 150;
			}

			//Reset to 0, just in case
			if (superMatterIntegrity < 0)
			{
				superMatterIntegrity = 0;
			}

			//Caps integrity change to previous integrity -+ 4, per update
			superMatterIntegrity = Mathf.Clamp(superMatterIntegrity,
				previousIntegrity - (DamageHardcap * superMatterMaxIntegrity),
				previousIntegrity + (DamageHardcap * superMatterMaxIntegrity));

			//Dont go over max
			superMatterIntegrity = Mathf.Min(superMatterIntegrity, superMatterMaxIntegrity);
		}

		#endregion

		#region CheckEffects

		private void CheckEffects()
		{
			//Handle high power zaps/anomaly generation
			//If the power is above 5000 or if the damage is above 550
			if (power > PowerPenaltyThreshold || superMatterIntegrity < DamagePenaltyPoint)
			{
				//Lightning has min range of 4
				primaryRange = 4;

				if (removeMix.Moles > 0 && removeMix.Pressure > 0 && removeMix.Temperature > 0)
				{
					//We should always be able to zap our way out of the default enclosure
					primaryRange = (int)Mathf.Clamp(power / removeMix.Pressure * 10, 2, 7);
				}

				var zapCount = 2;

				if (power > CriticalPowerPenaltyThreshold)
				{
					zapCount = 4;
				}
				else if (power > SeverePowerPenaltyThreshold)
				{
					zapCount = 3;
				}

				//Now we deal with damage
				if (superMatterIntegrity < DamagePenaltyPoint && DMMath.Prob(20))
				{
					zapCount += 1;
				}

				LightningObjects(zapCount);

				if (DMMath.Prob(5))
				{
					TrySpawnAnomaly(fluxAnomaly);
				}

				if (power > SeverePowerPenaltyThreshold && DMMath.Prob(5) || DMMath.Prob(1))
				{
					TrySpawnAnomaly(gravitationalAnomaly);
				}

				if ((power > SeverePowerPenaltyThreshold && DMMath.Prob(2)) || (DMMath.Prob(0.3) && power > PowerPenaltyThreshold))
				{
					TrySpawnAnomaly(pyroAnomaly);
				}
			}
		}

		//Tries to find empty tile in 10 by 10 area from supermatter
		private void TrySpawnAnomaly(GameObject prefabToSpawn)
		{
			if(prefabToSpawn == null) return;

			var pos = GetRandomTile(8);

			pos = pos ?? GetRandomTile(8);

			//Try twice to get coord, if still null then failed
			if (pos == null) return;

			Spawn.ServerPrefab(prefabToSpawn, pos.Value, transform.parent);
		}

		private Vector3Int? GetRandomTile(int range)
		{
			var overloadPrevent = 0;

			while (overloadPrevent < 20)
			{
				var pos = registerTile.WorldPositionServer;
				pos.x += Random.Range(-range, range + 1);
				pos.y += Random.Range(-range, range + 1);

				if (MatrixManager.IsEmptyAt(pos, true, registerTile.Matrix.MatrixInfo))
				{
					return pos;
				}

				overloadPrevent++;
			}

			return null;
		}

		private void FireNuclearParticle()
		{
			ProjectileManager.InstantiateAndShoot(nuclearParticlePrefab,
				VectorExtensions.DegreeToVector2(Random.Range(0, 361)), gameObject, null, BodyPartType.None);
		}

		#endregion

		#region CheckWarnings

		private void CheckWarnings()
		{
			//Tells the engi team to get their butt in gear
			// while the core is still damaged and it's still worth noting its status
			if (superMatterIntegrity < WarningPoint)
			{
				warningTimer += updateTime;
				isDelam = true;

				if (warningTimer % 15 == 0)
				{
					//Play sound every 15 seconds
					PlayAlarmSound();
				}

				if (warningTimer >= 30)
				{
					if (superMatterIntegrity < EmergencyPoint)
					{
						AddMessageToChat($"{emergencyAlertText} Integrity: {GetIntegrityPercentage()}%", true);
					}
					else if (superMatterIntegrity <= previousIntegrity)
					{
						AddMessageToChat($"{warningAlertText} Integrity: {GetIntegrityPercentage()}%");
					}
					else
					{
						AddMessageToChat($"{safeAlertText} Integrity: {GetIntegrityPercentage()}%");
					}

					if (power > PowerPenaltyThreshold)
					{
						AddMessageToChat("Warning: Hyperstructure has reached dangerous power level");

						if (powerlossInhibitor < 0.5)
						{
							AddMessageToChat("DANGER: CHARGE INERTIA CHAIN REACTION IN PROGRESS");
						}
					}

					if (combinedGas > MolePenaltyThreshold)
					{
						AddMessageToChat("Warning: Critical coolant mass reached");
					}

					warningTimer = 0;
				}

				//Boom, you done goofed
				if (superMatterIntegrity < ExplosionPoint)
				{
					TriggerFinalCountdown();
				}
			}
			else
			{
				isDelam = false;
			}
		}

		#endregion

		#region FinalCountdown

		private void TriggerFinalCountdown()
		{
			if(finalCountdown) return;
			finalCountdown = true;

			//Turn on shield overlay
			overlaySpriteHandler.ChangeSprite(0);

			AddMessageToChat($"{emergencyAlertText} The supermatter has reached critical integrity failure. Emergency causality destabilization field has been activated.", true);

			StartCoroutine(FinalCountdown());
		}

		private IEnumerator FinalCountdown()
		{
			for (int i = 0; i < finalCountdownTime; i++)
			{
				if (superMatterIntegrity > ExplosionPoint)
				{
					//Was stabilised, woo!
					AddMessageToChat($"{safeAlertText} Failsafe has been disengaged, all systems stabilised", true);
					overlaySpriteHandler.PushClear();
					finalCountdown = false;
					yield break;
				}

				// A message once every 5 seconds until the final 5 seconds which count down individually
				if (i < finalCountdownTime - 5 && i % 5 == 0)
				{
					AddMessageToChat($"{finalCountdownTime - i} seconds remain before causality destabilization.", true);
				}
				else if (i >= finalCountdownTime - 5)
				{
					AddMessageToChat($"{finalCountdownTime - i}...", true);
				}

				//Wait one second
				yield return WaitFor.Seconds(1);
			}

			Explode();
		}

		#endregion

		#region Explode

		private void Explode()
		{
			SendMessageToAllPlayers("<color=red>You feel reality distort for a moment...</color>");

			if (combinedGas > MolePenaltyThreshold)
			{
				//Spawns a singularity which can eat the crystal...
				SendMessageToAllPlayers("<color=red>A horrible screeching fills your ears, and a wave of dread washes over you...</color>");
				Spawn.ServerPrefab(singularity, registerTile.WorldPosition, transform.parent);

				ScoreMachine.AddNewScoreEntry(LOOSE_SCORE, "Singularity created", ScoreMachine.ScoreType.Int, ScoreCategory.StationScore);
				ScoreMachine.AddToScoreInt(scoreForReleasingSingularity, LOOSE_SCORE);

				//Dont explode if singularity is spawned
				return;
			}

			if (power > PowerPenaltyThreshold)
			{
				//Spawns an energy ball
				Spawn.ServerPrefab(energyBall, registerTile.WorldPosition, transform.parent);
			}

			RadiationManager.Instance.RequestPulse( registerTile.LocalPositionServer, detonationRads, GetInstanceID());

			Explosion.StartExplosion(registerTile.WorldPositionServer, explosionStrength);

			_ = Despawn.ServerSingle(gameObject);
		}

		#endregion

		#region ShootLightning

		private void LightningObjects(int zapCount)
		{
			var objectsToShoot = new List<GameObject>();

			var machines = GetNearbyEntities(registerTile.WorldPositionServer, mask, primaryRange).ToList();

			foreach (Collider2D entity in machines)
			{
				if (entity.gameObject == gameObject) continue;

				objectsToShoot.Add(entity.gameObject);
			}

			//Dont shoot dead stuff so it doesnt loop
			objectsToShoot = objectsToShoot.Where(o => o.TryGetComponent<LivingHealthMasterBase>(out var health) == false || health.IsDead == false).ToList();

			for (int i = 0; i < zapCount; i++)
			{
				var target = GetTarget(objectsToShoot, doTeslaFirst: false);

				if (target == null)
				{
					//If no target objects shoot random tile instead
					var pos = GetRandomTile(primaryRange);
					if(pos == null) continue;

					Zap(gameObject, null, Random.Range(1,3), pos.Value);
					continue;
				}

				ShootLightning(target);

				objectsToShoot.Remove(target);
			}
		}

		private GameObject GetTarget(List<GameObject> objectsToShoot, bool random = true, bool doTeslaFirst = true)
		{
			if (objectsToShoot.Count == 0) return null;

			var teslaCoils = objectsToShoot.Where(o => o.TryGetComponent<TeslaCoil>(out var teslaCoil) && teslaCoil != null && teslaCoil.IsWrenched).ToList();

			if (teslaCoils.Any())
			{
				var groundingRods = teslaCoils.Where(o => o.TryGetComponent<TeslaCoil>(out var teslaCoil) && teslaCoil != null &&
														  teslaCoil.CurrentState == TeslaCoil.TeslaCoilState.Grounding).ToList();

				if (doTeslaFirst == false)
				{
					return groundingRods.Any() ? groundingRods.PickRandom() : objectsToShoot.PickRandom();
				}

				if (random)
				{
					return groundingRods.Any() ? groundingRods.PickRandom() : teslaCoils.PickRandom();
				}

				return groundingRods.Any() ? groundingRods[0] : teslaCoils[0];
			}

			return random ? objectsToShoot.PickRandom() : objectsToShoot[0];
		}

		private void ShootLightning(GameObject targetObject)
		{
			GameObject primaryTarget = ZapPrimaryTarget(targetObject);
			if (primaryTarget != null)
			{
				ZapSecondaryTargets(primaryTarget, targetObject.AssumedWorldPosServer());
			}
		}

		private GameObject ZapPrimaryTarget(GameObject targetObject)
		{
			Zap(gameObject, targetObject, Random.Range(1,3), targetObject == null ? targetObject.AssumedWorldPosServer() : default);

			SoundManager.PlayNetworkedAtPos(lightningSound, targetObject == null ? targetObject.AssumedWorldPosServer() : default, sourceObj: targetObject);

			return targetObject;
		}

		private void ZapSecondaryTargets(GameObject originatingObject, Vector3 centrepoint)
		{
			var ignored = new GameObject[2] { gameObject, originatingObject };

			var targets = new List<GameObject>();

			foreach (var entity in GetNearbyEntities(centrepoint, mask, secondaryRange, ignored))
			{
				targets.Add(entity.gameObject);
			}

			for (int i = 0; i < Random.Range(1, secondaryArcCount + 1); i++)
			{
				if(targets.Count == 0) break;

				var target = GetTarget(targets, false);
				Zap(originatingObject, target, 1);
				targets.Remove(target);
			}
		}

		private void Zap(GameObject originatingObject, GameObject targetObject, int arcs, Vector3 targetPosition = default)
		{
			ElectricalArcSettings arcSettings = new ElectricalArcSettings(
					arcEffect, originatingObject, targetObject, default, targetPosition, arcs, duration,
					false);

			if (targetObject != null)
			{
				var interfaces = targetObject.GetComponents<IOnLightningHit>();

				foreach (var lightningHit in interfaces)
				{
					lightningHit.OnLightningHit(duration + 1, Mathf.Clamp(power * 2, 4000, 20000));
				}
			}

			ElectricalArc.ServerCreateNetworkedArcs(arcSettings).OnArcPulse += OnPulse;
		}

		private void OnPulse(ElectricalArc arc)
		{
			if (arc.Settings.endObject == null || gameObject == null) return;

			if (arc.Settings.endObject.TryGetComponent<LivingHealthMasterBase>(out var health) && health != null)
			{
				health.ApplyDamageAll(gameObject, Mathf.Clamp(power * 2, 4000, 20000), AttackType.Magic, DamageType.Burn);
			}
			else if (arc.Settings.endObject.TryGetComponent<Integrity>(out var integrity) && integrity != null && integrity.Resistances.LightningDamageProof == false)
			{
				integrity.ApplyDamage(Mathf.Clamp(power * 2, 4000, 20000), AttackType.Magic, DamageType.Burn, true, explodeOnDestroy: true);
			}
		}

		#region Helpers

		private T GetFirstAt<T>(Vector3Int position) where T : MonoBehaviour
		{
			return MatrixManager.GetAt<T>(position, true).FirstOrDefault();
		}

		private MatrixManager.CustomPhysicsHit RaycastToTarget(Vector3 start, Vector3 end)
		{
			return MatrixManager.RayCast(start, default, primaryRange,
					LayerTypeSelection.Walls, LayerMask.GetMask("Door Closed"),
					end);
		}

		private IEnumerable<Collider2D> GetNearbyEntities(Vector3 centrepoint, int mask, int range, GameObject[] ignored = default)
		{
			Collider2D[] entities = Physics2D.OverlapCircleAll(centrepoint, range, mask);
			foreach (Collider2D coll in entities)
			{
				if (ignored != null && ignored.Contains(coll.gameObject)) continue;

				if (RaycastToTarget(centrepoint, coll.transform.position).ItHit == false)
				{
					yield return coll;
				}
			}
		}

		#endregion

		#endregion

		#region ChatMessages

		private void AddMessageToChat(string message, bool sendToCommon = false)
		{
			ChatEvent chatEvent = new ChatEvent();
			chatEvent.message = message;
			chatEvent.speaker = "Supermatter Warning System: ";
			chatEvent.VoiceLevel = Loudness.SCREAMING;
			chatEvent.position = registerTile.WorldPositionServer;
			chatEvent.originator = gameObject;

			chatEvent.channels = ChatChannel.Engineering;
			if (sendToCommon) chatEvent.channels |= ChatChannel.Common;

			InfluenceChat(chatEvent);

		}

		protected override bool SendSignalLogic()
		{
			return true;
		}

		public override void SignalFailed() { }

		public bool WillInfluenceChat()
		{
			return true;
		}

		public ChatEvent InfluenceChat(ChatEvent chatToManipulate)
		{
			CommsServer.RadioMessageData msg = new CommsServer.RadioMessageData();

			msg.ChatEvent = chatToManipulate;
			TrySendSignal(radioSO, msg);
			return chatToManipulate;
		}

		private void SendMessageToAllPlayers(string message)
		{
			foreach (var player in PlayerList.Instance.InGamePlayers)
			{
				Chat.AddExamineMsgFromServer(player.GameObject, message);
			}
		}

		#endregion

		#region Hit, Bump, Collision

		//Called when hit by projectiles
		public void OnHitDetect(OnHitDetectData data)
		{
			if (isHugBox) return;

			//Increase power if emitter projectile
			if (data.BulletName == emitterBulletName)
			{
				//Plus 10 as our lasers do 20 but TGs do 30, but the laser strength works fine for field generators
				power += (data.DamageData.Damage + 10) * 2;
				return;
			}

			if(canTakeIntegrityDamage == false) return;

			//Else do integrity damage
			superMatterIntegrity -= data.DamageData.Damage * 2;
		}

		//Called when bumped by players or collided with by flying items
		public void OnBump(GameObject bumpedBy, GameObject client)
		{
			if (isServer == false || isHugBox) return;

			if (bumpedBy.TryGetComponent<PlayerHealthV2>(out var playerHealth))
			{
				//Players, you big idiot
				var job = bumpedBy.GetComponent<PlayerScript>().Mind?.occupation;

				Chat.AddActionMsgToChat(bumpedBy,
					$"You slam into the {gameObject.ExpensiveName()} as your ears are filled with unearthly ringing. Your last thought is 'Oh, fuck.'",
					$"The {(job != null ? job.JobType.JobString() : "person")} slams into the {gameObject.ExpensiveName()} inducing a resonance... {bumpedBy.ExpensiveName()} body starts to glow and burst into flames before flashing into dust!");

				playerHealth.OnGib();
				matterPower += 100;
			}
			else if (bumpedBy.TryGetComponent<LivingHealthMasterBase>(out var health))
			{
				//Npcs
				Chat.AddActionMsgToChat(bumpedBy, $"The {bumpedBy.ExpensiveName()} slams into the {gameObject.ExpensiveName()} inducing a resonance... " +
													"its body starts to glow and burst into flames before flashing into dust!");

				health.OnGib();
			}
			else if (bumpedBy.TryGetComponent<Integrity>(out var integrity))
			{
				//Items flying
				Chat.AddActionMsgToChat(bumpedBy, $"The {bumpedBy.ExpensiveName()} smacks into the {gameObject.ExpensiveName()} and rapidly flashes to ash!");
				LogBumpForAdmin(bumpedBy);

				integrity.ApplyDamage(1000, AttackType.Rad, DamageType.Brute, true, ignoreArmor: true);
			}

			matterPower += 150;
			RadiationManager.Instance.RequestPulse( registerTile.WorldPositionServer, 200, GetInstanceID());
			SoundManager.PlayNetworkedAtPos(lightningSound, registerTile.WorldPositionServer, sourceObj: gameObject);
		}

		private void LogBumpForAdmin(GameObject thrownObject)
		{
			if (thrownObject.TryGetComponent<LastTouch>(out var touch) == false || touch.LastTouchedBy == null) return;
			var time = DateTime.Now.ToString(CultureInfo.InvariantCulture);
			PlayerAlerts.LogPlayerAction(time, PlayerAlertTypes.RDM, touch.LastTouchedBy,
				$"{time} : A {thrownObject.ExpensiveName()} was thrown at a super-matter and was last touched by {touch.LastTouchedBy.Script.playerName} ({touch.LastTouchedBy.Username}).");
		}

		#endregion

		#region HandApply

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			//Dont destroy wrench, it is used to unwrench crystal
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench)) return false;

			//We dont want to vaporize unvaporizible things
			if (Validations.HasItemTrait(interaction.HandObject, superMatterSliver) || Validations.HasItemTrait(interaction.HandObject, superMatterTongs)) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			//Kill player if they touched with empty hand
			if (interaction.HandObject == null)
			{
				RadiationManager.Instance.RequestPulse(registerTile.WorldPositionServer, 200, GetInstanceID());

				if (isHugBox && DMMath.Prob(95))
				{
					Chat.AddExamineMsg(interaction.Performer, "You're lucky, that could have gone very badly");
					interaction.Performer.GetComponent<RegisterPlayer>().ServerStun();
					return;
				}

				Chat.AddActionMsgToChat(interaction.Performer,
					$"You reach out and touch {gameObject.ExpensiveName()}. Everything starts burning and all you can hear is ringing. Your last thought is 'That was not a wise decision'",
					$"{interaction.Performer.ExpensiveName()} reaches out and touches {gameObject.ExpensiveName()}, inducing a resonance... {interaction.Performer.ExpensiveName()} body starts to glow and burst into flames before flashing into dust!");

				interaction.Performer.GetComponent<PlayerHealthV2>().OnGib();
				matterPower += 200;
				return;
			}

			//Try removing piece if using supermatter scalpel
			if (Validations.HasItemTrait(interaction.HandObject, superMatterScalpel))
			{
				ToolUtils.ServerUseToolWithActionMessages(interaction, 30,
					$"You carefully begin to scrape the {gameObject.ExpensiveName()} with the {interaction.HandObject.ExpensiveName()}...",
					$"{interaction.Performer.ExpensiveName()} starts scraping off a part of the {gameObject.ExpensiveName()}...",
					$"You extract a sliver from the {gameObject.ExpensiveName()}. <color=red>The {gameObject.ExpensiveName()} begins to react violently!</color>",
					$"{interaction.Performer.ExpensiveName()} scrapes off a shard from the {gameObject.ExpensiveName()}.",
					() =>
					{
						Spawn.ServerPrefab(superMatterShard, interaction.Performer.AssumedWorldPosServer(),
							interaction.Performer.transform.parent);
						matterPower += 800;

						//Destroy Scalpel
						Chat.AddExamineMsgFromServer(interaction.Performer, $"A tiny piece of the {interaction.HandObject.ExpensiveName()} falls off, rendering it useless!");
						_ = Despawn.ServerSingle(interaction.HandObject);
					}
				);

				return;
			}

			//Else destroy the item the supermatter was touched with
			Chat.AddActionMsgToChat(interaction.Performer,
				$"You touch the {gameObject.ExpensiveName()} with the {interaction.HandObject.ExpensiveName()}, and everything suddenly goes silent.\n The {interaction.HandObject.ExpensiveName()} flashes into dust as you flinch away from the {gameObject.ExpensiveName()}.",
				$"As {interaction.Performer.ExpensiveName()} touches the {gameObject.ExpensiveName()} with {interaction.HandObject.ExpensiveName()}, silence fills the room...");
			_ = Despawn.ServerSingle(interaction.HandObject);
			RadiationManager.Instance.RequestPulse(registerTile.WorldPositionServer, 150, GetInstanceID());
			SoundManager.PlayNetworkedAtPos(lightningSound, registerTile.WorldPositionServer, sourceObj: gameObject);
			matterPower += 200;
		}

		#endregion

		#region Status

		private void PlayAlarmSound()
		{
			switch (GetStatus())
			{
				case SuperMatterStatus.Delaminating:
					SoundManager.PlayNetworkedAtPos(blobAlarm, registerTile.WorldPositionServer, sourceObj: gameObject);
					break;
				case SuperMatterStatus.Emergency:
					SoundManager.PlayNetworkedAtPos(engineAlert1, registerTile.WorldPositionServer, sourceObj: gameObject);
					break;
				case SuperMatterStatus.Danger:
					SoundManager.PlayNetworkedAtPos(engineAlert2, registerTile.WorldPositionServer, sourceObj: gameObject);
					break;
				case SuperMatterStatus.Warning:
					SoundManager.PlayNetworkedAtPos(terminalAlert, registerTile.WorldPositionServer, sourceObj: gameObject);
					break;
			}
		}

		private SuperMatterStatus GetStatus()
		{
			var node = registerTile.Matrix.GetMetaDataNode(registerTile.LocalPositionServer, false);

			if (node == null) return SuperMatterStatus.Error;

			var gas = node.GasMix;

			var integrityPercentage = GetIntegrityPercentage();

			if (integrityPercentage < SupermatterDelamPercent)
				return SuperMatterStatus.Delaminating;

			if (integrityPercentage < SupermatterEmergencyPercent)
				return SuperMatterStatus.Emergency;

			if (integrityPercentage < SupermatterDangerPercent)
				return SuperMatterStatus.Danger;

			if ((integrityPercentage < SupermatterWarningPercent) || (gas.Temperature > CriticalTemperature))
				return SuperMatterStatus.Warning;

			if (gas.Temperature > (CriticalTemperature * 0.8))
				return SuperMatterStatus.Notify;

			if (power > 5)
				return SuperMatterStatus.Normal;

			return SuperMatterStatus.Inactive;
		}

		private float GetIntegrityPercentage()
		{
			var matterIntegrity = superMatterIntegrity / superMatterMaxIntegrity;

			matterIntegrity *= 100;
			matterIntegrity = matterIntegrity < 0 ? 0 : matterIntegrity;

			return matterIntegrity;
		}

		#endregion

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			return "<color=red>You get headaches just from looking at it</color>";
		}

		private enum SuperMatterStatus
		{
			Error,
			Inactive,
			Normal,
			Notify,
			Warning,
			Danger,
			Emergency,
			Delaminating
		}
	}
}

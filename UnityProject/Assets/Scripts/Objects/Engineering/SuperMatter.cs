using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Systems.Atmospherics;
using Systems.ElectricalArcs;
using Systems.Explosions;
using Systems.Radiation;
using AddressableReferences;
using Light2D;
using Mirror;
using ScriptableObjects.Gun;
using SoundMessages;
using UnityEngine;
using Weapons.Projectiles.Behaviours;
using Random = UnityEngine.Random;

namespace Objects.Engineering
{
	public class SuperMatter : NetworkBehaviour, IOnHitDetect, IExaminable
	{
		#region lightSpriteDefines

		[SerializeField]
		private LightSprite lightSprite;

		[SerializeField]
		private float pulseSpeed = 1f; //here, a value of 0.5f would take 2 seconds and a value of 2f would take half a second

		private const float MAXIntensity = 0.9f; // Max alpha is 1f, but lower so not blinding
		private const float MINIntensity = 0.1f; // Min alpha is 0f, 0.1f so light doesnt go away completely

		private float targetIntensity = 1f;
		private float currentIntensity;

		private Vector3 startingLightScale = new Vector3(1, 1, 1);

		#endregion

		#region OverlaySprite

		[SerializeField]
		private SpriteHandler mainSprite = null;

		[SerializeField]
		private SpriteHandler overlaySpriteHandler = null;

		#endregion

		#region HeatPenaltyDefines

		// Higher == Bigger heat and waste penalty from having the crystal surrounded by this gas. Negative numbers reduce penalty.
		private Dictionary<Gas, float> heatPenaltyDefines = new Dictionary<Gas, float>
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

		#endregion

		#region TransmitDefines

		//All of these get divided by 10-bzcomp * 5 before having 1 added and being multiplied with power to determine rads
		//Keep the negative values here above -10 and we won't get negative rads
		//Higher == Bigger bonus to power generation.
		private Dictionary<Gas, float> transmitDefines = new Dictionary<Gas, float>
		{
			{Gas.Oxygen, 1.5f},
			{Gas.Plasma, 4},
			{Gas.BZ, -2},
			{Gas.Tritium, 30},
			{Gas.Pluoxium, -5},
			{Gas.WaterVapor, -9}
		};

		#endregion

		#region HeatResistanceDefines

		//Higher == Gas makes the crystal more resistant against heat damage.
		private Dictionary<Gas, float> heatResistanceDefines = new Dictionary<Gas, float>
		{
			{Gas.NitrousOxide, 6},
			{Gas.Pluoxium, 3},
			{Gas.WaterVapor, 10}
		};

		#endregion

		#region WarningDefines

		//If integrity percent remaining is less than these values, the monitor sets off the relevant alarm.
		private const int SupermatterDelamPercent =  5;
		private const int SupermatterEmergencyPercent = 25;
		private const int SupermatterDangerPercent = 50;
		private const int SupermatterWarningPercent = 100;

		private string safe_alert = "Crystalline hyperstructure returning to safe operating parameters.";
		private const int warning_point = 950;
		private string warning_alert = "Danger! Crystal hyperstructure integrity faltering!";
		private const int damage_penalty_point = 450;
		private const int emergency_point = 300;
		private string emergency_alert = "CRYSTAL DELAMINATION IMMINENT.";
		private const int explosion_point = 50;

		#endregion

		#region AnomalyDefines

		[SerializeField]
		private GameObject singularity = null;

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
		private const float DamageHardcap = 0.002f;
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

		#endregion

		#region CompositionDefines

		// raw composition of each gas in the chamber, ranges from 0 to 1
		private float n2comp = 0;
		private float plasmacomp = 0;
		private float o2comp = 0;
		private float co2comp = 0;
		private float n2ocomp = 0;
		private float tritiumcomp = 0;
		private float bzcomp = 0;
		private float pluoxiumcomp = 0;
		private float h2ocomp = 0;
		private float freoncomp = 0;

		///Determines the rate of positive change in gas comp values
		private float gas_change_rate = 0.05f;

		///The last air sample's total molar count, will always be above or equal to 0
		private float combined_gas = 0;

		private float gasmix_power_ratio = 0;
		private float dynamic_heat_modifier = 1;
		private float dynamic_heat_resistance = 1;
		private float powerloss_inhibitor = 1;
		private float powerloss_dynamic_scaling = 0;
		private float power_transmission_bonus = 0;
		private float mole_heat_penalty = 0;
		private float matter_power = 0;

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
		[SerializeField, Range(3, 12)]
		private int primaryRange = 10;
		[SerializeField, Range(3, 12)]
		private int secondaryRange = 4;

		[SerializeField]
		private AddressableAudioSource lightningSound = null;

		private int mask;

		#endregion

		[SerializeField]
		private bool canTakeIntegrityDamage = true;

		private float superMatterMaxIntegrity = 1000f;
		private float superMatterIntegrity = 1000f;
		private float previousIntegrity;

		private float power;
		private float warningTimer;

		private RegisterTile registerTile;
		private Integrity integrity;

		private GasMix removeMix;

		private bool finalCountdown; //uh oh
		private int finalCountdownTime = 30; //30 seconds

		private float updateTime = 0.5f;

		[SyncVar(hook = nameof(SyncIsDelam))]
		private bool isDelam;

		#region SoundDefines

		private AudioSourceParameters soundParameters = new AudioSourceParameters();

		private string loopingSoundGuid = "";

		[SerializeField]
		private AddressableAudioSource normalLoopSound = null;
		[SerializeField]
		private AddressableAudioSource delamLoopSound = null;

		#endregion

		#region LifeCycle

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			integrity = GetComponent<Integrity>();
			mask = LayerMask.GetMask("Machines", "WallMounts", "Objects", "Players", "NPC");
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
			UpdateManager.Add(CallbackType.UPDATE, SuperMatterLightUpdate);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, SuperMatterUpdate);
			UpdateManager.Remove(CallbackType.UPDATE, SuperMatterLightUpdate);
			SoundManager.Stop(loopingSoundGuid);
		}

		[Client]
		private void SyncIsDelam(bool oldVar, bool newVar)
		{
			if (newVar)
			{
				SoundManager.Stop(loopingSoundGuid);
				loopingSoundGuid = Guid.NewGuid().ToString();
				SoundManager.PlayAtPosition(delamLoopSound, registerTile.WorldPositionServer, gameObject, loopingSoundGuid);
			}
			else
			{
				SoundManager.Stop(loopingSoundGuid);
				loopingSoundGuid = Guid.NewGuid().ToString();
				SoundManager.PlayAtPosition(normalLoopSound, registerTile.WorldPositionServer, gameObject, loopingSoundGuid);
			}
		}

		#endregion

		#region UpdateLoop

		private void SuperMatterUpdate()
		{
			if(CustomNetworkManager.IsServer == false) return;

			CheckPower();

			CheckEffects();

			CheckWarnings();
		}

		private void SuperMatterLightUpdate()
		{
			//Looping the light alpha to create a pulsing effect
			currentIntensity = Mathf.MoveTowards(lightSprite.Color.a, targetIntensity, Time.deltaTime * pulseSpeed);

			if(currentIntensity >= MAXIntensity)
			{
				currentIntensity = MAXIntensity;
				targetIntensity = MINIntensity;
			}
			else if(currentIntensity <= MINIntensity)
			{
				currentIntensity = MINIntensity;
				targetIntensity = MAXIntensity;
			}

			lightSprite.Color.a = currentIntensity;
		}

		#endregion

		#region CheckPower

		private void CheckPower()
		{
			var gasNode = registerTile.Matrix.GetMetaDataNode(registerTile.LocalPositionServer, false);
			if(gasNode == null) return;

			var gasMix = gasNode.GasMix;

			removeMix = gasMix.RemoveGas(gasMix, 0.15f * gasMix.Moles);

			previousIntegrity = superMatterIntegrity;

			var newIntegrity = superMatterIntegrity;

			if (removeMix.Moles == 0 || registerTile.Matrix.IsSpaceAt(registerTile.WorldPositionServer, true))
			{
				//Always does at least some integrity damage if allowed
				superMatterIntegrity = canTakeIntegrityDamage ? superMatterIntegrity - Mathf.Max((power / 1000) * DamageIncreaseMultiplier, 0.1f) : newIntegrity;
			}
			else
			{
				//If allowed to take integrity damage, calculate it
				superMatterIntegrity = canTakeIntegrityDamage ? CalculateDamage(newIntegrity) : newIntegrity;

				combined_gas = Mathf.Max(removeMix.Moles, 0);

				//Lets get the proportions of the gasses in the mix and then slowly move our comp to that value
				//Can cause an overestimation of mol count, should stabilize things though.
				//Prevents huge bursts of gas/heat when a large amount of something is introduced
				//They range between 0 and 1
				plasmacomp += Mathf.Clamp(Mathf.Max(removeMix.GetMoles(Gas.Plasma) / combined_gas, 0) - plasmacomp, -1,gas_change_rate);
				o2comp += Mathf.Clamp(Mathf.Max(removeMix.GetMoles(Gas.Oxygen) / combined_gas, 0) - o2comp, -1, gas_change_rate);
				co2comp += Mathf.Clamp(Mathf.Max(removeMix.GetMoles(Gas.CarbonDioxide) / combined_gas, 0) - co2comp, -1, gas_change_rate);
				pluoxiumcomp += Mathf.Clamp(Mathf.Max(removeMix.GetMoles(Gas.Pluoxium) / combined_gas, 0) - pluoxiumcomp, -1, gas_change_rate);
				tritiumcomp += Mathf.Clamp(Mathf.Max(removeMix.GetMoles(Gas.Tritium) / combined_gas, 0) - tritiumcomp, -1, gas_change_rate);
				bzcomp += Mathf.Clamp(Mathf.Max(removeMix.GetMoles(Gas.BZ) / combined_gas, 0) - bzcomp, -1, gas_change_rate);
				n2ocomp += Mathf.Clamp(Mathf.Max(removeMix.GetMoles(Gas.NitrousOxide) / combined_gas, 0) - n2ocomp, -1, gas_change_rate);
				n2comp += Mathf.Clamp(Mathf.Max(removeMix.GetMoles(Gas.Nitrogen) / combined_gas, 0) - n2comp, -1, gas_change_rate);
				h2ocomp += Mathf.Clamp(Mathf.Max(removeMix.GetMoles(Gas.WaterVapor) / combined_gas, 0) - h2ocomp, -1, gas_change_rate);
				freoncomp += Mathf.Clamp(Mathf.Max(removeMix.GetMoles(Gas.Freon) / combined_gas, 0) - freoncomp, -1, gas_change_rate);



				//No less then zero, and no greater then one, we use this to do explosions and heat to power transfer
				gasmix_power_ratio =
					Mathf.Min(
						Mathf.Max(
							((plasmacomp + o2comp + co2comp + h2ocomp + tritiumcomp + bzcomp - pluoxiumcomp - n2comp -
							  freoncomp)), 0), 1);

				//Minimum value of 1.5, maximum value of 23
				dynamic_heat_modifier = plasmacomp * heatPenaltyDefines[Gas.Plasma];
				dynamic_heat_modifier += o2comp * heatPenaltyDefines[Gas.Oxygen];
				dynamic_heat_modifier += co2comp * heatPenaltyDefines[Gas.CarbonDioxide];
				dynamic_heat_modifier += tritiumcomp * heatPenaltyDefines[Gas.Tritium];
				dynamic_heat_modifier += pluoxiumcomp * heatPenaltyDefines[Gas.Pluoxium];
				dynamic_heat_modifier += n2comp * heatPenaltyDefines[Gas.Nitrogen];
				dynamic_heat_modifier += bzcomp * heatPenaltyDefines[Gas.BZ];
				dynamic_heat_modifier += freoncomp * heatPenaltyDefines[Gas.Freon];
				dynamic_heat_modifier += h2ocomp * heatPenaltyDefines[Gas.WaterVapor];
				dynamic_heat_modifier = Mathf.Max(dynamic_heat_modifier, 0.5f);

				//Value between 1 and 10
				dynamic_heat_resistance =
					Mathf.Max(
						(n2ocomp * heatResistanceDefines[Gas.NitrousOxide]) + ((h2ocomp * heatResistanceDefines[Gas.WaterVapor])) +
						((pluoxiumcomp * heatResistanceDefines[Gas.Pluoxium])), 1);
				//Value between 30 and -5, used to determine radiation output as it concerns things like collectors
				power_transmission_bonus = plasmacomp * transmitDefines[Gas.Plasma];
				power_transmission_bonus += o2comp * transmitDefines[Gas.Oxygen];
				power_transmission_bonus += (h2ocomp * transmitDefines[Gas.WaterVapor]);
				power_transmission_bonus += bzcomp * transmitDefines[Gas.BZ];
				power_transmission_bonus += tritiumcomp * transmitDefines[Gas.Tritium];
				power_transmission_bonus += (pluoxiumcomp * transmitDefines[Gas.Pluoxium]);

				//more moles of gases are harder to heat than fewer, so let's scale heat damage around them
				mole_heat_penalty = Mathf.Max(combined_gas / MoleHeatPenalty, 0.25f);

				//Ramps up or down in increments of 0.02 up to the proportion of co2
				//Given infinite time, powerloss_dynamic_scaling = co2comp
				//Some value between 0 and 1
				if (combined_gas > PowerlossInhibitioMoleThreshold && co2comp > PowerlossInhibitionGasThreshold)
				{
					//If there are more then 20 mols, or more then 20% co2
					powerloss_dynamic_scaling = Mathf.Clamp(powerloss_dynamic_scaling +
					                                        Mathf.Clamp(co2comp - powerloss_dynamic_scaling, -0.02f, 0.02f), 0, 1);
				}
				else
				{
					powerloss_dynamic_scaling = Mathf.Clamp(powerloss_dynamic_scaling - 0.05f, 0f, 1);
				}

				//Ranges from 0 to 1(1-(value between 0 and 1 * ranges from 1 to 1.5(mol / 500)))
				//We take the mol count, and scale it to be our inhibitor
				powerloss_inhibitor =
					Mathf.Clamp(
						1 - (powerloss_dynamic_scaling *
						     Mathf.Clamp(combined_gas / PowerlossInhibitionMoleBoostThreshold, 1, 1.5f)), 0, 1);

				//Releases stored power into the general pool
				//We get this by consuming shit or being scalpeled
				if (matter_power != 0)
				{
					//We base our removed power off one 10th of the matter_power.
					var removedMatter = Mathf.Max(matter_power / MatterPowerConversion, 40);

					//Adds at least 40 power
					power = Mathf.Max(power + removedMatter, 0);

					//Removes at least 40 matter power
					matter_power = Mathf.Max(matter_power - removedMatter, 0);
				}

				var temp_factor = 50;
				if (gasmix_power_ratio > 0.8)
				{
					//with a perfect gas mix, make the power more based on heat
					mainSprite.ChangeSprite(1);
				}
				else
				{
					//in normal mode, power is less effected by heat
					temp_factor = 30;
					mainSprite.ChangeSprite(0);
				}

				//if there is more pluox and n2 then anything else, we receive no power increase from heat
				power = Mathf.Max((removeMix.Temperature * temp_factor / 273.15f) * gasmix_power_ratio + power, 0f);

				if (DMMath.Prob(50))
				{
					var strength = power * Mathf.Max(0,
						(1 + (power_transmission_bonus / (10 - (bzcomp * 5)))));
					RadiationManager.Instance.RequestPulse(registerTile.Matrix, registerTile.WorldPosition, strength, GetInstanceID());
				}

				if (bzcomp >= 0.4 && DMMath.Prob(30 * bzcomp))
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
				removeMix.Temperature += ((deviceEnergy * dynamic_heat_modifier) / ThermalReleaseModifier);
				//We can only emit so much heat, that being 57500
				removeMix.Temperature = Mathf.Max(0, Mathf.Min(removeMix.Temperature, 2500 * dynamic_heat_modifier));

				//Calculate how much gas to release
				//Varies based on power and gas content
				removeMix.AddGas(Gas.Plasma, Mathf.Max((deviceEnergy * dynamic_heat_modifier) / PlasmaReleaseModifier, 0));

				//Varies based on power, gas content, and heat
				removeMix.AddGas(Gas.Oxygen, Mathf.Max(
						((deviceEnergy + removeMix.Temperature * dynamic_heat_modifier) - 273.15f) / OxygenReleaseModifier,
						0));
				;

				//Return gas to tile
				gasMix.MergeGasMix(removeMix);
			}

			//Transitions between one function and another, one we use for the fast inital startup, the other is used to prevent errors with fusion temperatures.
			//Use of the second function improves the power gain imparted by using co2
			power = Mathf.Max(
				power - Mathf.Min(Mathf.Pow(power / 500, 3f) * powerloss_inhibitor, power * 0.83f * powerloss_inhibitor), 0);

			//Next is Effects
			//Then Warnings
		}

		private float CalculateDamage(float newIntegrity)
		{
			//causing damage
			//Due to DAMAGE_INCREASE_MULTIPLIER, we only deal one 4th of the damage the statements otherwise would cause

			//((((some value between 0.5 and 1 * temp - ((273.15 + 40) * some values between 1 and 6)) * some number between 0.25 and knock your socks off / 150) * 0.25
			//Heat and moles account for each other, a lot of hot moles are more damaging then a few
			//Moles start to have a positive effect on damage after 350
			newIntegrity = Mathf.Max(newIntegrity - (Mathf.Max(
					Mathf.Clamp(removeMix.Moles / 200f, 0.5f, 1f) * removeMix.Temperature -
					((273.15f + HeatPenaltyThreshold) * dynamic_heat_resistance), 0f) * mole_heat_penalty / 150f) *
				DamageIncreaseMultiplier, 0f);

			//Power only starts affecting damage when it is above 5000
			newIntegrity =
				Mathf.Max(newIntegrity - (Mathf.Max(power - PowerPenaltyThreshold, 0) / 500) * DamageIncreaseMultiplier, 0);

			//Molar count only starts affecting damage when it is above 1800
			newIntegrity =
				Mathf.Max(newIntegrity - (Mathf.Max(combined_gas - MolePenaltyThreshold, 0) / 80) * DamageIncreaseMultiplier,
					0);

			//There might be a way to integrate healing and hurting via heat
			//healing damage
			if (combined_gas < MolePenaltyThreshold)
				//Only heals damage when the temp is below 313.15, heals up to 2 damage
				newIntegrity =
					Mathf.Max(newIntegrity + (Mathf.Min(removeMix.Temperature - (273.15f + HeatPenaltyThreshold), 0) / 150),
						0);

			//caps damage rate
			//Takes the lower number between archived damage + (1.8) and damage
			//This means we can only deal 1.8 damage per function call
			return Mathf.Min(previousIntegrity - (DamageHardcap * superMatterMaxIntegrity), newIntegrity);
		}

		#endregion

		#region CheckEffects

		private void CheckEffects()
		{
			//After this point power is lowered
			//This wraps around to the begining of the function
			//Handle high power zaps/anomaly generation
			//If the power is above 5000 or if the damage is above 550
			if (power > PowerPenaltyThreshold || superMatterIntegrity < damage_penalty_point)
			{
				var range = 4f;
				var zap_cutoff = 1500f;
				if (removeMix.Moles > 0 && removeMix.Pressure > 0 && removeMix.Temperature > 0)
				{
					//You may be able to freeze the zapstate of the engine with good planning, we'll see
					//If the core is cold, it's easier to jump, ditto if there are a lot of moles
					zap_cutoff = Mathf.Clamp(3000 - (power * (removeMix.Moles) / 10) / removeMix.Temperature,
						350, 3000);
					//We should always be able to zap our way out of the default enclosure
					//See supermatter_zap() for more details
					range = Mathf.Clamp(power / removeMix.Pressure * 10, 2, 7);
				}

				var zap_count = 2;

				if (power > CriticalPowerPenaltyThreshold)
				{
					zap_count = 4;
				}
				else if (power > SeverePowerPenaltyThreshold)
				{
					zap_count = 3;
				}

				//Now we deal with damage
				if (superMatterIntegrity < damage_penalty_point && DMMath.Prob(20))
				{
					zap_count += 1;
				}

				LightningObjects(zap_count);

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

		#endregion

		#region CheckWarnings

		private void CheckWarnings()
		{

			warningTimer += updateTime;
			//Tells the engi team to get their butt in gear
			// while the core is still damaged and it's still worth noting its status
			if (superMatterIntegrity < warning_point)
			{
				isDelam = true;

				if (warningTimer >= 30)
				{
					PlayAlarmSound();

					if (superMatterIntegrity < emergency_point)
					{
						AddMessageToChat($"Integrity: {GetIntegrityPercentage()}%", true);
					}
					else if (superMatterIntegrity <= previousIntegrity)
					{
						AddMessageToChat($"Integrity: {GetIntegrityPercentage()}%");
					}
					else
					{
						AddMessageToChat($"Integrity: {GetIntegrityPercentage()}%");
					}

					if (power > PowerPenaltyThreshold)
					{
						AddMessageToChat("Warning: Hyperstructure has reached dangerous power level");

						if (powerloss_inhibitor < 0.5)
						{
							AddMessageToChat("DANGER: CHARGE INERTIA CHAIN REACTION IN PROGRESS");
						}
					}

					if (combined_gas > MolePenaltyThreshold)
					{
						AddMessageToChat("Warning: Critical coolant mass reached");
					}

					warningTimer = 0;
				}

				//Boom, you done goofed
				if (superMatterIntegrity < explosion_point)
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

			AddMessageToChat("The supermatter has reached critical integrity failure. Emergency causality destabilization field has been activated.", true);

			StartCoroutine(FinalCountdown());
		}

		private IEnumerator FinalCountdown()
		{
			for (int i = 0; i < finalCountdownTime; i++)
			{
				if (superMatterIntegrity < superMatterMaxIntegrity)
				{
					//Was stabilised, woo!
					AddMessageToChat("Failsafe has been disengaged, all systems stabilised", true);
					overlaySpriteHandler.PushClear();
					finalCountdown = false;
					yield break;
				}

				// A message once every 5 seconds until the final 5 seconds which count down individually
				if (i < finalCountdownTime - 5 && finalCountdownTime % 5 == 0)
				{
					AddMessageToChat($"{finalCountdownTime - i} remain before causality destabilization.", true);
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
			SendMessageToAllPlayers("</color=red>You feel reality distort for a moment...<color>");

			if (combined_gas > MolePenaltyThreshold)
			{
				//Spawns a singularity which can eat the crystal...
				SendMessageToAllPlayers("</color=red>A horrible screeching fills your ears, and a wave of dread washes over you...<color>");
				Spawn.ServerPrefab(singularity, registerTile.WorldPosition, transform.parent);

				//Dont explode if singularity is spawned
				return;
			}

			if (power > PowerPenaltyThreshold)
			{
				//Spawns an energy ball
				Spawn.ServerPrefab(energyBall, registerTile.WorldPosition, transform.parent);
			}

			Explosion.StartExplosion(registerTile.LocalPositionServer, 10000, registerTile.Matrix);

			Despawn.ServerSingle(gameObject);
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

			if(objectsToShoot.Count == 0) return;

			for (int i = 0; i < zapCount; i++)
			{
				var target = GetTarget(objectsToShoot, doTeslaFirst: false);

				if(target == null) break;

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
			if (arc.Settings.endObject == null) return;

			if (arc.Settings.endObject.TryGetComponent<LivingHealthBehaviour>(out var health) && health != null)
			{
				health.ApplyDamage(gameObject, Mathf.Clamp(power * 2, 4000, 20000), AttackType.Magic, DamageType.Burn);
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

		//Tries to find empty tile in 10 by 10 area from supermatter
		private void TrySpawnAnomaly(GameObject prefabToSpawn)
		{
			if(prefabToSpawn == null) return;

			var overloadPrevent = 0;

			while (overloadPrevent < 20)
			{
				var pos = registerTile.WorldPositionServer;
				pos.x += Random.Range(-11, 11);
				pos.y += Random.Range(-11, 11);

				if (MatrixManager.IsEmptyAt(pos, true))
				{
					Spawn.ServerPrefab(prefabToSpawn, pos, transform.parent);
					return;
				}

				overloadPrevent++;
			}
		}

		private void FireNuclearParticle()
		{

		}

		private void AddMessageToChat(string message, bool sendToCommon = false)
		{
			Chat.AddCommMsgByMachineToChat(gameObject, message, ChatChannel.Engineering, broadcasterName: "Supermatter Warning System: ");

			if (sendToCommon)
			{
				Chat.AddCommMsgByMachineToChat(gameObject, message, ChatChannel.Common, broadcasterName: "Supermatter Warning System: ");
			}
		}

		private void SendMessageToAllPlayers(string message)
		{
			foreach (var player in PlayerList.Instance.InGamePlayers)
			{
				Chat.AddExamineMsgFromServer(player.GameObject, message);
			}
		}

		private void PlayAlarmSound()
		{
			switch (GetStatus())
			{
				case SuperMatterStatus.Delaminating:
					break;
				case SuperMatterStatus.Emergency:
					break;
				case SuperMatterStatus.Danger:
					break;
				case SuperMatterStatus.Warning:
					break;
			}
		}

		private SuperMatterStatus GetStatus()
		{
			var node = registerTile.Matrix.GetMetaDataNode(registerTile.LocalPositionServer, false);

			if (node == null) return SuperMatterStatus.Error;

			var gas = node.GasMix;

			var integrity = GetIntegrityPercentage();

			if (integrity < SupermatterDelamPercent)
				return SuperMatterStatus.Delaminating;

			if (integrity < SupermatterEmergencyPercent)
				return SuperMatterStatus.Emergency;

			if (integrity < SupermatterDangerPercent)
				return SuperMatterStatus.Danger;

			if ((integrity < SupermatterWarningPercent) || (gas.Temperature > CriticalTemperature))
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


		//Called when hit by projectiles
		public void OnHitDetect(DamageData damageData)
		{
			superMatterIntegrity += damageData.Damage * 2;
		}

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

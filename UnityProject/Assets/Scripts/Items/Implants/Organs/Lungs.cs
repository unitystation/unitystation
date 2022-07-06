using System;
using System.Collections.Generic;
using Systems.Atmospherics;
using Chemistry;
using Objects.Atmospherics;
using ScriptableObjects.Atmospherics;
using UnityEngine;

namespace HealthV2
{
	public class Lungs : BodyPartFunctionality
	{
		/// <summary>
		/// The number of ticks to wait until next breath is attempted
		/// </summary>
		[Tooltip("The number of ticks to wait until next breath is attempted")]
		[SerializeField]
		private int breatheCooldown = 4;

		private int currentBreatheCooldown = 4;

		/// <summary>
		/// The minimum pressure of the required gas needed to avoid suffocation
		/// </summary>
		[Tooltip("The minimum pressure needed to avoid suffocation")]
		[SerializeField]
		private float pressureSafeMin = 16;

		[SerializeField] private List<ToxicGas> toxicGases;

		/// <summary>
		/// The gas that this tries to put into the blood stream
		/// </summary>
		[Tooltip("The gas that this tries to put into the blood stream")]
		[SerializeField]
		private GasSO requiredGas;

		/// <summary>
		/// The base amount of blood that this attempts to process each single breath
		/// </summary>
		//[Tooltip("The base amount of blood in litres that this processes each breath")]
		//public float LungProcessAmount = 1.5f;

		/// <summary>
		/// The volume of the lung in litres
		/// </summary>
		[Tooltip("The volume of the lung in litres")]
		public float LungSize = 0.3f;

		[SerializeField, Range(0, 100)] private float coughChanceWhenInternallyBleeding = 32;
		[SerializeField] private float internalBleedingCooldown = 4f;
		private bool onCooldown = false;

		public override void ImplantPeriodicUpdate()
		{
			base.ImplantPeriodicUpdate();

			// Disable breathing for dead and brain damaged players
			if (RelatedPart.HealthMaster.IsDead)
				return;

			Brain brain = RelatedPart.HealthMaster.brain;
			if (brain && brain.RelatedPart.TotalModified <= 0.2f)
				return;

			Vector3Int position = RelatedPart.HealthMaster.ObjectBehaviour.registerTile.WorldPosition;
			MetaDataNode node = MatrixManager.GetMetaDataAt(position);
			var TotalModified = 1f;
			foreach (var modifier in bodyPart.AppliedModifiers)
			{
				var toMultiply = 1f;
				if (modifier == bodyPart.DamageModifier)
				{
					toMultiply = Mathf.Max(0f,
						Mathf.Max(bodyPart.MaxHealth - bodyPart.TotalDamageWithoutOxyCloneRadStam, 0) /
						bodyPart.MaxHealth);
				}
				else if (modifier == bodyPart.HungerModifier)
				{
					continue;
				}
				else
				{
					toMultiply = Mathf.Max(0f, modifier.Multiplier);
				}

				TotalModified *= toMultiply;
			}

			if (TryBreathing(node, TotalModified))
			{
				AtmosManager.Instance.UpdateNode(node);
			}

			if (RelatedPart.IsBleedingInternally)
			{
				InternalDamageLogic();
			}
		}

		/// <summary>
		/// Performs the action of breathing, expelling waste products from the used blood pool and refreshing
		/// the desired blood reagent (ie oxygen)
		/// </summary>
		/// <param name="node">The gas node at this lung's position</param>
		/// <returns>True if gas was exchanged</returns>
		public bool TryBreathing(IGasMixContainer node, float efficiency)
		{
			//Base effeciency is a little strong on the lungs
			//efficiency = (1 + efficiency) / 2;

			//Breathing is not timebased, but tick based, it will be slow when the blood has all the oxygen it needs
			//and will speed up if more oxygen is needed
			currentBreatheCooldown--;
			if (currentBreatheCooldown > 0)
			{
				return false;
			}
			if (RelatedPart.HealthMaster.CirculatorySystem.BloodPool[RelatedPart.bloodType] == 0)
			{
				return false; //No point breathing if we dont have blood.
			}

			// Try to get internal breathing if possible, otherwise get from the surroundings
			IGasMixContainer container = RelatedPart.HealthMaster.RespiratorySystem.GetInternalGasMix();
			var gasMixSink = node.GasMix; // Where to dump lung exhaust
			if (container == null)
			{
				// Could be in a container that has an internal gas mix, else use the tile's gas mix.
				var parentContainer = RelatedPart.HealthMaster.ObjectBehaviour.ContainedInContainer;
				if (parentContainer != null && parentContainer.TryGetComponent<GasContainer>(out var gasContainer))
				{
					container = gasContainer;
					gasMixSink = container.GasMix;
				}
				else
				{
					container = node;
				}
			}

			if (efficiency > 1)
			{
				efficiency = 1;
			}

			ReagentMix AvailableBlood =
				RelatedPart.HealthMaster.CirculatorySystem.BloodPool.Take(
					(RelatedPart.HealthMaster.CirculatorySystem.BloodPool.Total * efficiency) / 2f);
			bool tryExhale = BreatheOut(gasMixSink, AvailableBlood);
			bool tryInhale = BreatheIn(container.GasMix, AvailableBlood, efficiency);
			RelatedPart.HealthMaster.CirculatorySystem.BloodPool.Add(AvailableBlood);
			return tryExhale || tryInhale;
		}

		private readonly List<Reagent> SpecialCarrier = new List<Reagent>();

		/// <summary>
		/// Expels unwanted gases from the blood stream into the given gas mix
		/// </summary>
		/// <param name="gasMix">The gas mix to breathe out into</param>
		/// <param name="blood">The blood to pull gases from</param>
		/// <returns> True if breathGasMix was changed </returns>
		private bool BreatheOut(GasMix gasMix, ReagentMix blood)
		{
			SpecialCarrier.Clear();
			var OptimalBloodGasCapacity = 0f;
			var BloodGasCapacity = 0f;

			foreach (var Reagent in blood.reagents.m_dict)
			{
				var BloodType = Reagent.Key as BloodType;
				if (BloodType != null)
				{
					OptimalBloodGasCapacity += Reagent.Value * BloodType.BloodCapacityOf;
					BloodGasCapacity += Reagent.Value * BloodType.BloodGasCapability;
					SpecialCarrier.Add(BloodType.CirculatedReagent);
					SpecialCarrier.Add(BloodType.WasteCarryReagent);
				}
			}

			// This isn't exactly realistic, should also factor concentration of gases in the gasMix
			ReagentMix toExhale = new ReagentMix();
			foreach (var reagent in blood.reagents.m_dict)
			{
				if (Gas.ReagentToGas.ContainsKey(reagent.Key) == false) continue;

				if (reagent.Value <= 0) continue;

				if (SpecialCarrier.Contains(reagent.Key))
				{
					toExhale.Add(reagent.Key, (reagent.Value / OptimalBloodGasCapacity) * LungSize);
				}
				else
				{
					toExhale.Add(reagent.Key, (reagent.Value / BloodGasCapacity) * LungSize);
				}
			}

			RelatedPart.HealthMaster.RespiratorySystem.GasExchangeFromBlood(gasMix, blood, toExhale);
			//Debug.Log("Gas exhaled: " + toExhale.Total);
			return toExhale.Total > 0;
		}

		/// <summary>
		/// Pulls in the desired gas, as well as others, from the specified gas mix and adds them to the blood stream
		/// </summary>
		/// <param name="gasMix">The gas mix to breathe in from</param>
		/// <param name="blood">The blood to put gases into</param>
		/// <returns> True if breathGasMix was changed </returns>
		private bool BreatheIn(GasMix breathGasMix, ReagentMix blood, float efficiency)
		{
			if (RelatedPart.HealthMaster.RespiratorySystem.CanBreatheAnywhere)
			{
				blood.Add(RelatedPart.requiredReagent, RelatedPart.bloodType.GetSpareGasCapacity(blood));
				return false;
			}

			ReagentMix toInhale = new ReagentMix();
			var Available = RelatedPart.bloodType.GetNormalGasCapacity(blood);

			ToxinBreathinCheck(breathGasMix);
			float PercentageCanTake = 1;

			if (breathGasMix.Moles != 0)
			{
				PercentageCanTake = LungSize / breathGasMix.Moles;
			}

			if (PercentageCanTake > 1)
			{
				PercentageCanTake = 1;
			}

			var PressureMultiplier = breathGasMix.Pressure / pressureSafeMin;
			if (PressureMultiplier > 1)
			{
				PressureMultiplier = 1;
			}

			var TotalMoles = breathGasMix.Moles * PercentageCanTake;


			lock (breathGasMix.GasData.GasesArray) //no Double lock
			{
				foreach (var gasValues in breathGasMix.GasData.GasesArray)
				{
					var gas = gasValues.GasSO;
					if (Gas.GasToReagent.TryGetValue(gas, out var gasReagent) == false) continue;

					// n = PV/RT
					float gasMoles = breathGasMix.GetMoles(gas) * PercentageCanTake;

					// Get as much as we need, or as much as in the lungs, whichever is lower
					float molesRecieved = 0;

					if (gasReagent == RelatedPart.bloodType.CirculatedReagent)
					{
						var PercentageMultiplier = (gasMoles / (TotalMoles));
						molesRecieved = RelatedPart.bloodType.GetSpecialGasCapacity(blood) * PercentageMultiplier *
										PressureMultiplier;
					}
					else if (gasMoles != 0)
					{
						molesRecieved = (Available * (gasMoles / TotalMoles)) * PressureMultiplier;
					}

					if (molesRecieved > 0)
					{
						toInhale.Add(gasReagent, molesRecieved);
					}
				}
			}

			RelatedPart.HealthMaster.RespiratorySystem.GasExchangeToBlood(breathGasMix, blood, toInhale, LungSize);


			// Counterintuitively, in humans respiration is stimulated by pressence of CO2 in the blood, not lack of oxygen
			// May want to change this code to reflect that in the future so people don't hyperventilate when they are on nitrous oxide


			if (bodyPart.currentBloodSaturation >= RelatedPart.bloodType.BLOOD_REAGENT_SATURATION_OKAY)
			{
				currentBreatheCooldown = breatheCooldown; //Slow breathing, we're all good
				RelatedPart.HealthMaster.HealthStateController.SetSuffocating(false);
			}
			else if (bodyPart.currentBloodSaturation <= RelatedPart.bloodType.BLOOD_REAGENT_SATURATION_BAD)
			{
				RelatedPart.HealthMaster.HealthStateController.SetSuffocating(true);
				if (DMMath.Prob(20))
				{
					Chat.AddActionMsgToChat(RelatedPart.HealthMaster.gameObject, "You gasp for breath!",
						$"{RelatedPart.HealthMaster.playerScript.visibleName} gasps!");
				}
			}

			//Debug.Log("Gas inhaled: " + toInhale.Total + " Saturation: " + saturation);
			return toInhale.Total > 0;
		}

		/// <summary>
		/// Checks for toxic gases and if they excede their maximum range before they become deadly
		/// </summary>
		/// <param name="gasMix">the gases the character is breathing in</param>
		public virtual void ToxinBreathinCheck(GasMix gasMix)
		{
			if (RelatedPart.HealthMaster.RespiratorySystem.CanBreatheAnywhere ||
				RelatedPart.HealthMaster.playerScript == null) return;
			foreach (ToxicGas gas in toxicGases)
			{
				float pressure = gasMix.GetPressure(gas.GasType);
				if (pressure >= gas.PressureSafeMax)
				{
					RelatedPart.HealthMaster.RespiratorySystem.ApplyDamage(gas.UnsafeLevelDamage,
						gas.UnsafeLevelDamageType);
				}
			}
		}

		public override void InternalDamageLogic()
		{
			if (!onCooldown)
			{
				if (RelatedPart.CurrentInternalBleedingDamage > RelatedPart.MaximumInternalBleedDamage / 2)
				{
					Chat.AddActionMsgToChat(RelatedPart.HealthMaster.gameObject,
						"You gasp for air; but you drown in your own blood from the inside!",
						$"{RelatedPart.HealthMaster.playerScript.visibleName} gasps for air!");
					RelatedPart.HealthMaster.HealthStateController.SetSuffocating(true);
				}
				else
				{
					RelatedPart.InternalBleedingLogic();
				}

				if (DMMath.Prob(coughChanceWhenInternallyBleeding))
				{
					Chat.AddActionMsgToChat(RelatedPart.HealthMaster.gameObject, "You cough up blood!",
						$"{RelatedPart.HealthMaster.playerScript.visibleName} coughs up blood!");
					RelatedPart.CurrentInternalBleedingDamage -= 4;

					//TODO: TAKE BLOOD
					var bloodLoss = new ReagentMix();
					RelatedPart.HealthMaster.CirculatorySystem.BloodPool.TransferTo(bloodLoss,
						RelatedPart.CurrentInternalBleedingDamage);
					MatrixManager.ReagentReact(bloodLoss,
						RelatedPart.HealthMaster.gameObject.RegisterTile().WorldPositionServer);
				}

				onCooldown = true;
				StartCoroutine(CooldownTick());
			}
		}

		private IEnumerator<WaitForSeconds> CooldownTick()
		{
			yield return new WaitForSeconds(internalBleedingCooldown);
			onCooldown = false;
		}

		[Serializable]
		class ToxicGas
		{
			public GasSO GasType = default;
			public float PressureSafeMax = 0.4f;
			public float UnsafeLevelDamage = 10;
			public DamageType UnsafeLevelDamageType = DamageType.Tox;
		}
	}
}

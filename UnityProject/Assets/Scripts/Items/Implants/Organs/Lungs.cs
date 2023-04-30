using System;
using System.Collections.Generic;
using Chemistry;
using HealthV2;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using Objects.Atmospherics;
using ScriptableObjects.Atmospherics;
using Systems.Atmospherics;
using UnityEngine;

namespace Items.Implants.Organs
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

		public ReagentCirculatedComponent ReagentCirculatedComponent;
		public SaturationComponent SaturationComponent;
		public HungerComponent HungerComponent;

		public BodyPartAlerts BodyPartAlerts;

		public bool hasToxinsCash =false;

		private bool suffocatingCash = false;

		public AlertSO ToxinAlert;
		public AlertSO SuffocatingAlert;

		public override void Awake()
		{
			base.Awake();
			ReagentCirculatedComponent = this.GetComponentCustom<ReagentCirculatedComponent>();
			SaturationComponent = this.GetComponentCustom<SaturationComponent>();
			HungerComponent = this.GetComponentCustom<HungerComponent>();
			BodyPartAlerts = this.GetComponentCustom<BodyPartAlerts>();
		}

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

			var totalModified = 1f;
			foreach (var modifier in RelatedPart.AppliedModifiers)
			{
				var toMultiply = 1f;
				if (modifier == RelatedPart.DamageModifier)
				{
					toMultiply = Mathf.Max(0f,
						Mathf.Max(RelatedPart.MaxHealth - RelatedPart.TotalDamageWithoutOxyCloneRadStam, 0) /
						RelatedPart.MaxHealth);
				}
				else if (modifier == HungerComponent.OrNull()?.HungerModifier)
				{
					continue;
				}
				else
				{
					toMultiply = Mathf.Max(0f, modifier.Multiplier);
				}

				totalModified *= toMultiply;
			}

			if (TryBreathing(node, totalModified))
			{
				AtmosManager.Instance.UpdateNode(node);
			}
		}

		/// <summary>
		/// Performs the action of breathing, expelling waste products from the used blood pool and refreshing
		/// the desired blood reagent (ie oxygen)
		/// </summary>
		/// <param name="node">The gas node at this lung's position</param>
		/// <returns>True if gas was exchanged</returns>
		public bool TryBreathing(IGasMixContainer node, float efficiency, bool OverrideCooldown =false)
		{
			//Base effeciency is a little strong on the lungs
			//efficiency = (1 + efficiency) / 2;

			//Breathing is not timebased, but tick based, it will be slow when the blood has all the oxygen it needs
			//and will speed up if more oxygen is needed
			if (OverrideCooldown == false)
			{
				currentBreatheCooldown--;
				if (currentBreatheCooldown > 0)
				{
					return false;
				}
			}

			if (ReagentCirculatedComponent.AssociatedSystem.BloodPool[SaturationComponent.bloodType] == 0)
			{
				return false; //No point breathing if we dont have blood.
			}

			bool internalGasMix = true;

			// Try to get internal breathing if possible, otherwise get from the surroundings
			IGasMixContainer container = RelatedPart.HealthMaster.RespiratorySystem.GetInternalGasMix();
			var gasMixSink = node.GasMix; // Where to dump lung exhaust
			if (container == null)
			{
				// Could be in a container that has an internal gas mix, else use the tile's gas mix.
				var parentContainer = RelatedPart.HealthMaster.ObjectBehaviour.ContainedInObjectContainer;
				if (parentContainer != null && parentContainer.TryGetComponent<GasContainer>(out var gasContainer))
				{
					container = gasContainer;
					gasMixSink = container.GasMix;
				}
				else
				{
					internalGasMix = false;
					container = node;
				}
			}

			ReagentMix availableBlood =
				ReagentCirculatedComponent.AssociatedSystem.BloodPool.Take(
					(ReagentCirculatedComponent.AssociatedSystem.BloodPool.Total * efficiency) / 2f);

			if (internalGasMix == false)
			{
				var inNode = RelatedPart.HealthMaster.RegisterTile.Matrix.MetaDataLayer.Get(RelatedPart.HealthMaster.transform.localPosition.RoundToInt());
				if (inNode != null && inNode.SmokeNode.IsActive)
				{
					availableBlood.Add(inNode.SmokeNode.Present.Clone());
				}
			}

			bool tryExhale = BreatheOut(gasMixSink, availableBlood);
			bool tryInhale = BreatheIn(container.GasMix, availableBlood, efficiency);
			ReagentCirculatedComponent.AssociatedSystem.BloodPool.Add(availableBlood);
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
			var optimalBloodGasCapacity = 0f;
			var bloodGasCapacity = 0f;

			foreach (var Reagent in blood.reagents.m_dict)
			{
				var bloodType = Reagent.Key as BloodType;
				if (bloodType != null)
				{
					optimalBloodGasCapacity += Reagent.Value * bloodType.BloodCapacityOf;
					bloodGasCapacity += Reagent.Value * bloodType.BloodGasCapability;
					SpecialCarrier.Add(bloodType.CirculatedReagent);
					SpecialCarrier.Add(bloodType.WasteCarryReagent);
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
					toExhale.Add(reagent.Key, (reagent.Value / optimalBloodGasCapacity) * LungSize);
				}
				else
				{
					toExhale.Add(reagent.Key, (reagent.Value / bloodGasCapacity) * LungSize);
				}
			}

			RelatedPart.HealthMaster.RespiratorySystem.GasExchangeFromBlood(gasMix, blood, toExhale);
			//Debug.Log("Gas exhaled: " + toExhale.Total);
			return toExhale.Total > 0;
		}

		/// <summary>
		/// Pulls in the desired gas, as well as others, from the specified gas mix and adds them to the blood stream
		/// </summary>
		/// <param name="breathGasMix">The gas mix to breathe in from</param>
		/// <param name="blood">The blood to put gases into</param>
		/// <returns> True if breathGasMix was changed </returns>
		protected virtual bool BreatheIn(GasMix breathGasMix, ReagentMix blood, float efficiency)
		{
			if (RelatedPart.HealthMaster.RespiratorySystem.CanBreatheAnywhere)
			{
				blood.Add(SaturationComponent.requiredReagent, SaturationComponent.bloodType.GetSpareGasCapacity(blood));
				return false;
			}

			ReagentMix toInhale = new ReagentMix();
			var available = SaturationComponent.bloodType.GetNormalGasCapacity(blood);

			ToxinBreathinCheck(breathGasMix);
			float percentageCanTake = 1;

			if (breathGasMix.Moles != 0)
			{
				percentageCanTake = LungSize / breathGasMix.Moles;
			}

			if (percentageCanTake > 1)
			{
				percentageCanTake = 1;
			}

			var pressureMultiplier = breathGasMix.Pressure / pressureSafeMin;
			if (pressureMultiplier > 1)
			{
				pressureMultiplier = 1;
			}

			var totalMoles = breathGasMix.Moles * percentageCanTake;


			lock (breathGasMix.GasData.GasesArray) //no Double lock
			{
				foreach (var gasValues in breathGasMix.GasData.GasesArray)
				{
					var gas = gasValues.GasSO;
					if (Gas.GasToReagent.TryGetValue(gas, out var gasReagent) == false) continue;

					// n = PV/RT
					float gasMoles = breathGasMix.GetMoles(gas) * percentageCanTake;

					// Get as much as we need, or as much as in the lungs, whichever is lower
					float molesRecieved = 0;

					if (gasReagent == SaturationComponent.bloodType.CirculatedReagent)
					{
						var percentageMultiplier = (gasMoles / (totalMoles));
						molesRecieved = SaturationComponent.bloodType.GetSpecialGasCapacity(blood) * percentageMultiplier *
						                pressureMultiplier;
					}
					else if (gasMoles != 0)
					{
						molesRecieved = (available * (gasMoles / totalMoles)) * pressureMultiplier;
					}

					if (molesRecieved > 0)
					{
						toInhale.Add(gasReagent, molesRecieved);
					}
				}
			}

			RelatedPart.HealthMaster.RespiratorySystem.GasExchangeToBlood(breathGasMix, blood, toInhale, LungSize);


			bool suffocating = false;
			// Counterintuitively, in humans respiration is stimulated by pressence of CO2 in the blood, not lack of oxygen
			// May want to change this code to reflect that in the future so people don't hyperventilate when they are on nitrous oxide


			if (SaturationComponent.CurrentBloodSaturation >= SaturationComponent.bloodType.BLOOD_REAGENT_SATURATION_OKAY)
			{
				currentBreatheCooldown = breatheCooldown; //Slow breathing, we're all good
				suffocating = false;
			}
			else if (SaturationComponent.CurrentBloodSaturation <= SaturationComponent.bloodType.BLOOD_REAGENT_SATURATION_BAD)
			{
				suffocating = true;
				if (DMMath.Prob(20))
				{
					Chat.AddActionMsgToChat(RelatedPart.HealthMaster.gameObject, "You gasp for breath!",
						$"{RelatedPart.HealthMaster.playerScript.visibleName} gasps!");
				}
			}

			if (suffocatingCash != suffocating)
			{
				suffocatingCash = suffocating;
				if (suffocatingCash)
				{
					BodyPartAlerts.AddAlert(SuffocatingAlert);
				}
				else
				{
					BodyPartAlerts.RemoveAlert(SuffocatingAlert);
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

			var hasToxins = false;

			foreach (ToxicGas gas in toxicGases)
			{
				float pressure = gasMix.GetPressure(gas.GasType);
				if (pressure >= gas.PressureSafeMax)
				{
					RelatedPart.HealthMaster.RespiratorySystem.ApplyDamage(gas.UnsafeLevelDamage,
						gas.UnsafeLevelDamageType);

					hasToxins = true;
				}
			}

			if (hasToxinsCash != hasToxins)
			{
				hasToxinsCash = hasToxins;
				if (hasToxins)
				{
					BodyPartAlerts.AddAlert(ToxinAlert);
				}
				else
				{
					BodyPartAlerts.RemoveAlert(ToxinAlert);
				}
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

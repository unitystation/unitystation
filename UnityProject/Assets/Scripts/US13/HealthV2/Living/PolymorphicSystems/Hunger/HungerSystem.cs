using System;
using System.Collections.Generic;
using Chemistry;
using Logs;
using UnityEngine;
using US13.Core.Attributes;
using US13.HealthV2.Living.BodyParts;
using US13.HealthV2.Living.Metabolism;
using US13.HealthV2.Living.PolymorphicSystems.Bodypart;
using US13.ScriptableObjects;
using US13.Systems.StatusesAndEffects;
using US13.UI.Core.Alerts;

namespace US13.HealthV2.Living.PolymorphicSystems.Hunger
{
    /// <summary>
    /// HungerSystem manages the hunger and nutrition mechanics for a living creature.
    /// It tracks nutriment consumption across all registered body parts, updates hunger
    /// state based on blood nutriment availability, and synchronises UI alerts and
    /// status effects to reflect the creature's current hunger level.
    /// </summary>
    public class HungerSystem : HealthSystemBase
    {
        /// <summary>
        /// Maps each required reagent (nutriment type) to the body parts that consume it
        /// and the total amount needed per update tick.
        /// </summary>
        public Dictionary<Reagent, ReagentWithBodyParts> NutrimentToConsume = new();

        /// <summary>
        /// All HungerComponents (body parts that participate in hunger) registered with this system.
        /// </summary>
        public List<HungerComponent> BodyParts = new List<HungerComponent>();

        // Handles displaying hunger-related HUD alerts to the player.
        private BodyAlertManager bodyAlertManager;

        // Handles applying and removing status effects tied to hunger states.
        private StatusEffectManager statusEffectManager;

        /// <summary>
        /// How many minutes of normal activity the creature can sustain before it starts starving.
        /// Used during initialisation to scale stored body-fat amounts accordingly.
        /// </summary>
        public float NumberOfMinutesBeforeStarving = 30;

        /// <summary>
        /// The default nutriment reagent used by body parts that have not been assigned
        /// a specific nutriment type.
        /// </summary>
        [Tooltip("What does this live off?, Sets all the body parts that don't have a set nutriment")]
        public Reagent BodyNutriment;

        /// <summary>
        /// Lazy-loaded reference to the creature's ReagentPoolSystem (blood pool),
        /// which stores all circulating reagents including nutriments.
        /// </summary>
        private ReagentPoolSystem reagentPoolSystem
        {
            get
            {
                if (_reagentPoolSystem == null)
                {
                    _reagentPoolSystem = Base.reagentPoolSystem;
                }
                return _reagentPoolSystem;
            }
        }
        private ReagentPoolSystem _reagentPoolSystem;

        /// <summary>
        /// The last hunger state that was acted upon (alerts/effects updated).
        /// Used to detect when the state has changed since the previous update.
        /// </summary>
        public HungerState CashedHungerState = HungerState.Normal;

        /// <summary>
        /// Holds references to the StatusEffect assets that correspond to each hunger state.
        /// </summary>
        public HungerStatuesEffects HungerStatusEffects = new();

        [SerializeReference, SelectImplementation(typeof(IHungerCalculation))]
        public IHungerCalculation HungerCalculationMethod = null;

        /// <summary>
        /// Returns the AlertSO (HUD alert asset) that corresponds to the given hunger state.
        /// Returns null for states that have no associated alert (e.g. Normal).
        /// </summary>
        public AlertSO GetAlertSOFromHunger(HungerState HungerStates)
        {
            switch (HungerStates)
            {
                case HungerState.Full:        return CommonAlertSOs.Instance.Full;
                case HungerState.Starving:    return CommonAlertSOs.Instance.Starving;
                case HungerState.Malnourished:return CommonAlertSOs.Instance.Malnourished;
                case HungerState.Hungry:      return CommonAlertSOs.Instance.Hungry;
                default:                      return null;
            }
        }

        /// <summary>
        /// Returns the StatusEffect asset that corresponds to the given hunger state.
        /// Malnourished and unrecognised states fall back to the NotHungry effect.
        /// </summary>
        public StatusEffect GetStatusEffectFromHunger(HungerState hungerState)
        {
            switch (hungerState)
            {
                case HungerState.Full:        return HungerStatusEffects.FatStatusEffect;
                case HungerState.Hungry:      return HungerStatusEffects.HungryStatusEffect;
                case HungerState.Starving:    return HungerStatusEffects.StravingStatusEffect;
                case HungerState.Malnourished:
                default:                      return HungerStatusEffects.NotHungryStatusEffect;
            }
        }

        /// <summary>
        /// Called once when the system is initialised.
        /// Grabs references to the BodyAlertManager and StatusEffectManager on the same GameObject.
        /// </summary>
        public override void InIt()
        {
            base.InIt();
            bodyAlertManager   = Base.GetComponent<BodyAlertManager>();
            statusEffectManager = Base.GetComponent<StatusEffectManager>();
        }

        /// <summary>
        /// Called whenever a new body part is added to the creature.
        /// If the body part has a HungerComponent and is enabled, it is registered
        /// with this system and the nutriment consumption map is rebuilt.
        /// </summary>
        public override void BodyPartAdded(BodyPart bodyPart)
        {
            var component = bodyPart.GetComponent<HungerComponent>();
            if (component != null)
            {
                if (component.enabled == false) return;
                if (BodyParts.Contains(component) == false)
                {
                    BodyParts.Add(component);
                    BodyPartListChange(); // Rebuild the reagent consumption map.
                }
            }
        }

        /// <summary>
        /// Called when the system should start from a clean state (e.g. creature spawn).
        /// Assigns the default nutriment to any body part that doesn't have one set,
        /// then initialises the hunger/body-fat amounts for the configured starvation window.
        /// </summary>
        public override void StartFresh()
        {
            foreach (var bodyPart in BodyParts)
            {
                if (bodyPart.Nutriment == null)
                {
                    bodyPart.Nutriment = BodyNutriment;
                }
            }

            InitialiseHunger(NumberOfMinutesBeforeStarving);
        }

        /// <summary>
        /// Scales the stored body-fat amounts across all stomachs so that the creature
        /// will begin starving after approximately <paramref name="numberOfMinutesBeforeHunger"/> minutes.
        /// Also distributes per-body-part passive nutriment consumption rates proportionally
        /// to each part's blood throughput.
        ///
        /// Steps:
        ///  1. Sum total blood throughput across all body parts.
        ///  2. Derive a per-flow-unit consumption rate so 1 unit is depleted per minute.
        ///  3. Assign that rate to every body part scaled by its individual throughput.
        ///  4. Sum currently stored body fat across all stomachs.
        ///  5. Calculate the multiplier needed to make that fat last the desired number of minutes.
        ///  6. Apply a ±25% random variation to avoid all creatures starving at the same time.
        ///  7. Scale each stomach's body-fat stores by the multiplier.
        /// </summary>
        public void InitialiseHunger(float numberOfMinutesBeforeHunger)
        {
            // Step 1: Total blood throughput across all hunger-participating body parts.
            var TotalBloodThroughput = 0f;
            foreach (var bodyPart in BodyParts)
            {
                TotalBloodThroughput += bodyPart.BloodThroughput;
            }

            // Step 2: Consumption per flow unit per second so that the total across all
            // body parts equals 1 unit of nutriment consumed per minute.
            var ConsumptionPerFlowSecond = (1f / 60f) / TotalBloodThroughput;

            // Step 3: Assign the base consumption rate to each body part.
            foreach (var bodyPart in BodyParts)
            {
                bodyPart.PassiveConsumptionNutriment = ConsumptionPerFlowSecond;
            }

            // Step 4: Sum currently stored body fat across all stomachs.
            var Stomachs = Base.GetStomachs();
            var MinutesAvailable = 0f;
            foreach (var Stomach in Stomachs)
            {
                foreach (var bodyFat in Stomach.BodyFats)
                {
                    MinutesAvailable += bodyFat.AbsorbedAmount;
                }
            }

            // Step 5: Multiplier to stretch stored fat to last the desired number of minutes.
            var Bymultiply = numberOfMinutesBeforeHunger / MinutesAvailable;

            // Step 6: Apply ±25% random variance so starvation timing differs between creatures.
            Bymultiply *= (1 + UnityEngine.Random.Range(-0.25f, 0.25f));

            // Step 7: Scale each body-fat store by the multiplier.
            foreach (var Stomach in Stomachs)
            {
                foreach (var bodyFat in Stomach.BodyFats)
                {
                    bodyFat.AbsorbedAmount *= Bymultiply;
                }
            }

            BodyPartListChange();
            UpdateStatusEffects(HungerState.Starving, HungerState.Normal);
        }

        /// <summary>
        /// Called whenever a body part is removed from the creature.
        /// Deregisters its HungerComponent and rebuilds the nutriment consumption map.
        /// </summary>
        public override void BodyPartRemoved(BodyPart bodyPart)
        {
            var component = bodyPart.GetComponent<HungerComponent>();
            if (component != null)
            {
                if (BodyParts.Contains(component))
                {
                    BodyParts.Remove(component);
                }
                BodyPartListChange();
            }
        }

        /// <summary>
        /// Rebuilds the NutrimentToConsume dictionary from scratch whenever the list of
        /// body parts changes (add, remove, or initialise).
        /// Groups body parts by their required reagent and accumulates the total nutriment
        /// demand for each reagent type (consumption rate × blood throughput).
        /// </summary>
        public void BodyPartListChange()
        {
            NutrimentToConsume.Clear();

            foreach (var bodyPart in BodyParts)
            {
                // Ensure there's an entry for this body part's nutriment type.
                if (NutrimentToConsume.ContainsKey(bodyPart.Nutriment) == false)
                {
                    NutrimentToConsume[bodyPart.Nutriment] = new ReagentWithBodyParts();
                }

                NutrimentToConsume[bodyPart.Nutriment].RelatedBodyParts.Add(bodyPart);

                // Accumulate total nutriment demand: passive rate × blood throughput.
                NutrimentToConsume[bodyPart.Nutriment].TotalNeeded +=
                    bodyPart.PassiveConsumptionNutriment * bodyPart.reagentCirculatedComponent.Throughput;
            }
        }

        /// <summary>
        /// Main per-tick update. Called via a periodic update on the UpdateManager. Check HealthV2 for update rates.
        /// Steps:
        /// 1. Calculates the current hunger state.
        /// 2. Sums heart efficiency across all pumping devices.
        /// 3. Runs nutriment consumption and healing calculations.
        /// 4. If the hunger state has changed since last tick, updates HUD alerts
        ///    and status effects accordingly.
        /// </summary>
        public override void SystemUpdate()
        {
            var state = HungerState.Normal;

            if (HungerCalculationMethod != null)
            {
	            state = HungerCalculationMethod.CalculateHungerState(Base, this);
            }

            // Detect a hunger state change and update UI/effects only when needed.
            if (state != CashedHungerState)
            {
                try
                {
                    UpdateStatusEffects(CashedHungerState, state);
                }
                catch (Exception e)
                {
                    Loggy.Error($"An issue happened while updating hunger state changes: {e}");
                }
                CashedHungerState = state;
            }
        }

        /// <summary>
        /// Swaps the active status effect from the old hunger state to the new one.
        /// Removes the outgoing effect and applies the incoming one.
        /// </summary>
        private void UpdateStatusEffects(HungerState oldState, HungerState newState)
        {
            var oldStatusEffect = GetStatusEffectFromHunger(oldState);
            if (oldStatusEffect != null)
            {
                statusEffectManager?.RemoveStatus(oldStatusEffect);
            }

            var newStatusEffect = GetStatusEffectFromHunger(newState);
            if (newStatusEffect != null)
            {
                statusEffectManager?.AddStatus(newStatusEffect);
            }
        }

        /// <summary>
        /// Debug/editor button. Instantly empties the blood pool of all nutriments
        /// and zeroes all stomach body-fat stores, putting the creature into a
        /// Starving state immediately.
        /// </summary>
        [NaughtyAttributes.Button()]
        public void MakeStarving()
        {
            foreach (var KVP in NutrimentToConsume)
            {
                reagentPoolSystem.BloodPool.Remove(KVP.Key, 9999);
            }

            var Stomachs = Base.GetStomachs();
            foreach (var Stomach in Stomachs)
            {
                foreach (var bodyFat in Stomach.BodyFats)
                {
                    bodyFat.AbsorbedAmount = 0;
                }
            }
        }

        /// <summary>
        /// Debug/editor button. Sets all stomach body-fat stores to 4 units,
        /// simulating a creature that is hungry but not yet starving.
        /// </summary>
        [NaughtyAttributes.Button()]
        public void MakeHungary()
        {
            var Stomachs = Base.GetStomachs();
            foreach (var Stomach in Stomachs)
            {
                foreach (var bodyFat in Stomach.BodyFats)
                {
                    bodyFat.AbsorbedAmount = 4;
                }
            }
        }

        /// <summary>
        /// Debug/editor button. Fills all stomach body-fat stores to their maximum capacity,
        /// simulating a creature that has just eaten a full meal.
        /// </summary>
        [NaughtyAttributes.Button()]
        public void MakeFull()
        {
            var Stomachs = Base.GetStomachs();
            foreach (var Stomach in Stomachs)
            {
                foreach (var bodyFat in Stomach.BodyFats)
                {
                    bodyFat.AbsorbedAmount = bodyFat.MinuteStoreMaxAmount;
                }
            }
        }

        /// <summary>
        /// Creates and returns a copy of this system with its configuration values preserved
        /// (status effects, starvation window, default nutriment). Body part lists are not copied
        /// since they will be rebuilt when the new system is initialised.
        /// </summary>
        public override HealthSystemBase CloneThisSystem()
        {
            return new HungerSystem
            {
                HungerStatusEffects           = HungerStatusEffects,
                NumberOfMinutesBeforeStarving = NumberOfMinutesBeforeStarving,
                BodyNutriment                 = BodyNutriment,
                HungerCalculationMethod = HungerCalculationMethod,
            };
        }

        /// <summary>
        /// Groups body parts that share the same required reagent together,
        /// tracking the total nutriment demand for that reagent and any
        /// alternative reagents that could substitute for it.
        /// </summary>
        public class ReagentWithBodyParts
        {
            /// <summary>Fraction of demand currently satisfied (0–1). Not yet used in calculations.</summary>
            public float Percentage;

            /// <summary>Total amount of this reagent required per update tick across all related body parts.</summary>
            public float TotalNeeded;

            /// <summary>All body parts that consume this particular reagent.</summary>
            public List<HungerComponent> RelatedBodyParts = new List<HungerComponent>();

            /// <summary>
            /// Maps alternative/substitute reagents to their own groupings.
            /// Intended to support reagent substitution, but not yet implemented.
            /// </summary>
            public Dictionary<Reagent, ReagentWithBodyParts> ReplacesWith = new Dictionary<Reagent, ReagentWithBodyParts>();
        }

        /// <summary>
        /// Container for the StatusEffect assets that are applied at each hunger level.
        /// Serialised so values can be assigned in the Unity Inspector.
        /// </summary>
        [Serializable]
        public class HungerStatuesEffects
        {
            /// <summary>Applied when the creature is satisfied (not hungry).</summary>
            public StatusEffect NotHungryStatusEffect;

            /// <summary>Applied when the creature is starving.</summary>
            public StatusEffect StravingStatusEffect;

            /// <summary>Applied when the creature is hungry but not yet starving.</summary>
            public StatusEffect HungryStatusEffect;

            /// <summary>Applied when the creature is overfull / fat.</summary>
            public StatusEffect FatStatusEffect;
        }
    }
}
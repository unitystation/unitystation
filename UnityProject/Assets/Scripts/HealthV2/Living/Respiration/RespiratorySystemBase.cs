using UnityEngine;
using Chemistry;
using Systems.Atmospherics;
using Objects.Atmospherics;
using System.Collections.Generic;
using Items.Implants.Organs;

namespace HealthV2
{
	[RequireComponent(typeof(LivingHealthMasterBase))]
	[RequireComponent(typeof(CirculatorySystemBase))]
	public class RespiratorySystemBase : MonoBehaviour //Not really a Respiratory More like atmospheric system idk TODO give better name
	{
		private LivingHealthMasterBase healthMaster;
		private PlayerScript playerScript;
		private UniversalObjectPhysics objectBehaviour;
		private HealthStateController healthStateController;

		[Tooltip("If this is turned on, the organism can breathe anywhere and wont affect atmospherics.")]
		[SerializeField]
		private bool canBreathAnywhere = false;

		public bool CanBreatheAnywhere => canBreathAnywhere;

		[Tooltip("How often the respiration system should update.")] [SerializeField]
		private float tickRate = 1f;

		public bool IsSuffocating => healthStateController.IsSuffocating;

		public List<BreathingTubeImplant> CurrentBreathingTubes { get; private set; } = new List<BreathingTubeImplant>();

		private void Awake()
		{
			healthMaster = GetComponent<LivingHealthMasterBase>();
			playerScript = GetComponent<PlayerScript>();
			objectBehaviour = GetComponent<UniversalObjectPhysics>();
			healthStateController = GetComponent<HealthStateController>();
		}

		private void OnEnable()
		{
			if (CustomNetworkManager.IsServer == false) return;

			UpdateManager.Add(UpdateMe, tickRate);
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.IsServer == false) return;

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		//Handle by UpdateManager
		//Server Side Only
		private void UpdateMe()
		{
			if (MatrixManager.IsInitialized && !canBreathAnywhere)
			{
				MonitorSystem();
			}
		}

		private void MonitorSystem()
		{
			if (healthMaster.IsDead) return;

		}

		/// <summary>
		/// Takes reagents from blood and puts them into a GasMix as gases
		/// </summary>
		public void GasExchangeFromBlood(GasMix atmos, ReagentMix blood, ReagentMix toProcess)
		{
			foreach (var reagent in toProcess.reagents.m_dict)
			{
				blood.Remove(reagent.Key, float.MaxValue);

				if (canBreathAnywhere || Gas.ReagentToGas.TryGetValue(reagent.Key, out var gas) == false) continue;

				//For now block breathing out water vapour as it will just fill a room
				if(gas == Gas.WaterVapor) continue;

				atmos.AddGas(gas, reagent.Value);
			}
		}

		/// <summary>
		/// Takes gases from a GasMix and puts them into blood as a reagent
		/// </summary>
		public void GasExchangeToBlood(GasMix atmos, ReagentMix blood, ReagentMix toProcess, float LungCapacity)
		{
			lock (toProcess.reagents)
			{
				foreach (var Reagent in toProcess.reagents.m_dict)
				{
					blood.Add(Reagent.Key, Reagent.Value);
				}
			}


			if (!canBreathAnywhere)
			{
				if (LungCapacity + 0.1f > atmos.Moles) //Just so scenario where there isn't Gas * 0 = 0
				{
					atmos.MultiplyGas(0.01f);
				}
				else
				{
					if (atmos.Moles == 0)
					{
						return;
					}

					var percentageRemaining =  1 - (LungCapacity / atmos.Moles);
					atmos.MultiplyGas(percentageRemaining);
				}
			}
		}

		public GasContainer GetInternalGasMix()
		{
			if (playerScript == null || playerScript.Equipment == null) return null;

			foreach(BreathingTubeImplant implant in CurrentBreathingTubes) //If emped, player will breath no air
			{
				if(implant.isEMPed)
				{
					GasContainer gasContainer = new GasContainer();
					gasContainer.GasMix = new GasMix();
					return gasContainer;
				}
			}

			// Check if internals exist
			var hasMask = false;

			if (CurrentBreathingTubes.Count > 0) hasMask = true;
			else
			{
				foreach (var itemSlot in playerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.mask))
				{
					if (itemSlot.Item == null) continue;
					if (itemSlot.ItemAttributes.CanConnectToTank)
					{
						hasMask = true;
						break;
					}
				}
			}

			var internalsEnabled = playerScript.Equipment.IsInternalsEnabled; //TPODPPPP
			if (hasMask && internalsEnabled)
			{
				foreach (var gasSlot in playerScript.DynamicItemStorage.GetGasSlots())
				{
					if (gasSlot.Item == null) continue;
					var gasContainer = gasSlot.Item.GetComponent<GasContainer>();
					if (gasContainer)
					{
						return gasContainer;
					}
				}
			}

			return null;
		}

		public bool IsEVACompatible() //Only used for splash protection now
		{
			if (playerScript == null)
			{
				return false;
			}


			foreach (var itemSlot in playerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.head))
			{
				if (itemSlot.Item == null) return false;
				if (itemSlot.ItemAttributes.IsEVACapable == false) return false;
			}

			foreach (var itemSlot in playerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.outerwear))
			{
				if (itemSlot.Item == null) return false;
				if (itemSlot.ItemAttributes.IsEVACapable == false) return false;
			}

			return true;
		}

		public void ApplyDamage(float amount, DamageType damageType)
		{
			//TODO: Figure out what kind of damage low pressure should be doing.
			healthMaster.ApplyDamageAll(null, amount, AttackType.Internal, damageType);
		}
	}
}

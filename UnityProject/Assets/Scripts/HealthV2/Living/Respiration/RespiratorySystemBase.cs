using Systems.Atmospherics;
using Chemistry;
using Items;
using NaughtyAttributes;
using Objects.Atmospherics;
using ScriptableObjects.Atmospherics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HealthV2
{
	[RequireComponent(typeof(LivingHealthMasterBase))]
	[RequireComponent(typeof(CirculatorySystemBase))]
	public class RespiratorySystemBase : MonoBehaviour
	{
		private LivingHealthMasterBase healthMaster;
		private PlayerScript playerScript;
		private ObjectBehaviour objectBehaviour;
		private HealthStateController healthStateController;
		private CirculatorySystemBase circulatorySystem;

		[Tooltip("If this is turned on, the organism can breathe anywhere and wont affect atmospherics.")]
		[SerializeField]
		private bool canBreathAnywhere = false;

		public bool CanBreathAnywhere => canBreathAnywhere;

		[Tooltip("How often the respiration system should update.")] [SerializeField]
		private float tickRate = 1f;

		public bool IsSuffocating => healthStateController.IsSuffocating;
		public float temperature => healthStateController.Temperature;
		public float pressure => healthStateController.Pressure;


		private void Awake()
		{
			circulatorySystem = GetComponent<CirculatorySystemBase>();
			healthMaster = GetComponent<LivingHealthMasterBase>();
			playerScript = GetComponent<PlayerScript>();
			objectBehaviour = GetComponent<ObjectBehaviour>();
			healthStateController = GetComponent<HealthStateController>();
		}

		void OnEnable()
		{
			if (CustomNetworkManager.IsServer == false) return;

			UpdateManager.Add(UpdateMe, tickRate);
		}

		void OnDisable()
		{
			if (CustomNetworkManager.IsServer == false) return;

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		//Handle by UpdateManager
		//Server Side Only
		void UpdateMe()
		{
			if (MatrixManager.IsInitialized && !canBreathAnywhere)
			{
				MonitorSystem();
			}
		}

		private void MonitorSystem()
		{
			if (!healthMaster.IsDead)
			{
				Vector3Int position = objectBehaviour.AssumedWorldPositionServer();
				MetaDataNode node = MatrixManager.GetMetaDataAt(position);

				if (!IsEVACompatible())
				{
					healthStateController.SetTemperature(node.GasMix.Temperature);
					healthStateController.SetPressure(node.GasMix.Pressure);
					CheckPressureDamage();
				}
				else
				{
					healthStateController.SetPressure(101.325f);
					healthStateController.SetTemperature(293.15f);
				}

				// if(healthMaster.OverallHealth >= HealthThreshold.SoftCrit)
				// {
				// 	if (Breathe(node))
				// 	{
				// 		AtmosManager.Update(node);
				// 	}
				// }
			}
		}

		/// <summary>
		/// Takes reagents from blood and puts them into a GasMix as gases
		/// </summary>
		public void GasExchangeFromBlood(GasMix atmos, ReagentMix blood, ReagentMix toProcess)
		{
			foreach (var Reagent in toProcess.reagents.m_dict)
			{
				blood.Remove(Reagent.Key, Reagent.Value);
				if (!canBreathAnywhere)
					atmos.AddGas(GAS2ReagentSingleton.Instance.GetReagentToGas(Reagent.Key), Reagent.Value);
			}
		}

		/// <summary>
		/// Takes gases from a GasMix and puts them into blood as a reagent
		/// </summary>
		public void GasExchangeToBlood(GasMix atmos, ReagentMix blood, ReagentMix toProcess)
		{
			lock (toProcess.reagents)
			{
				foreach (var Reagent in toProcess.reagents.m_dict)
				{
					blood.Add(Reagent.Key, Reagent.Value);
					if (!canBreathAnywhere)
						atmos.RemoveGas(GAS2ReagentSingleton.Instance.GetReagentToGas(Reagent.Key), Reagent.Value );
				}
			}
		}

		public GasContainer GetInternalGasMix()
		{
			if (playerScript != null)
			{
				// Check if internals exist
				bool HasMask = false;

				foreach (var itemSlot in playerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.mask))
				{
					if (itemSlot.Item == null) continue;
					if (itemSlot.ItemAttributes.CanConnectToTank)
					{
						HasMask = true;
						break;
					}
				}

				bool internalsEnabled = playerScript.Equipment.IsInternalsEnabled; //TPODPPPP
				if (HasMask && internalsEnabled)
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
			}

			return null;
		}

		private void CheckPressureDamage()
		{
			if (pressure < AtmosConstants.MINIMUM_OXYGEN_PRESSURE)
			{
				ApplyDamage(AtmosConstants.LOW_PRESSURE_DAMAGE, DamageType.Brute);
			}
			else if (pressure > AtmosConstants.HAZARD_HIGH_PRESSURE)
			{
				float damage = Mathf.Min(
					((pressure / AtmosConstants.HAZARD_HIGH_PRESSURE) - 1) * AtmosConstants.PRESSURE_DAMAGE_COEFFICIENT,
					AtmosConstants.MAX_HIGH_PRESSURE_DAMAGE);

				ApplyDamage(damage, DamageType.Brute);
			}
		}

		private bool IsEVACompatible()
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
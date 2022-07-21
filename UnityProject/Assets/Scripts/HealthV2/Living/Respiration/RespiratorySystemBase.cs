using UnityEngine;
using Chemistry;
using Systems.Atmospherics;
using Objects.Atmospherics;
using ScriptableObjects.Atmospherics;

namespace HealthV2
{
	[RequireComponent(typeof(LivingHealthMasterBase))]
	[RequireComponent(typeof(CirculatorySystemBase))]
	public class RespiratorySystemBase : MonoBehaviour
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
		public float Temperature => healthStateController.Temperature;
		public float Pressure => healthStateController.Pressure;


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

			if (IsEVACompatible())
			{
				healthStateController.SetPressure(AtmosConstants.ONE_ATMOSPHERE);
				healthStateController.SetTemperature(293.15f);
				return;
			}

			GasMix ambientGasMix;
			if (objectBehaviour.ContainedInContainer != null &&
					objectBehaviour.ContainedInContainer.TryGetComponent<GasContainer>(out var gasContainer))
			{
				ambientGasMix = gasContainer.GasMix;
			}
			else
			{
				var matrix = healthMaster.RegisterTile.Matrix;
				Vector3Int localPosition = MatrixManager.WorldToLocalInt(objectBehaviour.registerTile.WorldPosition, matrix);
				ambientGasMix = matrix.MetaDataLayer.Get(localPosition).GasMix;
			}

			healthStateController.SetTemperature(ambientGasMix.Temperature);
			healthStateController.SetPressure(ambientGasMix.Pressure);
			CheckPressureDamage();
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
			if (Pressure < AtmosConstants.MINIMUM_OXYGEN_PRESSURE)
			{
				ApplyDamage(AtmosConstants.LOW_PRESSURE_DAMAGE, DamageType.Brute);
			}
			else if (Pressure > AtmosConstants.HAZARD_HIGH_PRESSURE)
			{
				float damage = Mathf.Min(
					((Pressure / AtmosConstants.HAZARD_HIGH_PRESSURE) - 1) * AtmosConstants.PRESSURE_DAMAGE_COEFFICIENT,
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

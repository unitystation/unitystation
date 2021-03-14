using Systems.Atmospherics;
using Chemistry;
using Items;
using NaughtyAttributes;
using Objects.Atmospherics;
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
		[SerializeField] private bool canBreathAnywhere = false;
		[SerializeField] private float tickRate = 1f;
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
			UpdateManager.Add(UpdateMe, tickRate);
		}

		void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		//Handle by UpdateManager
		void UpdateMe()
		{
			//Server Only:
			if (CustomNetworkManager.IsServer && MatrixManager.IsInitialized
											  && !canBreathAnywhere)
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

		public void GasExchange(GasMix atmos, ReagentMix blood, ReagentMix toProcess, bool takeFromBlood = false)
		{
			if (takeFromBlood)
			{
				foreach (var Reagent in toProcess)
				{
					blood.Remove(Reagent.Key, Reagent.Value);
					if(!canBreathAnywhere)
						atmos.AddGas(GAS2ReagentSingleton.Instance.GetReagentToGas(Reagent.Key), Reagent.Value / 10000f);
				}
			}
			else
			{
				foreach (var Reagent in toProcess)
				{
					blood.Add(Reagent.Key, Reagent.Value);
					if(!canBreathAnywhere)
						atmos.RemoveGas(GAS2ReagentSingleton.Instance.GetReagentToGas(Reagent.Key), Reagent.Value / 10000f);
				}
			}
		}

		public GasContainer GetInternalGasMix()
		{
			if (playerScript != null)
			{
				// Check if internals exist
				var maskItemAttrs = playerScript.ItemStorage.GetNamedItemSlot(NamedSlot.mask).ItemAttributes;
				bool internalsEnabled = playerScript.Equipment.IsInternalsEnabled;
				if (maskItemAttrs != null && maskItemAttrs.CanConnectToTank && internalsEnabled)
				{
					foreach (var gasSlot in playerScript.ItemStorage.GetGasSlots())
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

		public float HandleBreathing(GasMix breathGasMix, Gas requiredGas, float reagentSafeMin)
		{
			if(canBreathAnywhere)
			{
				return float.MaxValue;
			}
			float gasPressure = breathGasMix.GetPressure(requiredGas);

			float gasInhaled = 0;

			if (gasPressure < reagentSafeMin)
			{
				if (Random.value < 0.1)
				{
					Chat.AddActionMsgToChat(gameObject, "You gasp for breath", $"{healthMaster.gameObject.ExpensiveName()} gasps");
				}

				if (gasPressure > 0)
				{
					float ratio = 1 - gasPressure / reagentSafeMin;
					gasInhaled = breathGasMix.GetMoles(requiredGas) * ratio * AtmosConstants.BREATH_VOLUME;
				}

				healthStateController.SetSuffocating(true);
			}
			else
			{
				gasInhaled = breathGasMix.GetMoles(requiredGas);
				healthStateController.SetSuffocating(false);
			}
			return gasInhaled;
		}

		private void CheckPressureDamage()
		{
			if (pressure < AtmosConstants.MINIMUM_OXYGEN_PRESSURE)
			{
				ApplyDamage(AtmosConstants.LOW_PRESSURE_DAMAGE, DamageType.Brute);
			}
			else if (pressure > AtmosConstants.HAZARD_HIGH_PRESSURE)
			{
				float damage = Mathf.Min(((pressure / AtmosConstants.HAZARD_HIGH_PRESSURE) - 1) * AtmosConstants.PRESSURE_DAMAGE_COEFFICIENT,
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

			ItemAttributesV2 headItem = playerScript.ItemStorage.GetNamedItemSlot(NamedSlot.head).ItemAttributes;
			ItemAttributesV2 suitItem = playerScript.ItemStorage.GetNamedItemSlot(NamedSlot.outerwear).ItemAttributes;

			if (headItem != null && suitItem != null)
			{
				return headItem.IsEVACapable && suitItem.IsEVACapable;
			}

			return false;
		}

		private void ApplyDamage(float amount, DamageType damageType)
		{
			//TODO: Figure out what kind of damage low pressure should be doing.
			healthMaster.ApplyDamageAll(null, amount, AttackType.Internal, damageType);
		}
	}

}
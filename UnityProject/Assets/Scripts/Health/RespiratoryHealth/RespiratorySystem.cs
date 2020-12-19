﻿using System.Collections.Generic;
using UnityEngine;
using Systems.Atmospherics;
using Items;
using Objects.Atmospherics;

/// <inheritdoc />
/// <summary>
/// Controls the RepiratorySystem for this living thing
/// Mostly managed server side and states sent to the clients
/// </summary>
public class RespiratorySystem : MonoBehaviour //Do not turn into NetBehaviour
{
	private const float OXYGEN_SAFE_MIN = 16;
	public bool IsSuffocating;
	public float temperature = 293.15f;
	public float pressure = 101.325f;

	/// <summary>
	/// 2 minutes of suffocation = 100% damage
	/// </summary>
	public int SuffocationDamage => Mathf.RoundToInt((suffocationTime / 120f) * 100f);

	public float suffocationTime = 0f;

	private BloodSystem bloodSystem;
	private LivingHealthBehaviour livingHealthBehaviour;
	private Equipment equipment;
	private ObjectBehaviour objectBehaviour;

	private float tickRate = 1f;
	private PlayerScript playerScript; //can be null since mobs also use this!
	private RegisterTile registerTile;
	private float breatheCooldown = 0;
	public bool CanBreatheAnywhere { get; set; }


	void Awake()
	{
		bloodSystem = GetComponent<BloodSystem>();
		livingHealthBehaviour = GetComponent<LivingHealthBehaviour>();
		playerScript = GetComponent<PlayerScript>();
		registerTile = GetComponent<RegisterTile>();
		equipment = GetComponent<Equipment>();
		objectBehaviour = GetComponent<ObjectBehaviour>();
	}

	void OnEnable()
	{
		if (CustomNetworkManager.IsServer)
		{
			UpdateManager.Add(ServerPeriodicUpdate, tickRate);
		}
	}

	void OnDisable()
	{
		if (CustomNetworkManager.IsServer)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, ServerPeriodicUpdate);
		}
	}

	//Handle by UpdateManager
	void ServerPeriodicUpdate()
	{
		if (MatrixManager.IsInitialized && !CanBreatheAnywhere)
		{
			MonitorSystem();
		}
	}

	private void MonitorSystem()
	{
		if (livingHealthBehaviour.IsDead) return;

		Vector3Int position = objectBehaviour.AssumedWorldPositionServer();
		MetaDataNode node = MatrixManager.GetMetaDataAt(position);

		if (!IsEVACompatible())
		{
			temperature = node.GasMix.Temperature;
			pressure = node.GasMix.Pressure;
			CheckPressureDamage();
		}
		else
		{
			pressure = 101.325f;
			temperature = 293.15f;
		}

		if (livingHealthBehaviour.OverallHealth >= HealthThreshold.SoftCrit)
		{
			if (Breathe(node))
			{
				AtmosManager.Update(node);
			}
		}
		else
		{
			bloodSystem.OxygenDamage += 1;
		}
	}

	private bool Breathe(IGasMixContainer node)
	{
		breatheCooldown --; //not timebased, but tickbased
		if(breatheCooldown > 0){
			return false;
		}
		// if no internal breathing is possible, get the from the surroundings
		IGasMixContainer container = GetInternalGasMix() ?? node;
		GasMix gasMix = container.GasMix;

		float oxygenUsed = HandleBreathing(gasMix);

		if (oxygenUsed > 0)
		{
			gasMix.RemoveGas(Gas.Oxygen, oxygenUsed);
			node.GasMix.AddGas(Gas.CarbonDioxide, oxygenUsed);
			registerTile.Matrix.MetaDataLayer.UpdateSystemsAt(registerTile.LocalPositionClient, SystemType.AtmosSystem);
			return true;
		}

		return false;
	}

	private GasContainer GetInternalGasMix()
	{
		if (playerScript != null)
		{

			// Check if internals exist
			var maskItemAttrs = playerScript.ItemStorage.GetNamedItemSlot(NamedSlot.mask).ItemAttributes;
			bool internalsEnabled = equipment.IsInternalsEnabled;
			if (maskItemAttrs != null && maskItemAttrs.CanConnectToTank && internalsEnabled)
			{
				foreach ( var gasSlot in playerScript.ItemStorage.GetGasSlots() )
				{
					if (gasSlot.Item == null) continue;
					var gasContainer = gasSlot.Item.GetComponent<GasContainer>();
					if ( gasContainer )
					{
						return gasContainer;
					}
				}
			}
		}

		return null;
	}

	private float HandleBreathing(GasMix gasMix)
	{
		float oxygenPressure = gasMix.GetPressure(Gas.Oxygen);

		float oxygenUsed = 0;

		if (oxygenPressure < OXYGEN_SAFE_MIN)
		{
			if (Random.value < 0.1)
			{
				Chat.AddActionMsgToChat(gameObject, "You gasp for breath", $"{gameObject.ExpensiveName()} gasps");
			}

			if (oxygenPressure > 0)
			{
				float ratio = 1 - oxygenPressure / OXYGEN_SAFE_MIN;
				bloodSystem.OxygenDamage += 1 * ratio;
				oxygenUsed = gasMix.GetMoles(Gas.Oxygen) * ratio * AtmosConstants.BREATH_VOLUME;
			}
			else
			{
				bloodSystem.OxygenDamage += 1;
			}
			IsSuffocating = true;
		}
		else
		{
			oxygenUsed = gasMix.GetMoles(Gas.Oxygen) * AtmosConstants.BREATH_VOLUME;
			IsSuffocating = false;
			bloodSystem.OxygenDamage -= 2.5f;
			breatheCooldown = 4;
		}
		return oxygenUsed;
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
		livingHealthBehaviour.ApplyDamage(null, amount, AttackType.Internal, damageType);
	}
}

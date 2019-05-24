using System.Collections.Generic;
using Atmospherics;
using Facepunch.Steamworks;
using Objects;
using UnityEngine;

/// <inheritdoc />
/// <summary>
/// Controls the RepiratorySystem for this living thing
/// Mostly managed server side and states sent to the clients
/// </summary>
public class RespiratorySystem : MonoBehaviour //Do not turn into NetBehaviour
{
	private const float OXYGEN_SAFE_MIN = 16;
	public bool IsBreathing { get; private set; } = true;
	public bool IsSuffocating { get; private set; }

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
	private float tick = 0f;
	private PlayerScript playerScript;
	private float breatheCooldown = 0;


	void Awake()
	{
		bloodSystem = GetComponent<BloodSystem>();
		livingHealthBehaviour = GetComponent<LivingHealthBehaviour>();
		playerScript = GetComponent<PlayerScript>();
		equipment = GetComponent<Equipment>();
		objectBehaviour = GetComponent<ObjectBehaviour>();
	}

	void OnEnable()
	{
		UpdateManager.Instance.Add(UpdateMe);
	}

	void OnDisable()
	{
		if (UpdateManager.Instance != null)
		{
			UpdateManager.Instance.Remove(UpdateMe);
		}
	}

	//Handle by UpdateManager
	void UpdateMe()
	{
		//Server Only:
		if (CustomNetworkManager.IsServer)
		{
			tick += Time.deltaTime;
			if (tick >= tickRate)
			{
				tick = 0f;
				MonitorSystem();
			}
		}
	}

	private void MonitorSystem()
	{
		if (!livingHealthBehaviour.IsDead)
		{
			Vector3Int position = objectBehaviour.AssumedLocation().RoundToInt();
			MetaDataNode node = MatrixManager.GetMetaDataAt(position);

			if (!IsEVACompatible())
			{
				CheckPressureDamage(node.GasMix.Pressure);
			}

			if(livingHealthBehaviour.OverallHealth >= HealthThreshold.SoftCrit){
				if (Breathe(node))
					{
						AtmosManager.Update(node);
					}
			}
			else{
				bloodSystem.OxygenDamage += 1;
			}
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
		GasMix breathGasMix = gasMix.RemoveVolume(AtmosConstants.BREATH_VOLUME, true);

		float oxygenUsed = HandleBreathing(breathGasMix);

		if (oxygenUsed > 0)
		{
			breathGasMix.RemoveGas(Gas.Oxygen, oxygenUsed);
			breathGasMix.AddGas(Gas.CarbonDioxide, oxygenUsed);
		}

		gasMix += breathGasMix;
		container.GasMix = gasMix;

		return oxygenUsed > 0;
	}

	private GasContainer GetInternalGasMix()
	{
		if (playerScript != null)
		{
			Dictionary<string, InventorySlot> inventory = playerScript.playerNetworkActions.Inventory;

			// Check if internals exist
			ItemAttributes mask = inventory.ContainsKey("mask") ? inventory["mask"]?.ItemAttributes : null;

			bool internalsEnabled = equipment.IsInternalsEnabled;

			//todo: devise a convenient method to get item/script from top level of inventory instead of this
			if (mask != null && mask.CanConnectToTank && internalsEnabled)
			{
				foreach ( var gasSlot in GasContainer.GasSlots )
				{
					if ( !inventory.ContainsKey(gasSlot) || inventory[gasSlot] == null || !inventory[gasSlot].Item )
					{
						continue;
					}

					var gasContainer = inventory[gasSlot].Item.GetComponent<GasContainer>();
					if ( gasContainer )
					{
						return gasContainer;
					}
				}
			}
		}

		return null;
	}

	private float HandleBreathing(GasMix breathGasMix)
	{
		float oxygenPressure = breathGasMix.GetPressure(Gas.Oxygen);

		float oxygenUsed = 0;

		if (oxygenPressure < OXYGEN_SAFE_MIN)
		{
			if (Random.value < 0.1)
			{
				PostToChatMessage.SendGasp(base.gameObject);
			}

			if (oxygenPressure > 0)
			{
				float ratio = 1 - oxygenPressure / OXYGEN_SAFE_MIN;
				bloodSystem.OxygenDamage += 1 * ratio;
				oxygenUsed = breathGasMix.GetMoles(Gas.Oxygen) * ratio;
			}
			else
			{
				bloodSystem.OxygenDamage += 1;
			}
		}
		else
		{
			oxygenUsed = breathGasMix.GetMoles(Gas.Oxygen);
			bloodSystem.OxygenDamage -= 2.5f;
			breatheCooldown = 4;
		}
		return oxygenUsed;
	}

	private void CheckPressureDamage(float pressure)
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

		Dictionary<string, InventorySlot> inventory = playerScript.playerNetworkActions.Inventory;

		ItemAttributes headItem = inventory.ContainsKey("head") ? inventory["head"]?.ItemAttributes : null;
		ItemAttributes suitItem = inventory.ContainsKey("suit") ? inventory["suit"]?.ItemAttributes : null;

		if (headItem != null && suitItem != null)
		{
			return headItem.IsEVACapable && suitItem.IsEVACapable;
		}

		return false;
	}

	private void ApplyDamage(float amount, DamageType damageType)
	{
		livingHealthBehaviour.ApplyDamage(null, amount, damageType);
	}

	// --------------------
	// UPDATES FROM SERVER
	// --------------------

	/// <summary>
	/// Updated from server via NetMsg
	/// </summary>
	public void UpdateClientRespiratoryStats(bool isBreathing, bool isSuffocating)
	{
		if (CustomNetworkManager.IsServer)
		{
			return;
		}

		IsBreathing = isBreathing;
		IsSuffocating = isSuffocating;
	}
}
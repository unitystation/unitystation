using System;
using UnityEngine;
using Systems.Atmospherics;
using Items;
using Objects.Atmospherics;
using Random = UnityEngine.Random;

/// <inheritdoc />
/// <summary>
/// Controls the RepiratorySystem for this living thing
/// Mostly managed server side and states sent to the clients
/// </summary>
public class RespiratorySystem : MonoBehaviour //Do not turn into NetBehaviour
{
	private const float OXYGEN_SAFE_MIN = 16; //minimum amount of oxygen before you start suffocating
	private const float PLASMA_SAFE_MAX = 0.4F; //maximum amount of plasma in the air before it starts killing you
	private const float PLASMA_WARNING_LEVEL = 0.2F; //minimum amount of plasma in the air before it starts showing warning signs
	private const float CARBON_DIOXIDE_SAFE_MAX = 10; //maximum amount of CO2 in the air before it starts killing you
	private const float CARBON_DIOXIDE_WARNING_LEVEL = 7.5F; //minimum amount of CO2 in the air before it starts showing warning signs
	private const int MIN_TOXIN_DMG = 1; //Minimum damage toxic air can deal
	private const int MAX_TOXIN_DMG = 10; //Maximum damage toxic air can deal
	public bool IsSuffocating;
	public float temperature = 293.15f;
	public float pressure = 101.325f;
	private float CarbonSuffocationTimer = 0;

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

	[SerializeField]
	private string[] CO2LowYouMessages =
	{
		"The air feels heavier than usual.",
		"You feel a little woozy.",
		"You cough."
	};

	[SerializeField]
	private string[] CO2LowOthersMessages =
	{
		"{0} wobbles on {1} feet.",
		"{0} coughs a little."
	};

	[SerializeField]
	private string[] CO2MedYouMessages =
	{
		"You can't breathe!",
		"You choke on the air!",
		"You gag on the air!"
	};

	[SerializeField]
	private string[] CO2MedOthersMessages =
	{
		"{0} is gagging violently!",
		"{0} is coughing and choking!"
	};

	[SerializeField]
	private string[] CO2HighYouMessages =
	{
		"You can't breathe at all!",
		"You're suffocating!",
		"The air feels like concrete!"
	};

	[SerializeField]
	private string[] CO2HighOthersMessages =
	{
		"{0} is convulsing violently!",
		"{0} is writhing in pain, unable to breathe!"
	};

	[SerializeField]
	private string[] plasmaLowYouMessages =
	{
		"Your nose is tingling!",
		"You feel an urge to cough!",
		"Your throat itches!",
		"You sneeze."
	};

	[SerializeField]
	private string[] plasmaLowOthersMessages =
	{
		"{0} scratches {1} nose.",
		"{0} clears {1} throat.",
		"{0} sneezes."
	};

	[SerializeField]
	private string[] plasmaHighYouMessages =
	{
		"Your throat stings as you draw a breath!",
		"Your throat burns as you draw a breath!"
	};

	[SerializeField]
	private string[] plasmaHighOthersMessages =
	{
		"{0} coughs.",
		"{0} coughs frantically."
	};

	[SerializeField]
	private string[] GasMaskFiltered =
	{
		"You feel a light tingling as the mask filters something out of the air.",
		"You catch a slight sensation of something in the air.",
		"You can hear your mask quietly make a hissing sound."
	};

	[SerializeField]
	private string[] GasMaskFilteredOthers =
	{
		"{0} breathes heavily under {1} mask.",
		"{0}'s mask makes a faint hissing sound."
	};

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
		// if (MatrixManager.IsInitialized && !CanBreatheAnywhere)
		// {
			// MonitorSystem();
		// }
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

		if (livingHealthBehaviour.OverallHealth >= livingHealthBehaviour.SOFTCRIT_THRESHOLD)
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

	private bool isWearingGasMask()
	{
		if (gameObject.Player() != null)
		{
			var maskItemAttrs = playerScript.ItemStorage.GetNamedItemSlot(NamedSlot.mask).ItemAttributes;
			if (maskItemAttrs != null && maskItemAttrs.HasTrait(CommonTraits.Instance.GasMask))
			{
				return true;
			}
		}
		return false;
	}

	private bool Breathe(IGasMixContainer node)
	{
		breatheCooldown--; //not timebased, but tickbased
		if (breatheCooldown > 0)
		{
			return false;
		}
		// if no internal breathing is possible, get the from the surroundings
		IGasMixContainer container = GetInternalGasMix() ?? node;
		GasMix gasMix = container.GasMix;

		float plasmaConsumed = 0;
		bool carbonDioxideInhaled = false;
		bool gasFiltered = false;
		float oxygenUsed = HandleBreathingOxygen(gasMix);

		if (isWearingGasMask())
		{
			gasFiltered = HandleWearingGasMask(gasMix);
		}
		if (gasFiltered == false)
		{
			plasmaConsumed = HandleBreathingPlasma(gasMix);
			carbonDioxideInhaled = HandleBreathingCarbonDioxide(gasMix);
		}
		if (oxygenUsed > 0)
		{
			gasMix.RemoveGas(Gas.Oxygen, oxygenUsed);
			node.GasMix.AddGas(Gas.CarbonDioxide, oxygenUsed);
			registerTile.Matrix.MetaDataLayer.UpdateSystemsAt(registerTile.LocalPositionClient, SystemType.AtmosSystem);
		}
		if (plasmaConsumed > 0)
		{
			gasMix.RemoveGas(Gas.Plasma, plasmaConsumed);
			registerTile.Matrix.MetaDataLayer.UpdateSystemsAt(registerTile.LocalPositionClient, SystemType.AtmosSystem);
		}
		if (oxygenUsed > 0 || plasmaConsumed > 0 || carbonDioxideInhaled)
		{
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

	private float HandleBreathingOxygen(GasMix gasMix)
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

	/// <summary>
	/// Placeholder method to add some effects for breathing plasma. Eventually this behavior should be
	/// handled with interfaces we can implement so different species react differently.
	/// </summary>
	/// <param name="plasmaAmount"></param>
	private float HandleBreathingPlasma(GasMix gasMix)
	{
		float plasmaPressure = gasMix.GetPressure(Gas.Plasma);

		float plasmaConsumed = 0;

		plasmaConsumed = gasMix.GetMoles(Gas.Plasma) * AtmosConstants.BREATH_VOLUME;
		// there is some plasma in the ambient but it is still safe
		if (plasmaPressure <= PLASMA_SAFE_MAX && plasmaPressure > PLASMA_WARNING_LEVEL)
		{
			if (DMMath.Prob(90))
			{
				return plasmaConsumed;
			}

			// 10% chances of message
			var theirPronoun = gameObject.Player() != null
				? gameObject.Player().Script.characterSettings.TheirPronoun(gameObject.Player().Script)
				: "its";
			Chat.AddActionMsgToChat(
				gameObject,
				plasmaLowYouMessages.PickRandom(),
				string.Format(
					plasmaLowOthersMessages.PickRandom(),
					gameObject.ExpensiveName(),
					string.Format(plasmaLowOthersMessages.PickRandom(), gameObject.ExpensiveName(), theirPronoun))
			);
		}
		// enough plasma to be visible and damage us!
		else if (plasmaPressure > PLASMA_SAFE_MAX)
		{
			var plasmaDamage = (gasMix.GetMoles(Gas.Plasma) / PLASMA_SAFE_MAX) * 10;
			bloodSystem.ToxinLevel = Mathf.Clamp(bloodSystem.ToxinLevel + Mathf.Clamp(plasmaDamage, MIN_TOXIN_DMG, MAX_TOXIN_DMG), 0, 200);

			if (DMMath.Prob(90))
			{
				return plasmaConsumed;
			}

			// 10% chances of message
			var theirPronoun = gameObject.Player() != null
				? gameObject.Player().Script.characterSettings.TheirPronoun(gameObject.Player().Script)
				: "its";
			Chat.AddActionMsgToChat(
				gameObject,
				plasmaHighYouMessages.PickRandom(),
				string.Format(plasmaHighOthersMessages.PickRandom(), gameObject.ExpensiveName(), theirPronoun)
			);
		}
		return plasmaConsumed;
	}

	private bool HandleBreathingCarbonDioxide(GasMix gasMix)
	{
		float carbonPressure = gasMix.GetPressure(Gas.CarbonDioxide);
		// there is a little carbon dioxide in the air.
		if (carbonPressure <= CARBON_DIOXIDE_SAFE_MAX && carbonPressure > CARBON_DIOXIDE_WARNING_LEVEL)
		{
			if (DMMath.Prob(90))
			{
				return true;
			}

			// 10% chances of message
			var theirPronoun = gameObject.Player() != null
				? gameObject.Player().Script.characterSettings.TheirPronoun(gameObject.Player().Script)
				: "its";
			Chat.AddActionMsgToChat(
				gameObject,
				CO2LowYouMessages.PickRandom(),
				string.Format(
					CO2LowOthersMessages.PickRandom(),
					gameObject.ExpensiveName(),
					string.Format(plasmaLowOthersMessages.PickRandom(), gameObject.ExpensiveName(), theirPronoun))
			);
		}
		// enough carbon dioxide to start suffocating you!
		else if (carbonPressure > CARBON_DIOXIDE_SAFE_MAX)
		{
			if (CarbonSuffocationTimer >= 0)
				CarbonSuffocationTimer++;
			if (CarbonSuffocationTimer >= 3 && CarbonSuffocationTimer < 7)
			{
				IsSuffocating = true;
				bloodSystem.OxygenDamage += 3;

				if (DMMath.Prob(90))
				{
					return true;
				}

				// 10% chances of message
				var theirPronoun = gameObject.Player() != null
					? gameObject.Player().Script.characterSettings.TheirPronoun(gameObject.Player().Script)
					: "its";
				Chat.AddActionMsgToChat(
					gameObject,
					CO2MedYouMessages.PickRandom(),
					string.Format(CO2MedOthersMessages.PickRandom(), gameObject.ExpensiveName(), theirPronoun)
				);
			}
			else if (CarbonSuffocationTimer >= 7)
			{
				IsSuffocating = true;
				bloodSystem.OxygenDamage += 8;

				if (DMMath.Prob(90))
				{
					return true;
				}

				// 10% chances of message
				var theirPronoun = gameObject.Player() != null
					? gameObject.Player().Script.characterSettings.TheirPronoun(gameObject.Player().Script)
					: "its";
				Chat.AddActionMsgToChat(
					gameObject,
					CO2HighYouMessages.PickRandom(),
					string.Format(CO2HighOthersMessages.PickRandom(), gameObject.ExpensiveName(), theirPronoun)
				);
				return true;
			}
		}

		if (carbonPressure < CARBON_DIOXIDE_WARNING_LEVEL)
		{
			CarbonSuffocationTimer = 0;
			return (carbonPressure > 0);
		}
		return false;
	}
	private bool HandleWearingGasMask(GasMix gasMix)
	{
		bool filtered = false;
		// if there is too much CO2 in the air
		if (gasMix.GetMoles(Gas.CarbonDioxide) >= 30)
		{
			GasMix gasMix2 = gasMix;
			gasMix2.RemoveGas(Gas.CarbonDioxide, 30);
			HandleBreathingCarbonDioxide(gasMix2);
			filtered = true;
		}
		// if there is too much plasma in the air
		if (gasMix.GetMoles(Gas.Plasma) >= 25)
		{
			GasMix gasMix2 = gasMix;
			gasMix2.RemoveGas(Gas.Plasma, 25);
			float plasmaBreathedWithMask = HandleBreathingPlasma(gasMix2);
			if (plasmaBreathedWithMask > 0)
			{
				gasMix.RemoveGas(Gas.Plasma, plasmaBreathedWithMask);
				registerTile.Matrix.MetaDataLayer.UpdateSystemsAt(registerTile.LocalPositionClient, SystemType.AtmosSystem);
			}
			filtered = true;
		}
		
		//if there's not enough to cause the plasma or CO2 warnings, skip the breathe messages
		if((gasMix.GetMoles(Gas.Plasma) < PLASMA_WARNING_LEVEL && gasMix.GetMoles(Gas.Plasma) > 0) || (gasMix.GetMoles(Gas.CarbonDioxide) < CARBON_DIOXIDE_WARNING_LEVEL && gasMix.GetMoles(Gas.CarbonDioxide) > 0))
		{
			return true;
		}
		
		//if somehow both are 0 return false
		if(gasMix.GetMoles(Gas.Plasma) == 0 && gasMix.GetMoles(Gas.CarbonDioxide) == 0)
		{
			return false;
		}

		if (DMMath.Prob(90))
		{
			return true;
		}

		if (!filtered)
		{
			// 10% chance of message
			var theirPronoun = gameObject.Player() != null
				? gameObject.Player().Script.characterSettings.TheirPronoun(gameObject.Player().Script)
				: "its";
			Chat.AddActionMsgToChat(
				gameObject,
				GasMaskFiltered.PickRandom(),
				string.Format(
					GasMaskFilteredOthers.PickRandom(),
					gameObject.ExpensiveName(),
					string.Format(plasmaLowOthersMessages.PickRandom(), gameObject.ExpensiveName(), theirPronoun))
			);
			return true;
		}
		return false;
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

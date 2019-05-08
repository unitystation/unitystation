using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
public class PowerGenerator : PowerSupplyControlInheritance
{
	public ObjectBehaviour objectBehaviour;
	[SyncVar(hook = "UpdateSecured")]
	public bool isSecured; //To ground
	private RegisterTile registerTile;
	public bool startSecured;
	public bool startAsOn;
	public Sprite generatorSecuredSprite;
	public Sprite generatorOnSprite;
	public Sprite generatorUnSecuredSprite;
	public SpriteRenderer spriteRend;
	public AudioSource generatorRunSfx;
	public AudioSource generatorEndSfx;
	public ParticleSystem smokeParticles;
	//Server only
	public List<SolidPlasma> plasmaFuel = new List<SolidPlasma>();

	void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
	}

	public override void OnStartServerInitialise()
	{
		CanConnectTo = new HashSet<PowerTypeCategory>
		{
			PowerTypeCategory.StandardCable,
			PowerTypeCategory.HighVoltageCable,
		};
		ApplianceType = PowerTypeCategory.PowerGenerator;
		// Voltage_source_voltage / Internal_resistance_of_voltage_source = 10 is good Rule of thumb
		powerSupply.InData.CanConnectTo = CanConnectTo;
		powerSupply.InData.Categorytype = ApplianceType;
		powerSupply.WireEndB = WireEndB;
		powerSupply.WireEndA = WireEndA;

		SupplyingVoltage = 760000;
		InternalResistance = 76000;
		//current = 20;

		//powerSupply.InData.ControllingDevice = this;
		//powerSupply.InData.ControllingUpdate = this;

		PowerInputReactions PIRMedium = new PowerInputReactions(); //You need a resistance on the output just so supplies can communicate properly
		PIRMedium.DirectionReaction = true;
		PIRMedium.ConnectingDevice = PowerTypeCategory.StandardCable;
		PIRMedium.DirectionReactionA.AddResistanceCall.ResistanceAvailable = true;
		PIRMedium.DirectionReactionA.YouShallNotPass = true;
		PIRMedium.ResistanceReaction = true;
		PIRMedium.ResistanceReactionA.Resistance.Ohms = MonitoringResistance;

		PowerInputReactions PIRHigh = new PowerInputReactions(); //You need a resistance on the output just so supplies can communicate properly
		PIRMedium.DirectionReaction = true;
		PIRMedium.ConnectingDevice = PowerTypeCategory.HighVoltageCable;
		PIRMedium.DirectionReactionA.AddResistanceCall.ResistanceAvailable = true;
		PIRMedium.DirectionReactionA.YouShallNotPass = true;
		PIRMedium.ResistanceReaction = true;
		PIRMedium.ResistanceReactionA.Resistance.Ohms = MonitoringResistance;

		powerSupply.InData.ConnectionReaction[PowerTypeCategory.HighVoltageCable] = PIRHigh;
		powerSupply.InData.ConnectionReaction[PowerTypeCategory.StandardCable] = PIRMedium;
		UpdateSecured(startSecured);
		StartCoroutine(CheckStartingPlasma());
	}

	/// <summary>
	/// Map solid plasma so that it is sitting on the same tile as the generator for it to be added
	/// to the starting plasma amounts.false Server Only.
	/// </summary>
	IEnumerator CheckStartingPlasma()
	{
		yield return YieldHelper.DeciSecond;
		var plasmaObjs = registerTile.Matrix.Get<SolidPlasma>(registerTile.Position);
		foreach (SolidPlasma plasma in plasmaObjs)
		{
			plasmaFuel.Add(plasma);
			plasma.GetComponent<CustomNetTransform>().DisappearFromWorldServer();
		}

		if (startAsOn)
		{
			UpdateServerState(startAsOn);
		}
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		UpdateState(isOn);
	}

	public override void StateChange(bool isOn)
	{
		if (isOn)
		{
			generatorRunSfx.Play();
			spriteRend.sprite = generatorOnSprite;
			smokeParticles.Play();
		}
		else
		{
			generatorRunSfx.Stop();
			smokeParticles.Stop();
			if (isSecured)
			{
				generatorEndSfx.Play();
				spriteRend.sprite = generatorSecuredSprite;
			}
			else
			{
				spriteRend.sprite = generatorUnSecuredSprite;
			}
		}
	}

	public override void UpdateServerState(bool _isOn)
	{
		if (_isOn)
		{
			if (TryBurnFuel())
			{
				powerSupply.TurnOnSupply();
				isOn = true;
			}
		}
		else
		{
			isOn = false;
			powerSupply.TurnOffSupply();
			if (plasmaFuel.Count > 0)
			{
				plasmaFuel[0].StopBurningPlasma();
			}
		}
	}

	void UpdateSecured(bool _isSecured)
	{
		isSecured = _isSecured;
		if (isServer)
		{
			objectBehaviour.isNotPushable = isSecured;
		}

		SoundManager.PlayAtPosition("Wrench", transform.position);

		if (!isSecured)
		{
			spriteRend.sprite = generatorUnSecuredSprite;
		}
		else
		{
			if (!isOn)
			{
				spriteRend.sprite = generatorSecuredSprite;
			}
			else
			{
				spriteRend.sprite = generatorOnSprite;
			}
		}
	}

	bool TryBurnFuel()
	{
		if (plasmaFuel.Count == 0)
		{
			return false;
		}

		if (plasmaFuel.Count > 0)
		{
			plasmaFuel[0].StartBurningPlasma(0.4f, FuelExhaustedEvent);
			return true;
		}
		return false;
	}

	//Server Only
	void FuelExhaustedEvent()
	{
		var pFuel = plasmaFuel[0];
		plasmaFuel.Remove(pFuel);
		if (isOn)
		{
			if (!TryBurnFuel())
			{
				UpdateServerState(false);
			}
		}
	}

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if (!isServer)
		{
			InteractMessage.Send(gameObject, hand);
		}
		else
		{
			var slot = InventoryManager.GetSlotFromOriginatorHand(originator, hand);
			var wrench = slot.Item?.GetComponent<WrenchTrigger>();
			if (wrench != null)
			{
				UpdateSecured(!isSecured);
				if (!isSecured && isOn)
				{
					isOn = !isOn;
					UpdateServerState(isOn);
				}
				return true;
			}

			var solidPlasma = slot.Item?.GetComponent<SolidPlasma>();
			if (solidPlasma != null)
			{
				plasmaFuel.Add(solidPlasma);
				InventoryManager.UpdateInvSlot(true, "", slot.Item, slot.UUID);
				return true;
			}

			if (isSecured)
			{
				UpdateServerState(!isOn);
			}
			ConstructionInteraction(originator, position, hand);
		}

		return true;
	}
}
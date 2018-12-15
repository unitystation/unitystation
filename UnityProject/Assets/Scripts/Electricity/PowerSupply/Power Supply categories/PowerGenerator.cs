using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
public class PowerGenerator : InputTrigger, IDeviceControl
{

	private bool SelfDestruct = false;

	public PowerSupply powerSupply;
	public PushPull pushPull;
	[SyncVar(hook = "UpdateState")]
	public bool isOn = false;
	[SyncVar(hook = "UpdateSecured")]
	public bool isSecured; //To ground
	public int DirectionStart = 0;
	public int DirectionEnd = 9;
	public float MonitoringResistance = 9999999999;
	public float current = 20;
	public float Previouscurrent = 20;
	public Sprite generatorSecuredSprite;
	public Sprite generatorOnSprite;
	public Sprite generatorUnSecuredSprite;
	public SpriteRenderer spriteRend;

	//Server only
	public List<SolidPlasma> plasmaFuel = new List<SolidPlasma>();

	public PowerTypeCategory ApplianceType = PowerTypeCategory.RadiationCollector;
	public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>()
	{
		PowerTypeCategory.StandardCable,
			PowerTypeCategory.HighVoltageCable,
	};

	public void PotentialDestroyed()
	{
		if (SelfDestruct)
		{
			//Then you can destroy
		}
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		powerSupply.InData.CanConnectTo = CanConnectTo;
		powerSupply.InData.Categorytype = ApplianceType;
		powerSupply.DirectionStart = DirectionStart;
		powerSupply.DirectionEnd = DirectionEnd;
		powerSupply.Data.SupplyingCurrent = 20;
		powerSupply.InData.ControllingDevice = this;

		PowerInputReactions PIRMedium = new PowerInputReactions(); //You need a resistance on the output just so supplies can communicate properly
		PIRMedium.DirectionReaction = true;
		PIRMedium.ConnectingDevice = PowerTypeCategory.MediumMachineConnector;
		PIRMedium.DirectionReactionA.AddResistanceCall.ResistanceAvailable = true;
		PIRMedium.DirectionReactionA.YouShallNotPass = true;
		PIRMedium.ResistanceReaction = true;
		PIRMedium.ResistanceReactionA.Resistance.Ohms = MonitoringResistance;

		isOn = false;
		UpdateServerState(isOn);
		UpdateSecured(false);
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		UpdateState(isOn);
	}

	void UpdateState(bool _isOn)
	{
		isOn = _isOn;
		if (isOn)
		{
			spriteRend.sprite = generatorOnSprite;
		}
		else
		{
			if (isSecured)
			{
				spriteRend.sprite = generatorSecuredSprite;
			}
			else
			{
				spriteRend.sprite = generatorSecuredSprite;
			}
		}
	}

	void UpdateServerState(bool _isOn)
	{
		if (isOn)
		{
			powerSupply.TurnOnSupply();
		}
		else
		{
			powerSupply.TurnOffSupply();
		}
	}

	void UpdateSecured(bool _isSecured)
	{
		isSecured = _isSecured;
		if (isServer)
		{
			pushPull.isNotPushable = isSecured;
		}
		else
		{
			Debug.Log("TODO: Wrench SoundFX");
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
	}

	public override void Interact(GameObject originator, Vector3 position, string hand)
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
				return;
			}

			var solidPlasma = slot.Item?.GetComponent<SolidPlasma>();
			if (solidPlasma != null)
			{
				plasmaFuel.Add(solidPlasma);
				InventoryManager.UpdateInvSlot(true, "", slot.Item, slot.UUID);
				return;
			}

			if (isSecured)
			{
				isOn = !isOn;
				UpdateServerState(isOn);
			}
		}
	}
}
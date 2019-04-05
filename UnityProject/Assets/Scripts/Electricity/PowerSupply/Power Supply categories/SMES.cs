using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SMES : PowerSupplyControlInheritance
{
	//public override bool isOnForInterface { get; set; } = false;
	//public override bool PassChangeToOff { get; set; } = false;
	public bool ResistanceChange = false;

	[SyncVar]
	public int currentCharge; // 0 - 100

	//Sprites:
	public Sprite offlineSprite;
	public Sprite onlineSprite;
	public Sprite[] chargeIndicatorSprites;
	public Sprite statusCriticalSprite;
	public Sprite statusSupplySprite;

	//Renderers:
	public SpriteRenderer statusIndicator;
	public SpriteRenderer OnOffIndicator;
	public SpriteRenderer chargeIndicator;

	public PowerTypeCategory ApplianceType = PowerTypeCategory.SMES;
	public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>
	{
		PowerTypeCategory.MediumMachineConnector
	};

	public override void OnStartServer()
	{
		base.OnStartServer();
		powerSupply.InData.CanConnectTo = CanConnectTo;
		powerSupply.InData.Categorytype = ApplianceType;
		powerSupply.DirectionStart = DirectionStart;
		powerSupply.DirectionEnd = DirectionEnd;

		resistance.Ohms = Resistance;
		resistance.ResistanceAvailable = false;

		PowerInputReactions PIRMedium = new PowerInputReactions(); //You need a resistance on the output just so supplies can communicate properly
		PIRMedium.DirectionReaction = true;
		PIRMedium.ConnectingDevice = PowerTypeCategory.MediumMachineConnector;
		PIRMedium.DirectionReactionA.AddResistanceCall.ResistanceAvailable = true;
		PIRMedium.DirectionReactionA.YouShallNotPass = true;
		PIRMedium.ResistanceReaction = true;
		PIRMedium.ResistanceReactionA.Resistance.Ohms = MonitoringResistance;

		PowerInputReactions PRSDCable = new PowerInputReactions();
		PRSDCable.DirectionReaction = true;
		PRSDCable.ConnectingDevice = PowerTypeCategory.StandardCable;
		PRSDCable.DirectionReactionA.AddResistanceCall = resistance;
		PRSDCable.ResistanceReaction = true;
		PRSDCable.ResistanceReactionA.Resistance = resistance;
		resistance.Ohms = 10000;

		powerSupply.InData.ConnectionReaction[PowerTypeCategory.MediumMachineConnector] = PIRMedium;
		powerSupply.InData.ConnectionReaction[PowerTypeCategory.StandardCable] = PRSDCable;
		currentCharge = 0;
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		UpdateState(isOn);
	}

	//Update the current State of the SMES (mostly visual for clientside updates) 
	public override void StateChange(bool isOn)
	{
		if (isOn)
		{
			OnOffIndicator.sprite = onlineSprite;
			chargeIndicator.gameObject.SetActive(true);
			statusIndicator.gameObject.SetActive(true);

			int chargeIndex = (currentCharge / 100) * 4;
			chargeIndicator.sprite = chargeIndicatorSprites[chargeIndex];
			if (chargeIndex == 0)
			{
				statusIndicator.sprite = statusCriticalSprite;
			}
			else
			{
				statusIndicator.sprite = statusSupplySprite;
			}
		}
		else
		{
			OnOffIndicator.sprite = offlineSprite;
			chargeIndicator.gameObject.SetActive(false);
			statusIndicator.gameObject.SetActive(false);
		}
	}

	[ContextMethod("Toggle Charge", "Power_Button")]
	public void ToggleCharge()
	{
		ToggleCanCharge = !ToggleCanCharge;
	}

	[ContextMethod("Toggle Support", "Power_Button")]
	public void ToggleSupport()
	{
		ToggleCansupport = !ToggleCansupport;
	}

	//FIXME: Objects at runtime do not get destroyed. Instead they are returned back to pool
	//FIXME: that also renderers IDevice useless. Please reassess
	public void OnDestroy()
	{
//		ElectricalSynchronisation.StructureChangeReact = true;
//		ElectricalSynchronisation.ResistanceChange = true;
//		ElectricalSynchronisation.CurrentChange = true;
		ElectricalSynchronisation.RemoveSupply(this, ApplianceType);
		SelfDestruct = true;
		//Make Invisible
	}

	public override void TurnOffCleanup (){
		BatteryCalculation.TurnOffEverything(this);
		ElectricalSynchronisation.RemoveSupply(this, ApplianceType);
	}
}
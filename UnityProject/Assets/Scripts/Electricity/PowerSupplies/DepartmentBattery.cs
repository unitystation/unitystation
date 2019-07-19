using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public enum BatteryStateSprite
{
	Full,
	Half,
	Empty,
}

public class DepartmentBattery : NetworkBehaviour, IInteractable<HandApply>, IInteractionProcessor<HandApply>, INodeControl
{
	public DepartmentBatterySprite CurrentSprite  = DepartmentBatterySprite.Default;
	public SpriteRenderer Renderer;

	public Sprite BatteryOpenPresent;
	public Sprite BatteryOpenMissing;
	public Sprite BatteryClosedMissing;

	public Sprite BatteryCharged;
	public Sprite PartialCharge;
	[SyncVar(hook = nameof(UpdateBattery))]
	public BatteryStateSprite CurrentState;

	public Sprite LightOn;
	public Sprite LightOff;
	public Sprite LightRed;

	public SpriteRenderer BatteryCompartmentSprite;
	public SpriteRenderer BatteryIndicatorSprite;
	public SpriteRenderer PowerIndicator;

	public List<DepartmentBatterySprite> enums;
	public List<Sprite> Sprite;
	public Dictionary<DepartmentBatterySprite,Sprite> Sprites = new Dictionary<DepartmentBatterySprite, Sprite>();

	public ElectricalNodeControl ElectricalNodeControl;
	public BatterySupplyingModule BatterySupplyingModule;

	[SyncVar(hook = nameof(UpdateState))]
	public bool isOn = false;


	[SyncVar]
	public int currentCharge; // 0 - 100

	void Start() {//Initialise Sprites
		for (int i = 0; i< enums.Count; i++)
		{
			Sprites[enums[i]] = Sprite[i];
		}

		if (enums.Count > 0)
		{
			Renderer.sprite = Sprites[CurrentSprite];
		}
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		UpdateState(isOn);
	}

	public void PowerNetworkUpdate() {
		if (BatterySupplyingModule.CurrentCapacity > 0)
		{
			if (BatterySupplyingModule.CurrentCapacity > (BatterySupplyingModule.CapacityMax / 2))
			{
				if (CurrentState != BatteryStateSprite.Full)
				{
					UpdateBattery(BatteryStateSprite.Full);
				}

			}
			else
			{
				if (CurrentState != BatteryStateSprite.Half)
				{
					UpdateBattery(BatteryStateSprite.Half);
				}
			}
		}
		else
		{
			if (CurrentState != BatteryStateSprite.Empty)
			{
				UpdateBattery(BatteryStateSprite.Empty);
			}
		}
	}

	void UpdateBattery(BatteryStateSprite State)
	{
		CurrentState = State;

		switch (CurrentState)
		{
			case BatteryStateSprite.Full:
				if (BatteryIndicatorSprite.enabled == false)
				{
					BatteryIndicatorSprite.enabled = true;
				}
				BatteryIndicatorSprite.sprite = BatteryCharged;
				break;
			case BatteryStateSprite.Half:
				if (BatteryIndicatorSprite.enabled == false)
				{
					BatteryIndicatorSprite.enabled = true;
				}
				BatteryIndicatorSprite.sprite = PartialCharge;
				break;
			case BatteryStateSprite.Empty:
				BatteryIndicatorSprite.enabled = false;
				break;
		}

	}

    public bool Interact(HandApply interaction)
    {
	    if (!DefaultWillInteract.HandApply(interaction, NetworkSide.Client)) return false;

		InteractionMessageUtils.SendRequest(interaction, this);
		return true;
    }


	public bool ServerProcessInteraction(HandApply interaction)
	{
		if (!DefaultWillInteract.HandApply(interaction, NetworkSide.Server)) return false;

		isOn = !isOn;
		UpdateServerState(isOn);
		return true;
	}
	public void UpdateServerState(bool _isOn)
	{
		if (isOn)
		{
			ElectricalNodeControl.TurnOnSupply();
		}
		else
		{
			ElectricalNodeControl.TurnOffSupply();
		}
	}

	public void UpdateState(bool _isOn)
	{
		isOn = _isOn;
		if (isOn)
		{
			PowerIndicator.sprite = LightOn;
		}
		else
		{
			PowerIndicator.sprite = LightOff;
		}
	}
}
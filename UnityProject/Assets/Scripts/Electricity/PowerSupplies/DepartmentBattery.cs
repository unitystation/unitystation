using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public enum BatteryStateSprite
{
	Full,
	Half,
	Empty,
}

public class DepartmentBattery : NetworkBehaviour, IInteractable<HandApply>, INodeControl
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

	private bool hasInit;


	[SyncVar]
	public int currentCharge; // 0 - 100

	void Start()
	{
		EnsureInit();
	}

	private void EnsureInit()
	{
		if (hasInit) return;
		for (int i = 0; i< enums.Count; i++)
		{
			Sprites[enums[i]] = Sprite[i];
		}

		if (enums.Count > 0)
		{
			Renderer.sprite = Sprites[CurrentSprite];
		}

		hasInit = true;
	}

	public override void OnStartClient()
	{
		EnsureInit();
		base.OnStartClient();
		UpdateState(isOn, isOn);
	}

	public void PowerNetworkUpdate() {
		if (BatterySupplyingModule.CurrentCapacity > 0)
		{
			if (BatterySupplyingModule.CurrentCapacity > (BatterySupplyingModule.CapacityMax / 2))
			{
				if (CurrentState != BatteryStateSprite.Full)
				{
					UpdateBattery(CurrentState, BatteryStateSprite.Full);
				}

			}
			else
			{
				if (CurrentState != BatteryStateSprite.Half)
				{
					UpdateBattery(CurrentState, BatteryStateSprite.Half);
				}
			}
		}
		else
		{
			if (CurrentState != BatteryStateSprite.Empty)
			{
				UpdateBattery(CurrentState, BatteryStateSprite.Empty);
			}
		}
	}

	void UpdateBattery(BatteryStateSprite oldState, BatteryStateSprite State)
	{
		EnsureInit();
		CurrentState = State;

		if (BatteryIndicatorSprite == null) return;

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

	public void ServerPerformInteraction(HandApply interaction)
	{
		isOn = !isOn;
		UpdateServerState(isOn);
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

	public void UpdateState(bool _wasOn, bool _isOn)
	{
		EnsureInit();
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
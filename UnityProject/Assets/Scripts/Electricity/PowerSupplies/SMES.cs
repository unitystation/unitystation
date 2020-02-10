using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SMES : NetworkBehaviour, IInteractable<HandApply>, INodeControl
{
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

	public ElectricalNodeControl ElectricalNodeControl;
	public BatterySupplyingModule BatterySupplyingModule;

	[SyncVar(hook = nameof(UpdateState))]
	public bool isOn = false;

	public override void OnStartClient()
	{
		UpdateState(isOn, isOn);
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

	public void PowerNetworkUpdate() { }

	public void UpdateState(bool _wasOn, bool _isOn)
	{
		isOn = _isOn;
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
}
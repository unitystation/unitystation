using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SMES : NetworkBehaviour, IInteractable<HandApply>, IInteractionProcessor<HandApply>, INodeControl
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

	[SyncVar(hook = "UpdateState")]
	public bool isOn = false;

	public override void OnStartClient()
	{
		base.OnStartClient();
		UpdateState(isOn);
	}

    public InteractionResult Interact(HandApply interaction)
	{
		// We can still re-use validators if we want:
		ValidationResult validationResult = CanApply.ONLY_IF_CONSCIOUS.Validate(interaction, NetworkSide.CLIENT);
		if (validationResult == ValidationResult.SUCCESS) { 
			RequestInteractMessage.Send(interaction, this);
			return (InteractionResult.SOMETHING_HAPPENED);
		}
		return (InteractionResult.NOTHING_HAPPENED);
	}

	//from IInteractionProcessor, this will be invoked when the server gets the RequestInteractMessage
	public InteractionResult ServerProcessInteraction(HandApply interaction)
	{
		ValidationResult validationResult = CanApply.ONLY_IF_CONSCIOUS.Validate(interaction, NetworkSide.SERVER);
		if (validationResult == ValidationResult.SUCCESS)
		{
			isOn = !isOn;
			UpdateServerState(isOn);
			return (InteractionResult.SOMETHING_HAPPENED);
		} 
		return (InteractionResult.NOTHING_HAPPENED);
		//validate and perform the update server side after it gets the RequestInteractMessage,
		//then inform all clients.
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

	public void UpdateState(bool _isOn)
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
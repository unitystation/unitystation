using System;
using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using Chemistry;
using UnityEngine;
using Mirror;
using UnityEngine.Events;

[RequireComponent(typeof(Pickupable))]
[RequireComponent(typeof(RadioMessager))]
[RequireComponent(typeof(RadioReceiver))]
[RequireComponent(typeof(HackingDevice))]
public class RemoteSignaller : NetworkBehaviour, IInteractable<HandActivate>, IServerSpawn
{
	private ItemAttributesV2 itemAtts;
	private RegisterTile registerTile;
	private Pickupable pickupable;
	private RadioMessager radioMessager;
	private RadioReceiver radioReceiver;
	private HackingDevice hackDevice;

	[SyncVar(hook = nameof(SyncIsOn))]
	private bool isOn;

	/// <summary>
	/// Is the signaler on?
	/// </summary>
	public bool IsOn => isOn;


	void Awake()
	{
		EnsureInit();
	}

	private void EnsureInit()
	{
		if (pickupable != null) return;

		pickupable = GetComponent<Pickupable>();
		itemAtts = GetComponent<ItemAttributesV2>();
		registerTile = GetComponent<RegisterTile>();

		radioMessager = GetComponent<RadioMessager>();
		radioReceiver = GetComponent<RadioReceiver>();

		hackDevice = GetComponent<HackingDevice>();
	}

	public override void OnStartClient()
	{
		EnsureInit();
		SyncIsOn(isOn, isOn);
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		EnsureInit();
		SyncIsOn(isOn, true);
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		if (IsOn)
		{
			if (interaction.Intent == Intent.Harm)
			{
				radioMessager.SendSignal();
			}
			else if (interaction.Intent == Intent.Disarm)
			{
				SyncIsOn(IsOn, !IsOn);
			}
			else if (interaction.Intent == Intent.Help)
			{
				//Ad frequency change code here.
			}
		}
	}

	private void SyncIsOn(bool _wasOn, bool _isOn)
	{
		isOn = _isOn;
	}

	/// <summary>
	/// Called when the remote signaler receives a signal. Puts a little message in chat if you're holding it.
	/// </summary>
	/// <param name="signal"></param>
	public void ServerReceiveSignal(RadioSignal signal)
	{
		if (pickupable.ItemSlot != null && pickupable.ItemSlot.Player != null)
		{
			UpdateChatMessage.Send(pickupable.ItemSlot.Player.gameObject, ChatChannel.Examine, ChatModifier.None, "You feel your signaler vibrate.");
		}

		hackDevice.SendOutputSignal();
	}
}
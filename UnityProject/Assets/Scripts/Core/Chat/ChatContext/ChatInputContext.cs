using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Items;
using Logs;

public class ChatInputContext : IChatInputContext
{
	/// <summary>
	/// Return default channel for a player. Depends on a current headset, antags, etc
	/// Can return ChatChannel.None if default channel is unknown
	/// Note: works only on a local client!
	/// </summary>
	public ChatChannel DefaultChannel
	{
		get
		{
			// Player doesn't even connected to the game?
			if (PlayerManager.LocalPlayerScript == null)
			{
				return ChatChannel.None;
			}

			// Player is some spooky ghost?
			if (PlayerManager.LocalPlayerScript.IsDeadOrGhost)
			{
				return ChatChannel.Ghost;
			}

			// Is not normal and not radio get default
			if (PlayerManager.LocalPlayerScript.IsNormal == false
			    && PlayerManager.LocalPlayerScript.PlayerTypeSettings.CheckForRadios == false)
			{
				return PlayerManager.LocalPlayerScript.PlayerTypeSettings.DefaultChannel;
			}

			// Player has some headset?
			var playerHeadset = GetPlayerHeadset();
			if (playerHeadset == null)
			{
				return PlayerManager.LocalPlayerScript.PlayerTypeSettings.DefaultChannel;
			}

			// Find default key for this channel
			var key = playerHeadset.EncryptionKey;
			if (EncryptionKey.DefaultChannel.ContainsKey(key) == false)
			{
				Loggy.LogError($"Can't find default channel for a {key}", Category.Chat);
				return PlayerManager.LocalPlayerScript.PlayerTypeSettings.DefaultChannel;
			}

			return EncryptionKey.DefaultChannel[key];
		}
	}

	// TODO: need to move it to Inventory.cs?
	private Headset GetPlayerHeadset()
	{
		var playerStorage = PlayerManager.LocalPlayerScript.GetComponent<DynamicItemStorage>();

		// Player has something in his ear?
		var itemSlotList = playerStorage.GetNamedItemSlots(NamedSlot.ear);
		foreach (var itemSlot in itemSlotList)
		{
			if (itemSlot.ItemObject)
			{
				var headset = itemSlot.ItemObject.GetComponent<Headset>();
				return headset;
			}
		}

		return null;
	}

}

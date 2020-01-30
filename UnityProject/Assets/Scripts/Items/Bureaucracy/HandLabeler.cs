using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandLabeler : NetworkBehaviour, ICheckedInteractable<HandApply>, IClientInteractable<HandActivate>, IServerSpawn
{
	public const int MAX_NAME_LENGTH = 16;

	private const int LABEL_CAPACITY = 30;

	[SyncVar]
	private int labelAmount;

	[SyncVar]
	private string currentLabel;
	
	public void OnInputReceived(string input)
	{
		if (labelAmount == 0) return;

		Chat.AddExamineMsgToClient("You set the " + this.gameObject.Item().InitialName.ToLower() + "s text to '" + input + "'.");
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRequestItemLabel(this.gameObject, input);
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		labelAmount = LABEL_CAPACITY;
		currentLabel = "";
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.HandObject != gameObject) return false;
		if (interaction.TargetObject.Item() == null) return false; //Only works on items

		if(currentLabel.Trim().Length == 0)
		{
			if (side == NetworkSide.Client)
				Chat.AddExamineMsgToClient("You haven't set a text yet.");

			return false;
		}

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		var item = interaction.TargetObject.Item();

		if (currentLabel.Length > HandLabeler.MAX_NAME_LENGTH)
		{
			currentLabel = currentLabel.Substring(0, HandLabeler.MAX_NAME_LENGTH);
		}

		item.ServerSetArticleName(item.InitialName + " '" + currentLabel + "'");

		Chat.AddActionMsgToChat(interaction, "You labeled " + item.InitialName + " as '" + currentLabel + "'.", interaction.Performer.Player().Name + " labeled " + item.InitialName + " as '" + currentLabel + "'.");
	}

	public bool Interact(HandActivate interaction)
	{
		UIManager.Instance.TextInputDialog.ShowDialog("Set label text", OnInputReceived);

		return true;
	}

	public void SetLabel(string label)
	{
		currentLabel = label;
	}
}

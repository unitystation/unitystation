using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuantumKey : MonoBehaviour, ICheckedInteractable<PositionalHandApply>, IInteractable<HandActivate>
{
	[HideInInspector]
	public QuantumPad padInBuffer;

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		if (Validations.IsTarget(gameObject, interaction)) return false;

		if (Validations.HasComponent<QuantumPad>(interaction.TargetObject)) return true;

		return false;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		var pad = interaction.TargetObject.GetComponent<QuantumPad>();
		if(pad == null) return;

		if (pad.disallowLinkChange)
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "The link has been hard locked, you cannot change it.");
			return;
		}

		if (pad == padInBuffer)
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "You cannot link the same pad together, clear the buffer if you wish to add to it.");
			return;
		}

		if (padInBuffer == null)
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "You set the buffer to this quantum pad.");
			padInBuffer = pad;
		}
		else
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "You set this quantum pad to connect to the pad in buffer.");
			pad.connectedPad = padInBuffer;
		}
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		Chat.AddExamineMsgFromServer(interaction.Performer, "You clear the quantum key buffer.");
		padInBuffer = null;
	}
}

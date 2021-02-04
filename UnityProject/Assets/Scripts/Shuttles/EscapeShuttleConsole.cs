using System.Collections;
using System.Collections.Generic;
using Strings;
using UnityEngine;

/// <summary>
/// Escape shuttle logic
/// </summary>
public class EscapeShuttleConsole : MonoBehaviour, ICheckedInteractable<HandApply>
{
	private bool beenEmagged;

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		return Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Emag);
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		TryEmagConsole(interaction);
	}

	private void TryEmagConsole(HandApply interaction)
	{
		if (beenEmagged)
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "The shuttle has already been emagged!");
			return;
		}

		beenEmagged = true;

		Chat.AddActionMsgToChat(interaction.Performer, "You emag the shuttle console", $"{interaction.Performer.ExpensiveName()} emags the shuttle console");

		if (GameManager.Instance.ShuttleSent) return;

		Chat.AddSystemMsgToChat("\n\n<color=#FF151F><size=40><b>Escape Shuttle Emergency Launch Triggered!</b></size></color>\n\n",
			MatrixManager.MainStationMatrix);

		Chat.AddSystemMsgToChat("\n\n<color=#FF151F><size=40><b>Escape Shuttle Emergency Launch Triggered!</b></size></color>\n\n",
			GameManager.Instance.PrimaryEscapeShuttle.MatrixInfo);

		SoundManager.PlayNetworked(SingletonSOSounds.Instance.Notice1);

		GameManager.Instance.ForceSendEscapeShuttleFromStation(10);
	}
}

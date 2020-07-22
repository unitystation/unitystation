using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allows Jukebox to be interacted with. Player can use the jukebox to choose the song to be played.
/// The Jukebox can be interacted with to, for example, check the song currently playing.
/// </summary>
[RequireComponent(typeof(Jukebox))]
public class InteractableJukebox : MonoBehaviour, ICheckedInteractable<HandApply>
{
	private Jukebox jukebox;
	private APCPoweredDevice power;

	// Start is called before the first frame update
	void Start()
	{
		jukebox = GetComponent<Jukebox>();
		power = GetComponent<APCPoweredDevice>();
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		//show the jukebox UI to the client
		TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType.Jukebox, TabAction.Open);		
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.TargetObject != gameObject) return false;

		// For a future iteration, allow the jukebox to be connected to the power grid.
		/*
		if (interaction.HandObject == null && power.State < PowerStates.On)
		{
			Chat.AddLocalMsgToChat("The Jukebox doesn't seem to have power.", gameObject);
			return false;
		}
		*/

		return true;
	}
}

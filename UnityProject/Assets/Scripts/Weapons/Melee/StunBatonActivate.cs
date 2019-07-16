using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(StunBaton))]
public class StunBatonActivate : Interactable<HandActivate>
{
	private StunBaton stunBaton;

	public void Start()
	{
		stunBaton = GetComponent<StunBaton>();
	}

	protected override void ClientPredictInteraction(HandActivate interaction)
	{
		stunBaton.ToggleState();
	}

	protected override void ServerPerformInteraction(HandActivate interaction)
	{
		SoundManager.PlayNetworkedAtPos(stunBaton.soundToggle, interaction.Performer.transform.position);
		stunBaton.ToggleState();
	}
}

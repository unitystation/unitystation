using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact : MonoBehaviour, IServerSpawn,
	ICheckedInteractable<HandApply>
{
	private ArtifactEffect currentEffect = null;
	private ArtifactTrigger currentTrigger;

	private void Awake()
	{
		currentEffect = GetComponent<ArtifactEffect>();
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		ServerSelectRandomTrigger();
	}

	public void ServerSelectRandomTrigger()
	{
		// get random trigger
		var allTriggers = System.Enum.GetValues(typeof(ArtifactTrigger));
		var triggerIndex = Random.Range(0, allTriggers.Length);
		currentTrigger = (ArtifactTrigger) allTriggers.GetValue(triggerIndex);
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		return DefaultWillInteract.Default(interaction, side);
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		// check if player tried touch artifact
		if (interaction.Intent != Intent.Harm)
		{
			if (currentTrigger == ArtifactTrigger.TOUCH)
			{
				currentEffect?.DoEffectTouch(interaction.Performer);
			}
			else
			{
				// print message that nothing happen
				Chat.AddExamineMsgFromServer(interaction.Performer,
					$"You touch {gameObject.ExpensiveName()}, but nothing happen. Maybe you need to activate it somehow...");
			}
		}
	}
}

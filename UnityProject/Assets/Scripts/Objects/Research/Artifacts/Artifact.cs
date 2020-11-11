using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact : MonoBehaviour, IServerSpawn,
	ICheckedInteractable<HandApply>
{
	[SerializeField]
	private ArtifactEffectsCollection possibleEffects = null;

	private ArtifactEffect currentEffect = null;
	private ArtifactTrigger currentTrigger;

	public void OnSpawnServer(SpawnInfo info)
	{
		ServerSelectRandomEffect();
		ServerSelectRandomTrigger();
	}

	public void ServerSelectRandomTrigger()
	{
		// get random trigger
		var allTriggers = System.Enum.GetValues(typeof(ArtifactTrigger));
		var triggerIndex = Random.Range(0, allTriggers.Length);
		currentTrigger = (ArtifactTrigger) allTriggers.GetValue(triggerIndex);
	}

	public void ServerSelectRandomEffect()
	{
		// check that possible effects are valid
		if (possibleEffects && possibleEffects.Effects != null)
		{
			var allEffects = possibleEffects.Effects;
			if (allEffects.Length > 0)
			{
				// select random effect
				var effectIndex = Random.Range(0, allEffects.Length);
				var selectedEffect = allEffects[effectIndex];

				// check that this effect is valid
				if (!selectedEffect)
				{
					Logger.LogError($"{possibleEffects} SO has invalid effect at index {effectIndex}!");
					return;
				}

				currentEffect = selectedEffect;
			}
		}
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
				currentEffect?.DoEffectTouch(this, interaction.Performer);
			}
		}
	}
}

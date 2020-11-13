using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact : MonoBehaviour, IServerSpawn,
	ICheckedInteractable<HandApply>
{
	public bool ForceTrigger = false;
	[ConditionalField(nameof(ForceTrigger))]
	public ArtifactTrigger ForcedTrigger;

	public float EffectTimeout = 10f;

	private ArtifactEffect currentEffect = null;
	private ArtifactTrigger currentTrigger;
	private float lastActivationTime;

	public bool UnderTimeout
	{
		get
		{
			// check that timeout has passed
			return Time.time - lastActivationTime < EffectTimeout;
		}
	}

	private void Awake()
	{
		currentEffect = GetComponent<ArtifactEffect>();
	}

	private void Update()
	{
		if (!CustomNetworkManager.IsServer)
		{
			return;
		}

		// check if artifact is always active and just emits aura
		if (currentTrigger == ArtifactTrigger.ALWAYS_ACTIVE)
		{
			TryActivateAura();
		}
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		// select trigger for artifact
		if (!ForceTrigger)
		{
			ServerSelectRandomTrigger();
		}
		else
		{
			currentTrigger = ForcedTrigger;
		}
	}

	public void ServerSelectRandomTrigger()
	{
		// get random trigger
		var allTriggers = System.Enum.GetValues(typeof(ArtifactTrigger));
		var triggerIndex = Random.Range(0, allTriggers.Length);
		currentTrigger = (ArtifactTrigger) allTriggers.GetValue(triggerIndex);
	}

	#region Touch Activation
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
				TryActivateByTouch(interaction.Performer);
			}
			else
			{
				// print message that nothing happen
				Chat.AddExamineMsgFromServer(interaction.Performer,
					$"You touch {gameObject.ExpensiveName()}, but nothing happen. Maybe you need to activate it somehow...");
			}
		}
	}

	public void TryActivateByTouch(GameObject performer)
	{
		if (!UnderTimeout)
		{
			currentEffect?.DoEffectTouch(performer);
			lastActivationTime = Time.time;
		}
	}
	#endregion

	public void TryActivateAura()
	{
		if (!UnderTimeout)
		{
			currentEffect?.DoEffectAura();
			lastActivationTime = Time.time;
		}
	}
}

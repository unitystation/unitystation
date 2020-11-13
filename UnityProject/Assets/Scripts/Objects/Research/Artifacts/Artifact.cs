using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ArtifactSprite
{
	public SpriteDataSO idleSprite;
	public SpriteDataSO activeSprite;
}

public class Artifact : MonoBehaviour, IServerSpawn,
	ICheckedInteractable<HandApply>
{
	[SerializeField]
	private SpriteHandler spriteHandler = null;

	public bool ForceTrigger = false;
	[ConditionalField(nameof(ForceTrigger))]
	public ArtifactTrigger ForcedTrigger;

	public ArtifactSprite[] RandomSprites;

	public float EffectTimeout = 10f;

	private ArtifactEffect currentEffect = null;
	private ArtifactTrigger currentTrigger;
	private ArtifactSprite currentSprite;

	private Coroutine animationCoroutine;
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

		// select random sprite
		ServerSelectRandomSprite();
	}

	public void ServerSelectRandomTrigger()
	{
		// get random trigger
		var allTriggers = System.Enum.GetValues(typeof(ArtifactTrigger));
		var triggerIndex = Random.Range(0, allTriggers.Length);
		currentTrigger = (ArtifactTrigger) allTriggers.GetValue(triggerIndex);
	}

	public void ServerSelectRandomSprite()
	{
		currentSprite = RandomSprites.PickRandom();
		spriteHandler?.SetSpriteSO(currentSprite.idleSprite);
	}

	#region Interactions
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
	#endregion

	public void TryActivateByTouch(GameObject performer)
	{
		if (!UnderTimeout)
		{
			currentEffect?.DoEffectTouch(performer);
			PlayActivationAnimation();

			lastActivationTime = Time.time;
		}
	}

	public void TryActivateAura()
	{
		if (!UnderTimeout)
		{
			currentEffect?.DoEffectAura();
			PlayActivationAnimation();

			lastActivationTime = Time.time;
		}
	}

	public void PlayActivationAnimation()
	{
		if (animationCoroutine != null)
			StopCoroutine(animationCoroutine);
		StartCoroutine(ActivationAnimationRoutine());
	}

	private IEnumerator ActivationAnimationRoutine()
	{
		if (spriteHandler && currentSprite != null)
		{
			// set animation sprite
			spriteHandler.SetSpriteSO(currentSprite.activeSprite);
			// wait for animation to play (just random time)
			yield return WaitFor.Seconds(1f);
			// return back to idle state
			spriteHandler.SetSpriteSO(currentSprite.idleSprite);
		}
	}
}

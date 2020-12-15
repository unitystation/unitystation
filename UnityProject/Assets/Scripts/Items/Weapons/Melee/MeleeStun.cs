using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;
using AddressableReferences;

/// <summary>
/// Adding this to a weapon stuns the target on hit
/// </summary>
public class MeleeStun : MonoBehaviour, ICheckedInteractable<HandApply>
{
	/// <summary>
	/// How long to stun for (in seconds)
	/// </summary>
	[SerializeField]
	private float stunTime = 0;
	/// <summary>
	/// how long till you can stun again
	/// </summary>
	[SerializeField]
	private int delay = 3;
	/// <summary>
	/// if you can stun
	/// </summary>
	private bool canStun = true;

	/// <summary>
	/// Sounds to play when stunning someone
	/// </summary>
	[SerializeField] private AddressableAudioSource stunSound = null;

	private int timer = 0;

	//Send only one message per second.
	private bool coolDownMessage;

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		return interaction.UsedObject == gameObject
			&& interaction.TargetObject.GetComponent<RegisterPlayer>();
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		GameObject target = interaction.TargetObject;
		GameObject performer = interaction.Performer;

		// Direction for lerp
		Vector2 dir = (target.transform.position - performer.transform.position).normalized;

		WeaponNetworkActions wna = performer.GetComponent<WeaponNetworkActions>();

		// If we're on harm intent we deal damage!
		// Note: this has to be done before the stun, because otherwise if we hit ourselves with an activated stun baton on harm intent.
		// We wouldn't deal damage to ourselves because CmdRequestMeleeAttack checks whether we're stunned
		if (interaction.Intent == Intent.Harm)
		{
			// Direction of attack towards the attack target.
			wna.ServerPerformMeleeAttack(target, dir, interaction.TargetBodyPart, LayerType.None);
		}

		RegisterPlayer registerPlayerVictim = target.GetComponent<RegisterPlayer>();

		// Stun the victim. We check if there is a cooldown preventing the attacker from stunning.
		if (registerPlayerVictim && canStun)
		{
			if (delay != 0)
			{
				canStun = false;
				timer = delay;
			}

			registerPlayerVictim.ServerStun(stunTime);
			SoundManager.PlayNetworkedAtPos(stunSound, target.transform.position, sourceObj: target.gameObject);
			// deactivates the stun and makes you wait;

			// Special case: If we're off harm intent (only stunning), we should still show the lerp (unless we're hitting ourselves).
			if (interaction.Intent != Intent.Harm && performer != target)
			{
				wna.RpcMeleeAttackLerp(dir, gameObject);
			}
		}
		else if (!canStun)
		{
			if (coolDownMessage) return;
			coolDownMessage = true;
			Chat.AddExamineMsg(performer, $"{gameObject.ExpensiveName()} is on cooldown.");
		}
	}

	private void OnEnable()
	{
		UpdateManager.Add(Timer, 1f);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, Timer);
	}

	private void Timer()
	{
		if(timer == 0) return;

		timer--;

		coolDownMessage = false;

		if (timer == 0)
		{
			canStun = true;
		}
	}
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adding this to a weapon stuns the target on hit
/// If the weapon has the StunBaton behaviour it only stuns when the baton is active
/// </summary>
public class MeleeStun : MonoBehaviour, ICheckedInteractable<HandApply>
{
	/// <summary>
	/// How long to stun for (in seconds)
	/// </summary>
	[SerializeField]
	private float stunTime = 0;

	/// <summary>
	/// Sounds to play when stunning someone
	/// </summary>
	[SerializeField]
	private string stunSound = "EGloves";

	private StunBaton stunBaton;

	public void Start()
	{
		stunBaton = GetComponent<StunBaton>();
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		return interaction.UsedObject == gameObject
			&& (!stunBaton || stunBaton.isActive)
			&& interaction.TargetObject.GetComponent<RegisterPlayer>();
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		GameObject target = interaction.TargetObject;
		GameObject performer = interaction.Performer;

		// Direction for lerp
		Vector2 dir = (target.transform.position - performer.transform.position).normalized;

		WeaponNetworkActions wna = performer.GetComponent<WeaponNetworkActions>();

		// If we're not on help intent we deal damage!
		// Note: this has to be done before the stun, because otherwise if we hit ourselves with an activated stun baton on harm intent
		// we wouldn't deal damage to ourselves because CmdRequestMeleeAttack checks whether we're stunned
		if (interaction.Intent != Intent.Help)
		{
			// Direction of attack towards the attack target.
			wna.ServerPerformMeleeAttack(target, dir, interaction.TargetBodyPart, LayerType.None);
		}

		RegisterPlayer registerPlayerVictim = target.GetComponent<RegisterPlayer>();

		// Stun the victim. We checke whether the baton is activated in WillInteract
		if (registerPlayerVictim)
		{
			registerPlayerVictim.ServerStun(stunTime);
			SoundManager.PlayNetworkedAtPos(stunSound, target.transform.position, sourceObj: target.gameObject);

			// Special case: If we're on help intent (only stun), we should still show the lerp (unless we're hitting ourselves)
			if (interaction.Intent == Intent.Help && performer != target)
			{
				wna.RpcMeleeAttackLerp(dir, gameObject);
			}
		}
	}
}

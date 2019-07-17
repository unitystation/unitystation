using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adding this to a weapon stuns the target on hit
/// If the weapon has the StunBaton behaviour it only stuns when the baton is active
/// </summary>
public class MeleeStun : Interactable<HandApply>
{
	/// <summary>
	/// How long to stun for (in seconds)
	/// </summary>
	[SerializeField]
	private float stunTime;

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

	protected override bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!base.WillInteract(interaction, side)) return false;

		return interaction.UsedObject == gameObject
			&& (!stunBaton || stunBaton.isActive)
			&& interaction.TargetObject.GetComponent<RegisterPlayer>();
	}

	protected override void ServerPerformInteraction(HandApply interaction)
	{
		GameObject target = interaction.TargetObject;
		GameObject performer = interaction.Performer;

		// Direction for lerp
		Vector2 dir = (target.transform.position - performer.transform.position).normalized;

		WeaponNetworkActions wna = performer.GetComponent<WeaponNetworkActions>();

		// If we're not on help intent we deal damage!
		// Note: this has to be done before the stun, because otherwise if we hit ourselves with an activated stun baton on harm intent
		// we wouldn't deal damage to ourselves because CmdRequestMeleeAttack checks whether we're stunned
		bool helpIntent = interaction.Performer.GetComponent<PlayerMove>().IsHelpIntent;
		if (!helpIntent)
		{
			// Direction of attack towards the attack target.
			wna.CmdRequestMeleeAttack(target, gameObject, dir, interaction.TargetBodyPart, LayerType.None);
		}

		RegisterPlayer registerPlayerVictim = target.GetComponent<RegisterPlayer>();

		// Stun the victim. We checke whether the baton is activated in WillInteract
		if (registerPlayerVictim)
		{
			registerPlayerVictim.Stun(stunTime);
			SoundManager.PlayNetworkedAtPos(stunSound, target.transform.position);

			// Special case: If we're on help intent (only stun), we should still show the lerp (unless we're hitting ourselves)
			if (helpIntent && performer != target)
			{
				wna.RpcMeleeAttackLerp(dir, gameObject);
			}
		}
	}
}

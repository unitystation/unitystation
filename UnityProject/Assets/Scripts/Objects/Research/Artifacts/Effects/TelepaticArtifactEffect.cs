using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// This artifact sends telepatic messages
/// </summary>
public class TelepaticArtifactEffect : ArtifactEffect
{
	[Tooltip("How far artifact sends telepatic message")]
	public int auraRadius = 10;

	public string[] Messages;
	public string[] DrasticMessages;

	public override void DoEffectTouch(HandApply touchSource)
	{
		base.DoEffectTouch(touchSource);
		Indocrinate(touchSource.Performer);
	}

	public override void DoEffectAura()
	{
		base.DoEffectAura();
		IndocrinateMessageArea();
	}

	public override void DoEffectPulse(GameObject pulseSource)
	{
		base.DoEffectPulse(pulseSource);
		IndocrinateMessageArea();
	}

	private void IndocrinateMessageArea()
	{
		var objCenter = gameObject.AssumedWorldPosServer().RoundToInt();
		var hitMask = LayerMask.GetMask("Players");
		var playerColliders = Physics2D.OverlapCircleAll(new Vector2(objCenter.x, objCenter.y), auraRadius, hitMask);

		foreach (var playerColl in playerColliders)
		{
			playerColl.TryGetComponent<PlayerScript>(out var player);

			if (player == null || player.IsDeadOrGhost || player.IsNormal == false) continue;

			Indocrinate(player.gameObject);
		}
	}

	private void Indocrinate(GameObject target)
	{
		if (Random.value > 0.2f)
			Chat.AddWarningMsgFromServer(target, Messages.PickRandom());
		else
			Chat.AddWarningMsgFromServer(target, DrasticMessages.PickRandom());
	}
}

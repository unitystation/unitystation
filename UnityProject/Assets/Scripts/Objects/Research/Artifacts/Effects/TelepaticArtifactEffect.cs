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
		var xMin = objCenter.x - auraRadius;
		var yMin = objCenter.y - auraRadius;
		var bounds = new BoundsInt(xMin, yMin, 0, 20, 20, 1);
		foreach (var connected in PlayerList.Instance.InGamePlayers)
		{
			var player = connected.Script;
			if (bounds.Contains(player.WorldPos) && player.IsDeadOrGhost == false)
			{
				Indocrinate(player.gameObject);
			}
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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
		// get effect shape around artifact
		var objCenter = gameObject.AssumedWorldPosServer().RoundToInt();
		var shape = EffectShape.CreateEffectShape(EffectShapeType.Square, objCenter, auraRadius);

		foreach (var pos in shape)
		{
			// check if tile has any alive player
			var players = MatrixManager.GetAt<PlayerScript>(pos, true).Distinct();
			foreach (var player in players)
			{
				if (!player.IsDeadOrGhost)
				{
					// send them message
					Indocrinate(player.gameObject);
				}
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

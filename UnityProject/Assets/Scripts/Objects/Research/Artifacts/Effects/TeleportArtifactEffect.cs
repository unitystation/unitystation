using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Systems.Teleport;
using UnityEngine;

public class TeleportArtifactEffect : ArtifactEffect
{
	public bool avoidSpace;
	public bool avoidImpassable = true;

	[Tooltip("How far artifact sends will detect players to teleport")]
	public int teleportAuraRadius = 10;

	[Tooltip("Min distance players could be teleported")]
	public int minTeleportDistance = 0;
	[Tooltip("Max distance players could be teleported")]
	public int maxTeleportDistance = 16;

	public EffectShapeType effectShapeType = EffectShapeType.Square;

	public override void DoEffectTouch(HandApply touchSource)
	{
		base.DoEffectTouch(touchSource);
		TeleportUtils.ServerTeleportRandom(touchSource.Performer, minTeleportDistance, maxTeleportDistance, avoidSpace, avoidImpassable);
	}

	public override void DoEffectAura()
	{
		base.DoEffectAura();

		// get effect shape around artifact
		var objCenter = gameObject.AssumedWorldPosServer().RoundToInt();
		var shape = EffectShape.CreateEffectShape(effectShapeType, objCenter, teleportAuraRadius);

		foreach (var pos in shape)
		{
			// check if tile has any alive player
			var players = MatrixManager.GetAt<PlayerScript>(pos, true).Distinct();
			foreach (var player in players)
			{
				if (!player.IsDeadOrGhost)
				{
					// teleport player
					TeleportUtils.ServerTeleportRandom(player.gameObject, minTeleportDistance, maxTeleportDistance, avoidSpace, avoidImpassable);
				}
			}
		}
	}
}

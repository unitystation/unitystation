using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DamageTile", menuName = "Interaction/TileInteraction/DamageTile")]
public class DamageTile : TileInteraction
{
	public override bool WillInteract(TileApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.Intent != Intent.Harm) return false;
		if (interaction.HandObject == null) return false;

		//don't allow spamming window knocks really fast
		return Cooldowns.TryStart(interaction, this, 1, side);
	}

	public override void ServerPerformInteraction(TileApply interaction)
	{
		if (interaction.HandObject != null)
		{
			var weaponNA = interaction.Performer.GetComponent<WeaponNetworkActions>();
			if (weaponNA == null) return;
			weaponNA.ServerPerformMeleeAttack(interaction.TileChangeManager.MetaTileMap.matrix.transform.parent.gameObject,
				interaction.TargetVector, BodyPartType.None, interaction.BasicTile.LayerType);
		}
	}
}

using UnityEngine;
using US13.HealthV2;
using US13.Items.Weapons;
using US13.UI.Systems.MainHUD.UI_Bottom;

namespace US13.Core.Input_System.InteractionV2.TileInteraction
{
	[CreateAssetMenu(fileName = "DamageTile", menuName = "Interaction/TileInteraction/DamageTile")]
	public class DamageTile : TileInteraction
	{
		public override bool WillInteract(TileApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (interaction.Intent != Intent.Harm) return false;
			if (interaction.HandObject == null) return false;

			//don't allow spamming window knocks really fast
			return Cooldowns.Cooldowns.TryStart(interaction, this, 1, side);
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
}

using System.Collections.Generic;
using UnityEngine;
using Core.Editor.Attributes;
using Items;
using Logs;


namespace Systems.Interaction
{
	// Do not derive from NetworkBehaviour, this is also used on tilemap layers
	/// <summary>
	/// Allows an object or tiles to be attacked by melee.
	/// </summary>
	public class Meleeable : MonoBehaviour, IPredictedCheckedInteractable<PositionalHandApply>
	{
		[SerializeField ]
		private bool isMeleeable = true;
		// If it has this component, isn't it assumed to be meleeable? Is this still true for tilemaps?
		public bool IsMeleeable
		{
			get
			{
				if (isMeleeable == false)
				{
					Loggy.LogWarning($"Remove {nameof(Meleeable)} component from {this} if it isn't meleeable, " +
						$"instead of relying on the isMeleeable field.");
				}
				return isMeleeable;
			}
			set
			{
				isMeleeable = value;
			}
		}

		/// <summary>
		/// Which layers are allowed to be attacked
		/// </summary>
		private static readonly HashSet<LayerType> attackableLayers = new HashSet<LayerType>(
			new[]
			{
			LayerType.Grills,
			LayerType.Walls,
			LayerType.Windows
			});

		private InteractableTiles interactableTiles;

		private void Start()
		{
			interactableTiles = GetComponent<InteractableTiles>();
		}

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (isMeleeable == false) return false;
			if (DefaultWillInteract.Default(interaction, side, Validations.CheckState(x => x.CanMelee)) == false) return false;
			// must be targeting us
			if (interaction.TargetObject != gameObject) return false;
			// allowed to attack due to cooldown?
			// note: actual cooldown is started in WeaponNetworkActions melee logic on server side,
			// clientPredictInteraction on clientside
			if (side == NetworkSide.Client && Cooldowns.IsOn(interaction, CooldownID.Asset(CommonCooldowns.Instance.Melee, side))) return false;

			bool LocalItemCheck()
			{
				return interaction.HandObject.OrNull()?.Item().CanBeUsedOnSelfOnHelpIntent ?? false;
			}
			// not punching unless harm intent
			if (interaction.Intent != Intent.Harm && !LocalItemCheck()) return false;

			// if attacking tiles, only some layers are allowed to be attacked
			if (interactableTiles != null)
			{
				var tileAt = interactableTiles.LayerTileAt(interaction.WorldPositionTarget, true);

				// Nothing there, could be space?
				if (tileAt == null) return false;

				if (attackableLayers.Contains(tileAt.LayerType) == false) return false;
			}

			return true;
		}

		public void ClientPredictInteraction(PositionalHandApply interaction)
		{
			// start clientside melee cooldown so we don't try to spam melee
			// requests to server
			Cooldowns.TryStartClient(interaction, CommonCooldowns.Instance.Melee);
		}

		// no rollback logic
		public void ServerRollbackClient(PositionalHandApply interaction) { }

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			var handObject = interaction.HandObject;

			var wna = interaction.Performer.GetComponent<WeaponNetworkActions>();
			var layerType = LayerType.None;
			if (interactableTiles != null)
			{
				// attacking tiles
				layerType = interactableTiles.LayerTileAt(interaction.WorldPositionTarget, true).LayerType;
			}

			wna.ServerPerformMeleeAttack(gameObject, interaction.TargetVector, interaction.TargetBodyPart, layerType);
			if (Validations.HasItemTrait(handObject, CommonTraits.Instance.Breakable))
			{
				handObject.GetComponent<ItemBreakable>()?.AddDamage();
			}
		}
	}
}

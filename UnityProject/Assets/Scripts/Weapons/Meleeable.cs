using System.Collections.Generic;
using UnityEngine;

//Do not derive from NetworkBehaviour, this is also used on tilemap layers
/// <summary>
/// Allows an object or tiles to be attacked by melee.
/// </summary>
public class Meleeable : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
{
	/// <summary>
	/// Which layers are allowed to be attacked on tiles regardless of intent
	/// </summary>
	private static readonly HashSet<LayerType> attackableLayers = new HashSet<LayerType>(
	new[] {
		LayerType.Grills,
		LayerType.Walls,
		LayerType.Windows
	});

	/// <summary>
	/// Which layers are allowed to be attacked on tiles only on harm intent
	/// </summary>
	/// NOTE: Not allowing attacking base or floors now because it's annoying during combat when you misclick
	// private static readonly HashSet<LayerType> harmIntentOnlyAttackableLayers = new HashSet<LayerType>(
	// 	new[] {
	// 		LayerType.Base,
	// 		LayerType.Floors
	// 	});
	private static readonly HashSet<LayerType> harmIntentOnlyAttackableLayers = new HashSet<LayerType>();

	private InteractableTiles interactableTiles;

	private void Start()
	{
		interactableTiles = GetComponent<InteractableTiles>();
	}

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		//are we in range
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		//must be targeting us
		if (interaction.TargetObject != gameObject) return false;
		//allowed to attack due to cooldown?
		var playerScript = interaction.Performer.GetComponent<PlayerScript>();
		//NOTE: we never start this cooldown on client side because there are too many
		//factors that only server knows that would influence whether they actually hit,
		//and we want to be fair to the client during combat and not lock them out of melee when they
		//never actually hit something.
		if (playerScript.IsOnCooldown(CooldownCategory.Melee))
		{
			return false;
		}

		//not punching unless harm intent
		if (interaction.HandObject == null && interaction.Intent != Intent.Harm) return false;

		//if attacking tiles, only some layers are allowed to be attacked
		if (interactableTiles != null)
		{
			var tileAt = interactableTiles.LayerTileAt(interaction.WorldPositionTarget);
			if (!attackableLayers.Contains(tileAt.LayerType))
			{
				return interaction.Intent == Intent.Harm && harmIntentOnlyAttackableLayers.Contains(tileAt.LayerType);
			}
		}

		return true;
	}

	//no rollback logic
	public void ServerRollbackClient(PositionalHandApply interaction) { }

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		var wna = interaction.Performer.GetComponent<WeaponNetworkActions>();
		if (interactableTiles != null)
		{
			//attacking tiles
			var tileAt = interactableTiles.LayerTileAt(interaction.WorldPositionTarget);
			wna.ServerPerformMeleeAttack(gameObject, interaction.TargetVector, BodyPartType.None, tileAt.LayerType);
		}
		else
		{
			//attacking objects
			wna.ServerPerformMeleeAttack(gameObject, interaction.TargetVector, interaction.TargetBodyPart, LayerType.None);
		}
	}

}
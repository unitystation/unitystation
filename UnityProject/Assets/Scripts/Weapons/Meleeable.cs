using System.Collections.Generic;
using UnityEngine;

//Do not derive from NetworkBehaviour, this is also used on tilemap layers
/// <summary>
/// Allows an object or tiles to be attacked by melee.
/// </summary>
public class Meleeable : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
{
	/// <summary>
	/// Which layers are allowed to be attacked on tiles
	/// </summary>
	private static readonly HashSet<LayerType> attackableLayers = new HashSet<LayerType>(
	new[] {
		LayerType.Base,
		LayerType.Floors,
		LayerType.Grills,
		LayerType.Walls,
		LayerType.Windows
	});

	//Cache these on start for checking at runtime
	private GameObject gameObjectRoot;
	private InteractableTiles interactableTiles;

	private void Start()
	{
		gameObjectRoot = transform.root.gameObject;
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
		if (!playerScript.weaponNetworkActions.AllowAttack)
		{
			return false;
		}

		//not punching unless harm intent
		if (interaction.HandObject == null && interaction.Intent != Intent.Harm) return false;

		//if attacking tiles, only some layers are allowed to be attacked
		if (interactableTiles != null)
		{
			var tileAt = interactableTiles.LayerTileAt(interaction.WorldPositionTarget);
			return attackableLayers.Contains(tileAt.LayerType);
		}

		return true;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		var wna = interaction.Performer.GetComponent<WeaponNetworkActions>();
		if (interactableTiles != null)
		{
			//attacking tiles
			var tileAt = interactableTiles.LayerTileAt(interaction.WorldPositionTarget);
			wna.CmdRequestMeleeAttack(gameObject, interaction.TargetVector, BodyPartType.None, tileAt.LayerType);
		}
		else
		{
			//attacking objects
			wna.CmdRequestMeleeAttack(gameObject, interaction.TargetVector, interaction.TargetBodyPart, LayerType.None);
		}
	}
}
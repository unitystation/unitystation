using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
///  Extended interaction logic for grilles. Checks if the performer should be electrocuted.
/// </summary>
[CreateAssetMenu(fileName = "GrilleInteraction", menuName = "Interaction/TileInteraction/GrilleInteraction")]
public class GrilleInteraction : DeconstructWhenItemUsed
{
	private TileApply interaction;

	public override void ServerPerformInteraction(TileApply interaction)
	{
		this.interaction = interaction;

		// If true, cancel the interact and apply electric shock.
		if (ShouldApplyShock())
		{
			ApplyShock();
		}
		else
        {
			base.ServerPerformInteraction(interaction);
        }
	}

	private bool ShouldApplyShock()
    {
		// TODO: Check if exposed cable node underneath
		bool cableNodeUnderneath = false;


		// TileApply properties
		Vector3Int targetCellPos = interaction.TargetCellPos;
		//InteractibleTiles targetInteractibleTiles = interaction.TargetInteractibleTiles;
		TileChangeManager tileChangeManager = interaction.TileChangeManager;
		BasicTile basicTile = interaction.BasicTile;
		ItemSlot handSlot = interaction.HandSlot;
		GameObject handObject = interaction.HandObject; // Or UsedObject? Change above too
		Vector2 worldPositionTarget = interaction.WorldPositionTarget;
		//ApplyType applyType = interaction.TileApplyType;
		// TileApply methods
		// No methods other than ToString();

		//Chat.AddExamineMsgFromServer(interaction, targetInteractibleTiles);



		// TODO: Check if cable underneath is powered
		bool cableNodePowered = false;
		var insulated = interaction.PerformerPlayerScript.ItemStorage.GetNamedItemSlot(NamedSlot.hands)
				.ItemAttributes.HasTrait(CommonTraits.Instance.Insulated);
		float shockChance = Random.value;

		if (!cableNodeUnderneath) return false;
		if (!cableNodePowered) return false;
		if (insulated) return false;
		if (shockChance > 0.6f) return false; // Chance grille manages to shock.

		return true;
	}

	private void ApplyShock()
    {
		// TODO: Implement shock
		// Remove the message when the shock animation has been implemented.
		Chat.AddExamineMsgFromServer(interaction.Performer, "You were shocked!");
	}
}

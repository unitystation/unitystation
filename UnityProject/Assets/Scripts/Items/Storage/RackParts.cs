using UnityEngine;
using ScriptableObjects;

public class RackParts : MonoBehaviour, ICheckedInteractable<PositionalHandApply>, ICheckedInteractable<InventoryApply>
{
	private static readonly StandardProgressActionConfig ProgressConfig =
		new StandardProgressActionConfig(StandardProgressActionType.Construction, allowMultiple: true);

	[Tooltip("The prefab to spawn when the rack parts are assembled.")]
	[SerializeField]
	private GameObject rackPrefab = default;

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
		{
			return false;
		}

		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
		{
			return true;
		}

		// Must be constructing the rack somewhere empty
		var vector = interaction.WorldPositionTarget.RoundToInt();
		if (!MatrixManager.IsPassableAtAllMatrices(vector, vector, false))
		{
			return false;
		}

		return true;
	}

	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
		{
			return false;
		}

		if (interaction.TargetObject != gameObject
		    || !Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wrench))
		{
			return false;
		}

		//only works if wrench is in hand
		if (!interaction.IsFromHandSlot) return false;

		return true;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
		{
			ToolUtils.ServerPlayToolSound(interaction);
			Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, interaction.WorldPositionTarget.RoundToInt(), transform.parent, count: 1,
				scatterRadius: Spawn.DefaultScatterRadius, cancelIfImpassable: true);
			Despawn.ServerSingle(gameObject);

			return;
		}

		void ProgressComplete()
		{
			Chat.AddExamineMsgFromServer(interaction.Performer,
					"You assemble a rack.");
			Spawn.ServerPrefab(rackPrefab, interaction.WorldPositionTarget.RoundToInt(),
				interaction.Performer.transform.parent);
			var handObj = interaction.HandObject;

			if (handObj != null && handObj.GetInstanceID() == gameObject.GetInstanceID()) // the rack parts were assembled from the hands, despawn in inventory-fashion
			{ // (note: instanceIDs used in case somebody starts assembling rack parts on the ground with rack parts in hand (which was not possible at the time this was written))
				Inventory.ServerDespawn(interaction.HandSlot);
			}
			else // the rack parts were assembled from the ground, despawn in general fashion
			{
				Despawn.ServerSingle(gameObject);
			}
		}

		var bar = StandardProgressAction.Create(ProgressConfig, ProgressComplete)
			.ServerStartProgress(interaction.WorldPositionTarget.RoundToInt(), 5f, interaction.Performer);
		if (bar != null)
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "You start constructing a rack...");
		}
	}

	public void ServerPerformInteraction(InventoryApply interaction)
	{
		ToolUtils.ServerPlayToolSound(interaction);
		Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, interaction.Performer.WorldPosServer().CutToInt(), transform.parent, count: 1,
			scatterRadius: Spawn.DefaultScatterRadius, cancelIfImpassable: true);
		Inventory.ServerDespawn(interaction.FromSlot);
	}
}

using UnityEngine;
using Objects.Construction;

/// <summary>
/// Main component for soap. Allows cleaning of tiles and gradual degradation of the soap as it is used.
/// </summary>
[RequireComponent(typeof(Pickupable))]
public class Soap : MonoBehaviour, ICheckedInteractable<PositionalHandApply>, IExaminable, IServerSpawn
{
	private static readonly StandardProgressActionConfig ProgressConfig =
		new StandardProgressActionConfig(StandardProgressActionType.Mop, true, false);

	[Tooltip("How many times can the soap be used until it is deleted?")]
	[SerializeField]
	private int uses = 100;

	private int maxUses;

	[Tooltip("Time taken to clean something with the soap, measured in seconds.")]
	[SerializeField]
	private float useTime = 3.5f;


	public void OnSpawnServer(SpawnInfo info)
	{
		maxUses = uses;
	}

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		//can only scrub tiles, for now
		if (!Validations.HasComponent<InteractableTiles>(interaction.TargetObject)) return false;

		//don't attempt to scrub walls
		if (MatrixManager.IsWallAtAnyMatrix(interaction.WorldPositionTarget.RoundToInt(), isServer: side == NetworkSide.Server))
		{
			return false;
		}

		return true;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		//server is performing server-side logic for the interaction
		//do the scrubbing
		void CompleteProgress()
		{
			CleanTile(interaction);
			Chat.AddExamineMsg(interaction.Performer, "You finish scrubbing.");
		}

		//Start the progress bar:
		var bar = StandardProgressAction.Create(ProgressConfig, CompleteProgress)
			.ServerStartProgress(interaction.WorldPositionTarget.RoundToInt(),
				useTime, interaction.Performer);
		if (bar)
		{
			Chat.AddActionMsgToChat(interaction.Performer,
				$"You begin to scrub the floor with the {gameObject.ExpensiveName()}...",
				$"{interaction.Performer.name} begins to scrub the floor with the {gameObject.ExpensiveName()}.");
		}
	}

	private void CleanTile(PositionalHandApply interaction)
	{
		var worldPos = interaction.WorldPositionTarget;
		var checkPos = worldPos.RoundToInt();
		var matrixInfo = MatrixManager.AtPoint(checkPos, true);
		matrixInfo.MetaDataLayer.Clean(checkPos, checkPos, false);
		UseUpSoap();
	}

	public void UseUpSoap()
	{
		uses -= 1;
		if (uses == 0)
		{
			Despawn.ServerSingle(gameObject);
		}
	}

	public string Examine(Vector3 worldPos = default)
	{
		float percentageLeft = (float) uses / (float) maxUses;
		
		if (percentageLeft <= 0.15f)
		{
			return "There's just a tiny bit left of what it used to be, you're not sure it'll last much longer.";
		}
		else if (percentageLeft > 0.15f && percentageLeft <= 0.30f)
		{
			return "It's dissolved quite a bit, but there's still some life to it.";
		}
		else if (percentageLeft > 0.30f && percentageLeft <= 0.50f)
		{
			return "It's past its prime, but it's definitely still good.";
		}
		else if (percentageLeft > 0.50f && percentageLeft <= 0.75f)
		{
			return "It's started to get a little smaller than it used to be, but it'll definitely still last for a while.";
		}
		else if (percentageLeft > 0.75f && percentageLeft < 1f)
		{
			return "It's seen some light use, but it's still pretty fresh.";
		}
		else
		{
			return "It looks like it just came out of the package.";
		}
	}

}

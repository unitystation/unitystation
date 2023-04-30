using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ScriptableObjects;
using Tiles;

namespace Objects.Construction
{
	/// <summary>
	/// The main reinforced girder component
	/// </summary>
	public class ReinforcedGirder : NetworkBehaviour, ICheckedInteractable<HandApply>, IServerSpawn, IExaminable
	{
		private TileChangeManager tileChangeManager;

		private bool strutsUnsecured;

		[Tooltip("Normal girder prefab.")]
		[SerializeField]
		private GameObject girder = null;

		[Tooltip("Tile to spawn when wall is constructed.")]
		[SerializeField]
		private BasicTile reinforcedWallTile = null;

		private void Start()
		{
			tileChangeManager = GetComponentInParent<TileChangeManager>();
			GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			strutsUnsecured = false;
		}

		private void OnWillDestroyServer(DestructionInfo arg0)
		{
			Spawn.ServerPrefab(CommonPrefabs.Instance.Plasteel, gameObject.TileWorldPosition().To3Int(), transform.parent, count: 1,
				scatterRadius: Spawn.DefaultScatterRadius, cancelIfImpassable: true);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			//start with the default HandApply WillInteract logic.
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			//only care about interactions targeting us
			if (interaction.TargetObject != gameObject) return false;
			//only try to interact if the user has plasteel, screwdriver, or wirecutter
			if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.PlasteelSheet) &&
				!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver) &&
				!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wirecutter)) return false;
			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.TargetObject != gameObject) return;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.PlasteelSheet))
			{
				if (strutsUnsecured == false)
				{
					ToolUtils.ServerUseToolWithActionMessages(interaction, 5f,
						"You start finalizing the reinforced wall...",
						$"{interaction.Performer.ExpensiveName()} starts finalizing the reinforced wall...",
						"You fully reinforce the wall.",
						$"{interaction.Performer.ExpensiveName()} fully reinforces the wall.",
						() => ConstructReinforcedWall(interaction));
				}
			}
			else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver))
			{
				if (strutsUnsecured == false)
				{
					ToolUtils.ServerUseToolWithActionMessages(interaction, 4f,
						"You start unsecuring the support struts...",
						$"{interaction.Performer.ExpensiveName()} starts unsecuring the support struts...",
						"You unsecure the support struts.",
						$"{interaction.Performer.ExpensiveName()} unsecures the support struts.",
						() => strutsUnsecured = true);
				}
				else
				{
					ToolUtils.ServerUseToolWithActionMessages(interaction, 4f,
						"You start securing the support struts...",
						$"{interaction.Performer.ExpensiveName()} starts securing the support struts...",
						"You secure the support struts.",
						$"{interaction.Performer.ExpensiveName()} secure the support struts.",
						() => strutsUnsecured = false);
				}
			}
			else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wirecutter))
			{
				if (strutsUnsecured)
				{
					ToolUtils.ServerUseToolWithActionMessages(interaction, 4f,
						"You start removing the inner grille...",
						$"{interaction.Performer.ExpensiveName()} starts removing the inner grille...",
						"You remove the inner grille.",
						$"{interaction.Performer.ExpensiveName()} removes the inner grille.",
						() =>
						{
							Spawn.ServerPrefab(girder, SpawnDestination.At(gameObject));
							Spawn.ServerPrefab(CommonPrefabs.Instance.Plasteel, SpawnDestination.At(gameObject));
							_ = Despawn.ServerSingle(gameObject);
						});
				}
			}
		}

		public string Examine(Vector3 worldPos)
		{
			return (strutsUnsecured ? "Secure support struts with a screwdriver, or remove the inner grille with a wirecutter."
					: "Add Plasteel to finalize the reinforced wall, or use a screwdriver to unsecure the support struts.");

		}

		[Server]
		private void ConstructReinforcedWall(HandApply interaction)
		{
			tileChangeManager.MetaTileMap.SetTile(Vector3Int.RoundToInt(transform.localPosition), reinforcedWallTile);
			interaction.HandObject.GetComponent<Stackable>().ServerConsume(1);
			_ = Despawn.ServerSingle(gameObject);
		}
	}
}

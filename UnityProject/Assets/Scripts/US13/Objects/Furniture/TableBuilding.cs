using Mirror;
using UnityEngine;
using US13.Core.Addressables;
using US13.Core.Addressables.Types;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Health.Objects;
using US13.Items;
using US13.Items.Tool;
using US13.Items.Traits;
using US13.Managers;
using US13.Messages.Server.SoundMessages;
using US13.ScriptableObjects;
using US13.Tilemaps.Behaviours;
using US13.Tilemaps.Tiles;
using Util;
using Random = UnityEngine.Random;

namespace US13.Objects.Furniture
{
	public class TableBuilding : NetworkBehaviour, ICheckedInteractable<HandApply>
	{
		[Tooltip("If apply Metal Sheet.")]
		[SerializeField]
		private LayerTile metalTable = default;

		[Tooltip("If apply Glass Sheet.")]
		[SerializeField]
		private LayerTile glassTable = default;

		[Tooltip("If apply Wood Plank.")]
		[SerializeField]
		private LayerTile woodTable = default;

		[Tooltip("If apply Wood Plank.")]
		[SerializeField]
		private LayerTile reinforcedTable = default;

		private Integrity integrity;

		private void Start()
		{
			integrity = gameObject.GetComponent<Integrity>();
			integrity.OnWillDestroyServer.AddListener(OnWillDestroyServer);
		}

		private void OnWillDestroyServer(DestructionInfo arg0)
		{
			Spawn.ServerPrefab(CommonPrefabs.Instance.MetalRods, gameObject.TileWorldPosition().To3Int(), transform.parent,
				count: Random.Range(0, 3), scatterRadius: Random.Range(0f, 2f));
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			//start with the default HandApply WillInteract logic.
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			//only care about interactions targeting us
			if (interaction.TargetObject != gameObject) return false;

			if (interaction.HandObject == null) return false;

			if (interaction.HandObject.GetComponent<Stackable>().Amount < 2) return false;

			//only try to interact if the user has a wrench, screwdriver in their hand
			if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench) &&
				!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.MetalSheet) &&
				!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.GlassSheet) &&
				!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.WoodenPlank) &&
				!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.PlasteelSheet)) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.TargetObject != gameObject) return;
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
			{
				ToolUtils.ServerUseToolWithActionMessages(interaction, 0.5f,
					"You start deconstructing the table frame...",
					$"{interaction.Performer.ExpensiveName()} starts deconstructing the table frame...",
					"You finish deconstructing the table frame.",
					$"{interaction.Performer.ExpensiveName()} deconstructs the table frame.",
					Disassemble);
				ToolUtils.ServerPlayToolSound(interaction);
			}
			else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.MetalSheet))
			{
				Assemble(interaction, "metal", metalTable, CommonSounds.Instance.Deconstruct);
			}
			else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.GlassSheet))
			{
				Assemble(interaction, "glass", glassTable, CommonSounds.Instance.GlassHit);
			}
			else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.WoodenPlank))
			{
				Assemble(interaction, "wooden", woodTable, CommonSounds.Instance.wood3);
			}
			else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.PlasteelSheet))
			{
				Assemble(interaction, "reinforced", reinforcedTable, CommonSounds.Instance.Deconstruct);
			}
		}

		private void Assemble(HandApply interaction, string tableType, LayerTile layerTile, AddressableAudioSource assemblySound)
		{
			ToolUtils.ServerUseToolWithActionMessages(interaction, 0.5f,
				$"You start constructing a {tableType} table...",
				$"{interaction.Performer.ExpensiveName()} starts constructing a {tableType} table...",
				$"You finish assembling the {tableType} table.",
				$"{interaction.Performer.ExpensiveName()} assembles a {tableType} table.",
				() => SpawnTable(interaction, layerTile));
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: 1f);
			SoundManager.PlayNetworkedAtPos(assemblySound, gameObject.AssumedWorldPosServer(), audioSourceParameters, sourceObj: gameObject);
		}

		private void Disassemble()
		{
			Spawn.ServerPrefab(CommonPrefabs.Instance.MetalRods, gameObject.AssumedWorldPosServer(), count: 2);
			_ = Despawn.ServerSingle(gameObject);
		}

		private void SpawnTable(HandApply interaction, LayerTile tableToSpawn)
		{
			var interactableTiles = InteractableTiles.GetAt(interaction.TargetObject.TileWorldPosition(), true);
			Vector3Int cellPos = interactableTiles.WorldToCell(interaction.TargetObject.TileWorldPosition());
			interaction.HandObject.GetComponent<Stackable>().ServerConsume(2);
			interactableTiles.TileChangeManager.MetaTileMap.SetTile(cellPos, tableToSpawn);
			interactableTiles.TileChangeManager.SubsystemManager.UpdateAt(cellPos);
			_ = Despawn.ServerSingle(gameObject);
		}
	}
}

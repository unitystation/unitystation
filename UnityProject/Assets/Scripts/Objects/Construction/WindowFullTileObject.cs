using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;
using AddressableReferences;
using Messages.Server.SoundMessages;
using Shared;
using Tiles;

namespace Objects.Construction
{
	/// <summary>
	/// The main girder component
	/// </summary>
	public class WindowFullTileObject : NetworkBehaviour, ICheckedInteractable<HandApply>
	{
		protected RegisterObject registerObject;
		protected UniversalObjectPhysics objectPhysics;

		[Header("Tile creation variables")]
		[Tooltip("Layer tile which this will create when placed.")]
		public LayerTile layerTile;

		[Header("Deconstruct variables.")]
		[Tooltip("Drops this when deconstructed.")]
		public List<DeconstructionData> onDeconstruct;

		[Serializable]
		public struct DeconstructionData
		{
			public GameObject prefab;
			public int count;
		}

		[Tooltip("Sound on deconstruction.")]
		public AddressableAudioSource soundOnDeconstruct;

		[Serializable]
		public struct DestroyData
		{
			public GameObject prefab;
			public int minCountOnDestroy;
			public int maxCountOnDestroy;
		}

		[Header("Destroyed variables.")]
		[Tooltip("Drops this when broken with force.")]
		public List<DestroyData> onDestroy;

		[Tooltip("Sound when destroyed.")]
		[SerializeField] private AddressableAudioSource soundOnDestroy = null;

		private void Awake()
		{
			registerObject = GetComponent<RegisterObject>();
			objectPhysics = GetComponent<UniversalObjectPhysics>();
		}

		protected virtual void OnEnable()
		{
			GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
		}

		protected virtual void OnDisable()
		{
			GetComponent<Integrity>().OnWillDestroyServer.RemoveListener(OnWillDestroyServer);
		}

		private void OnWillDestroyServer(DestructionInfo arg0)
		{
			foreach (var mat in onDestroy)
			{
				Spawn.ServerPrefab(mat.prefab, gameObject.TileWorldPosition().To3Int(), transform.parent, count: Random.Range(mat.minCountOnDestroy, mat.maxCountOnDestroy + 1),
					scatterRadius: Random.Range(0, 3), cancelIfImpassable: true);
			}

			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: 1f);
			SoundManager.PlayNetworkedAtPos(soundOnDestroy, gameObject.TileWorldPosition().To3Int(), audioSourceParameters, sourceObj: gameObject);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			//start with the default HandApply WillInteract logic.
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			//only care about interactions targeting us
			if (interaction.TargetObject != gameObject) return false;
			//only try to interact if the user has a wrench, screwdriver in their hand
			if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench) &&
				!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver)) { return false; }

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.TargetObject != gameObject) return;


			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver))
			{
				if (objectPhysics.IsNotPushable == false)
				{
					//secure it if there's floor
					if (MatrixManager.IsSpaceAt(registerObject.WorldPositionServer, true, registerObject.Matrix.MatrixInfo))
					{
						Chat.AddExamineMsg(interaction.Performer, "A floor must be present to secure the window!");
						return;
					}

					if (!ServerValidations.IsAnchorBlocked(interaction))
					{

						ToolUtils.ServerUseToolWithActionMessages(interaction, 4f,
							"You start securing the window...",
							$"{interaction.Performer.ExpensiveName()} starts securing the window...",
							"You secure the window.",
							$"{interaction.Performer.ExpensiveName()} secures the window.",
							() => ChangeAnchorStatus(interaction, true));
						return;
					}
				}
				else
				{
					//unsecure it
					ToolUtils.ServerUseToolWithActionMessages(interaction, 4f,
						"You start unsecuring the window...",
						$"{interaction.Performer.ExpensiveName()} starts unsecuring the window...",
						"You unsecure the window.",
						$"{interaction.Performer.ExpensiveName()} unsecures the window.",
						() => ChangeAnchorStatus(interaction, false));
				}

			}
			else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
			{
				//disassemble if it's unanchored
				if (objectPhysics.IsNotPushable == false)
				{
					ToolUtils.ServerUseToolWithActionMessages(interaction, 4f,
						"You start to disassemble the window...",
						$"{interaction.Performer.ExpensiveName()} starts to disassemble the window...",
						"You disassemble the window.",
						$"{interaction.Performer.ExpensiveName()} disassembles the window.",
						() => Disassemble(interaction));
					return;
				}
				else
				{
					Chat.AddExamineMsg(interaction.Performer, "You must unsecure it first.");
				}
			}

		}

		[Server]
		protected virtual void ChangeAnchorStatus(HandApply interaction, bool newState)
		{
			if (newState == false)
			{
				objectPhysics.ServerSetAnchored(false, interaction.Performer);
				return;
			}

			var interactableTiles = InteractableTiles.GetAt(interaction.TargetObject.TileWorldPosition(), true);
			Vector3Int cellPos = interactableTiles.WorldToCell(interaction.TargetObject.TileWorldPosition());
			interactableTiles.TileChangeManager.MetaTileMap.SetTile(cellPos, layerTile);
			interactableTiles.TileChangeManager.SubsystemManager.UpdateAt(cellPos);
			_ = Despawn.ServerSingle(gameObject);
		}

		[Server]
		protected virtual void Disassemble(HandApply interaction)
		{
			foreach (var mat in onDeconstruct)
			{
				Spawn.ServerPrefab(mat.prefab, registerObject.WorldPositionServer, count: mat.count);
			}

			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: 1f);
			SoundManager.PlayNetworkedAtPos(soundOnDeconstruct, gameObject.TileWorldPosition().To3Int(), audioSourceParameters, sourceObj: gameObject);
			_ = Despawn.ServerSingle(gameObject);
		}
	}
}

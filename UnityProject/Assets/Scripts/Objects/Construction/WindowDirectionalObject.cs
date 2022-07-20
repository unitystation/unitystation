using System;
using System.Collections.Generic;
using AddressableReferences;
using Messages.Server.SoundMessages;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;


namespace Objects.Construction
{
	/// <summary>
	/// Used for directional windows, based on WindowFullTileObject.
	/// </summary>
	public class WindowDirectionalObject : NetworkBehaviour, ICheckedInteractable<HandApply>
	{
		private RegisterObject registerObject;
		private UniversalObjectPhysics objectPhysics;

		//PM: Objects below don't have to be shards or rods, but it's more convenient for me to put "shards" and "rods" in the variable names.
		[Header("Destroyed variables.")]
		[Tooltip("Drops this when broken with force.")]
		public GameObject shardsOnDestroy;

		[Tooltip("Drops this count when destroyed.")]
		public int minCountOfShardsOnDestroy;

		[Tooltip("Drops this count when destroyed.")]
		public int maxCountOfShardsOnDestroy;

		[Tooltip("Drops this when broken with force.")]
		public GameObject rodsOnDestroy;

		[Tooltip("Drops this count when destroyed.")]
		public int minCountOfRodsOnDestroy;

		[Tooltip("Drops this count when destroyed.")]
		public int maxCountOfRodsOnDestroy;

		[Tooltip("Sound when destroyed.")]
		public AddressableAudioSource soundOnDestroy;

		[Header("Deconstruction variables")]
		[Tooltip("Items to drop when deconstructed.")]
		public GameObject matsOnDeconstruct;

		[Tooltip("Quantity of mats when deconstructed.")]
		public int countOfMatsOnDissasemle;

		[Tooltip("Sound on deconstruction.")]
		public AddressableAudioSource soundOnDeconstruct;

		private void Awake()
		{
			registerObject = GetComponent<RegisterObject>();
			objectPhysics = GetComponent<UniversalObjectPhysics>();
		}

		private void OnEnable()
		{
			GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
			objectPhysics.OnLocalTileReached.AddListener(OnLocalTileChange);
		}

		private void OnDisable()
		{
			GetComponent<Integrity>().OnWillDestroyServer.RemoveListener(OnWillDestroyServer);
			objectPhysics.OnLocalTileReached.RemoveListener(OnLocalTileChange);
		}

		private void OnLocalTileChange(Vector3Int oldLocalPos, Vector3Int newLocalPos)
		{
			//We have moved from old spot so redo atmos blocks for new and old positions
			UpdateSubsystemsAt(oldLocalPos);

			if(oldLocalPos == newLocalPos) return;

			UpdateSubsystemsAt(newLocalPos);
		}

		private void OnWillDestroyServer(DestructionInfo arg0)
		{
			Spawn.ServerPrefab(shardsOnDestroy, gameObject.TileWorldPosition().To3Int(), transform.parent, count: Random.Range(minCountOfShardsOnDestroy, maxCountOfShardsOnDestroy + 1),
				scatterRadius: Random.Range(0, 3), cancelIfImpassable: true);

			Spawn.ServerPrefab(rodsOnDestroy, gameObject.TileWorldPosition().To3Int(), transform.parent, count: Random.Range(minCountOfRodsOnDestroy, maxCountOfRodsOnDestroy + 1),
				scatterRadius: Random.Range(0, 3), cancelIfImpassable: true);

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
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench) == false &&
				Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver) == false ) return false;

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

					if (ServerValidations.IsAnchorBlocked(interaction) == false)
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
		private void ChangeAnchorStatus(HandApply interaction, bool newState)
		{
			objectPhysics.ServerSetAnchored(newState, interaction.Performer);
			UpdateSubsystems();
		}

		private void UpdateSubsystems()
		{
			objectPhysics.registerTile.Matrix.TileChangeManager.SubsystemManager.UpdateAt(objectPhysics.OfficialPosition.ToLocalInt(registerObject.Matrix));
		}

		private void UpdateSubsystemsAt(Vector3Int localPos)
		{
			objectPhysics.registerTile.Matrix.TileChangeManager.SubsystemManager.UpdateAt(localPos);
		}

		[Server]
		private void Disassemble(HandApply interaction)
		{
			Spawn.ServerPrefab(matsOnDeconstruct, registerObject.WorldPositionServer, count: countOfMatsOnDissasemle);
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: 1f);
			SoundManager.PlayNetworkedAtPos(soundOnDeconstruct, gameObject.TileWorldPosition().To3Int(), audioSourceParameters, sourceObj: gameObject);
			_ = Despawn.ServerSingle(gameObject);
		}
	}
}

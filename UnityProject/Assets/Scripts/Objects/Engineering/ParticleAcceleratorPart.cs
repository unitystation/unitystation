using System;
using Objects.Construction;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.Events;

namespace Objects.Engineering
{
	public class ParticleAcceleratorPart : MonoBehaviour, ICheckedInteractable<HandApply>, IExaminable, IServerSpawn
	{
		private ParticleAcceleratorState currentState = ParticleAcceleratorState.Frame;
		public ParticleAcceleratorState CurrentState => currentState;

		private RegisterTile registerTile;
		private Integrity integrity;
		private SpriteHandler spriteHandler;
		private WrenchSecurable wrenchSecurable;

		private Directional directional;
		public Directional Directional => directional;

		[SerializeField]
		private ParticleAcceleratorType particleAcceleratorType = ParticleAcceleratorType.Back;

		public ParticleAcceleratorType ParticleAcceleratorType => particleAcceleratorType;

		[SerializeField]
		private int amountOfWiresNeeded = 4;
		private int amountOfWiresUsed;

		[SerializeField]
		private bool shootsBullet;

		public bool ShootsBullet => shootsBullet;

		[SerializeField]
		private bool startSetUp;
		public bool StartSetUp => startSetUp;

		public UnityEvent<ParticleAcceleratorPart> OnShutDown = new UnityEvent<ParticleAcceleratorPart>();

		#region LifeCycle

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			integrity = GetComponent<Integrity>();
			wrenchSecurable = GetComponent<WrenchSecurable>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			directional = GetComponent<Directional>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if (startSetUp)
			{
				ChangeState(ParticleAcceleratorState.Closed);
				amountOfWiresUsed = amountOfWiresNeeded;
				wrenchSecurable.ServerSetPushable(false);
			}
			registerTile.OnLocalPositionChangedServer.AddListener(OnRegisterTileMove);
			integrity.OnWillDestroyServer.AddListener(OnIntegrityDestroy);
		}

		public void OnDisable()
		{
			registerTile.OnLocalPositionChangedServer.RemoveListener(OnRegisterTileMove);
			integrity.OnWillDestroyServer.RemoveListener(OnIntegrityDestroy);
		}

		#endregion

		#region ShutOff

		//Shutdown PA is this part is moved, or if destroyed, sends event to PA controller
		private void ShutDown()
		{
			if(CustomNetworkManager.IsServer == false) return;

			//Should only move during frame state
			if (CurrentState == ParticleAcceleratorState.Frame) return;

			ChangeState(ParticleAcceleratorState.Closed);
			OnShutDown.Invoke(this);
		}

		private void OnIntegrityDestroy(DestructionInfo info)
		{
			ShutDown();
		}

		private void OnRegisterTileMove(Vector3Int newLocalPosition)
		{
			ShutDown();
		}

		#endregion

		#region Interaction

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Cable)) return true;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver)) return true;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wirecutter)) return true;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (wrenchSecurable.IsAnchored == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"{gameObject.ExpensiveName()} must be wrenched down first");
				return;
			}

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Cable) && CurrentState == ParticleAcceleratorState.Frame)
			{
				TryAddCable(interaction);
			}
			else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver))
			{
				TryCloseOpen(interaction);
			}
			else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wirecutter))
			{
				TryRemoveCable(interaction);
			}
		}

		#endregion

		#region Wires and Closing/Opening

		private void TryAddCable(HandApply interaction)
		{
			ToolUtils.ServerUseToolWithActionMessages(interaction, 3,
				$"You start to add wires to the {gameObject.ExpensiveName()}...",
				$"{interaction.Performer.ExpensiveName()} starts to add wires to the {gameObject.ExpensiveName()}...",
				$"You add wires to the {gameObject.ExpensiveName()}.",
				$"{interaction.Performer.ExpensiveName()} adds wires to the {gameObject.ExpensiveName()}'.",
				() =>
				{
					if (interaction.HandObject.TryGetComponent<Stackable>(out var stackable) && stackable != null)
					{
						if (stackable.Amount >= amountOfWiresNeeded - amountOfWiresUsed)
						{
							stackable.ServerConsume(amountOfWiresNeeded - amountOfWiresUsed);
							amountOfWiresUsed = amountOfWiresNeeded;
							ChangeState(ParticleAcceleratorState.Wired);
						}
						else
						{
							amountOfWiresUsed = stackable.Amount;
							stackable.ServerConsume(stackable.Amount);
						}
					}
				});
		}

		private void TryRemoveCable(HandApply interaction)
		{
			if (CurrentState == ParticleAcceleratorState.Frame)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "No wires to be removed");
			}
			else if (CurrentState == ParticleAcceleratorState.Wired)
			{
				//Remove wires
				ToolUtils.ServerUseToolWithActionMessages(interaction, 3,
					$"You start to remove the {gameObject.ExpensiveName()}'s wires...",
					$"{interaction.Performer.ExpensiveName()} starts to remove the {gameObject.ExpensiveName()}'s wires...",
					$"You remove the {gameObject.ExpensiveName()}'s wires.",
					$"{interaction.Performer.ExpensiveName()} removes the {gameObject.ExpensiveName()}'s wires.",
					() =>
					{
						ChangeState(ParticleAcceleratorState.Frame);
						Spawn.ServerPrefab(CommonPrefabs.Instance.SingleCableCoil, registerTile.WorldPositionServer,
							transform.parent, count: amountOfWiresUsed);
					});
			}
			else
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Cover must be opened first");
			}
		}

		private void TryCloseOpen(HandApply interaction)
		{
			if (CurrentState == ParticleAcceleratorState.Frame)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"{amountOfWiresNeeded - amountOfWiresUsed} more wires needed");
			}
			else if (CurrentState == ParticleAcceleratorState.Wired)
			{
				if (amountOfWiresUsed == amountOfWiresNeeded)
				{
					//Screw drive closed
					ToolUtils.ServerUseToolWithActionMessages(interaction, 3,
						$"You start to close the {gameObject.ExpensiveName()}'s cover...",
						$"{interaction.Performer.ExpensiveName()} starts to close the {gameObject.ExpensiveName()}'s cover...",
						$"You close the {gameObject.ExpensiveName()}'s cover.",
						$"{interaction.Performer.ExpensiveName()} closes the {gameObject.ExpensiveName()}'s cover.",
						() =>
						{
							ChangeState(ParticleAcceleratorState.Closed);
						});
				}
				else
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, $"{amountOfWiresNeeded - amountOfWiresUsed} more wires needed");
				}
			}
			else if (CurrentState == ParticleAcceleratorState.Off || CurrentState == ParticleAcceleratorState.Closed)
			{
				//Screw drive open
				ToolUtils.ServerUseToolWithActionMessages(interaction, 3,
					$"You start to open the {gameObject.ExpensiveName()}'s cover...",
					$"{interaction.Performer.ExpensiveName()} starts to open the {gameObject.ExpensiveName()}'s cover...",
					$"You open the {gameObject.ExpensiveName()}'s cover.",
					$"{interaction.Performer.ExpensiveName()} opens the {gameObject.ExpensiveName()}'s cover.",
					() =>
					{
						ChangeState(ParticleAcceleratorState.Wired);
					});
			}
			else
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Turn off the particle accelerator first");
			}
		}

		#endregion

		#region ChangeState

		public void ChangeState(ParticleAcceleratorState newState)
		{
			spriteHandler.ChangeSprite((int)newState);

			wrenchSecurable.blockAnchorChange = newState != ParticleAcceleratorState.Frame;

			currentState = newState;
		}

		#endregion

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			return Directional != null ? $"Is pointing {Directional.CurrentDirection}" : "";
		}
	}

	public enum ParticleAcceleratorState
	{
		//Construction states
		Frame,
		Wired,
		Closed,

		//Power states
		Off,
		On0,
		On1,
		On2,
		On3
	}

	public enum ParticleAcceleratorType
	{
		Controller,
		Back,
		FuelBox,
		PowerBox,
		FrontMiddle,
		FrontLeft,
		FrontRight
	}
}

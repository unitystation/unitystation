using System;
using System.Collections.Generic;
using System.Text;
using Objects.Other;
using ScriptableObjects;
using UnityEngine;
using Weapons;

namespace Systems.Construction
{
	public class TurretFrame : MonoBehaviour, ICheckedInteractable<HandApply>, IExaminable
	{
		[SerializeField] private StatefulState initialState = null;
		[SerializeField] private StatefulState anchoredState = null;
		[SerializeField] private StatefulState metalAddedState = null;
		[SerializeField] private StatefulState wrenchState = null;
		[SerializeField] private StatefulState gunAddedState = null;
		[SerializeField] private StatefulState proxAddedState = null;
		[SerializeField] private StatefulState screwState = null;
		[SerializeField] private StatefulState secondMetalAddedState = null;

		[SerializeField] private GameObject proximityPrefab = null;

		[SerializeField] private GameObject turretPrefab = null;

		[SerializeField] private ItemTrait gunTrait = null;

		private ItemSlot gunSlot;
		private Stateful stateful;
		private StatefulState CurrentState => stateful.CurrentState;
		private UniversalObjectPhysics objectBehaviour;
		private Integrity integrity;

		private void Awake()
		{
			gunSlot = GetComponent<ItemStorage>().GetIndexedItemSlot(0);
			stateful = GetComponent<Stateful>();
			objectBehaviour = GetComponent<UniversalObjectPhysics>();
			integrity = GetComponent<Integrity>();
		}

		private void OnEnable()
		{
			if (CustomNetworkManager.IsServer == false) return;

			integrity.OnWillDestroyServer.AddListener(WhenDestroyed);
		}

		#region Construction/Deconstruction Interactions

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (!Validations.IsTarget(gameObject, interaction)) return false;

			//Anchor or disassemble
			if (CurrentState == initialState)
			{
				return Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench) ||
				       Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar);
			}

			//Adding metal or unanchor
			if (CurrentState == anchoredState)
			{
				return Validations.HasItemTrait(interaction, CommonTraits.Instance.MetalSheet) ||
				       Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench);
			}

			//Wrench or remove metal
			if (CurrentState == metalAddedState)
			{
				return Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench) ||
				       Validations.HasUsedActiveWelder(interaction);
			}

			//Add gun or unwrench
			if (CurrentState == wrenchState)
			{
				return Validations.HasItemTrait(interaction, gunTrait) ||
					Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench);
			}

			//Add prox or remove gun
			if (CurrentState == gunAddedState)
			{
				return Validations.HasItemTrait(interaction, CommonTraits.Instance.ProximitySensor) ||
				       interaction.HandObject == null;
			}

			//Screw or remove prox
			if (CurrentState == proxAddedState)
			{
				return Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver) ||
				       interaction.HandObject == null;
			}

			//Add metal or unscrew
			if (CurrentState == screwState)
			{
				return Validations.HasItemTrait(interaction, CommonTraits.Instance.MetalSheet) ||
				       Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver);
			}

			//Finish construction or remove metal
			if (CurrentState == secondMetalAddedState)
			{
				return Validations.HasUsedActiveWelder(interaction) ||
				       Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar);
			}

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			//Anchor or disassemble
			if (CurrentState == initialState)
			{
				if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench))
				{
					if (!ServerValidations.IsAnchorBlocked(interaction))
					{
						//wrench in place
						ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
							"You start wrenching the turret frame into place...",
							$"{interaction.Performer.ExpensiveName()} starts wrenching the turret frame into place...",
							"You wrench the turret frame into place.",
							$"{interaction.Performer.ExpensiveName()} wrenches the turret frame into place.",
							() =>
							{
								objectBehaviour.ServerSetAnchored(true, interaction.Performer);

								stateful.ServerChangeState(anchoredState);
							});

						return;
					}

					Chat.AddExamineMsgFromServer(interaction.Performer, "Unable to anchor turret frame here");
				}
				else if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar))
				{
					//deconstruct
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start deconstructing the turret frame...",
						$"{interaction.Performer.ExpensiveName()} starts deconstructing the turret frame...",
						"You deconstruct the turret frame.",
						$"{interaction.Performer.ExpensiveName()} deconstructs the turret frame.",
						() =>
						{
							Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, SpawnDestination.At(gameObject), 5);
							_ = Despawn.ServerSingle(gameObject);
						});
				}

				return;
			}

			//Adding metal or unanchor
			if (CurrentState == anchoredState)
			{
				if (Validations.HasItemTrait(interaction, CommonTraits.Instance.MetalSheet))
				{
					//Add metal
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start adding a metal cover to the turret frame...",
						$"{interaction.Performer.ExpensiveName()} starts adding a metal cover to the turret frame...",
						"You add a metal cover to the turret frame.",
						$"{interaction.Performer.ExpensiveName()} adds a metal cover to the turret frame.",
						() =>
						{
							Inventory.ServerConsume(interaction.HandSlot, 1);
							stateful.ServerChangeState(metalAddedState);
						});
				}
				else if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench))
				{
					//Unanchor
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start unwrenching the turret frame from the floor...",
						$"{interaction.Performer.ExpensiveName()} starts unwrenching the turret frame from the floor...",
						"You unwrench the turret frame from the floor.",
						$"{interaction.Performer.ExpensiveName()} unwrenches the turret frame from the floor.",
						() =>
						{
							objectBehaviour.ServerSetAnchored(false, interaction.Performer);

							stateful.ServerChangeState(initialState);
						});
				}

				return;
			}

			//Wrench or remove metal
			if (CurrentState == metalAddedState)
			{
				if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench))
				{
					//Wrench
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start wrenching the bolts on the turret frame...",
						$"{interaction.Performer.ExpensiveName()} starts wrenching the bolts on the turret frame...",
						"You wrench the bolts on the turret frame.",
						$"{interaction.Performer.ExpensiveName()} wrenches the bolts on the turret frame.",
						() =>
						{
							stateful.ServerChangeState(wrenchState);
						});
				}
				else if (Validations.HasUsedActiveWelder(interaction))
				{
					//Remove metal
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start removing the metal cover from the turret frame...",
						$"{interaction.Performer.ExpensiveName()} starts removing the metal cover from the turret frame...",
						"You remove the metal cover from the turret frame.",
						$"{interaction.Performer.ExpensiveName()} removes the metal cover from the turret frame.",
						() =>
						{
							Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, SpawnDestination.At(gameObject), 1);
							stateful.ServerChangeState(anchoredState);
						});
				}

				return;
			}

			//Add gun or unwrench
			if (CurrentState == wrenchState)
			{
				if (Validations.HasItemTrait(interaction, gunTrait))
				{
					//Add energy gun
					Chat.AddActionMsgToChat(interaction, $"You place the {interaction.UsedObject.ExpensiveName()} inside the turret frame.",
						$"{interaction.Performer.ExpensiveName()} places the {interaction.UsedObject.ExpensiveName()} inside the turret frame.");
					Inventory.ServerTransfer(interaction.HandSlot, gunSlot);
					stateful.ServerChangeState(gunAddedState);
				}
				else if  (Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench))
				{
					//Remove unwrench bolts
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start removing the bolts from the turret frame...",
						$"{interaction.Performer.ExpensiveName()} starts removing the bolts from the turret frame...",
						"You remove the bolts from the turret frame.",
						$"{interaction.Performer.ExpensiveName()} removes the bolts from the turret frame.",
						() =>
						{
							stateful.ServerChangeState(metalAddedState);
						});
				}

				return;
			}

			//Add prox or remove gun
			if (CurrentState == gunAddedState)
			{
				if (Validations.HasItemTrait(interaction, CommonTraits.Instance.ProximitySensor))
				{
					//Add proximity sensor
					Chat.AddActionMsgToChat(interaction, $"You place the {interaction.UsedObject.ExpensiveName()} inside the turret frame.",
						$"{interaction.Performer.ExpensiveName()} places the {interaction.UsedObject.ExpensiveName()} inside the turret frame.");
					_ = Despawn.ServerSingle(interaction.UsedObject);
					stateful.ServerChangeState(proxAddedState);
				}
				else if  (interaction.HandObject == null)
				{
					//Remove gun
					Chat.AddActionMsgToChat(interaction, $"You remove the {gunSlot.ItemObject.ExpensiveName()} from the frame.",
						$"{interaction.Performer.ExpensiveName()} removes the {gunSlot.ItemObject.ExpensiveName()} from the frame.");
					Inventory.ServerDrop(gunSlot);

					stateful.ServerChangeState(wrenchState);
				}

				return;
			}

			//Screw or remove prox
			if (CurrentState == proxAddedState)
			{
				if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver))
				{
					//Screw
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start closing the internal hatch of the turret frame...",
						$"{interaction.Performer.ExpensiveName()} starts closing the internal hatch of the turret frame...",
						"You close the internal hatch of the turret frame.",
						$"{interaction.Performer.ExpensiveName()} closes the internal hatch of the turret frame.",
						() =>
						{
							stateful.ServerChangeState(screwState);
						});
				}
				else if  (interaction.HandObject == null)
				{
					//Remove prox
					Chat.AddActionMsgToChat(interaction, $"You remove the {gunSlot.ItemObject.ExpensiveName()} from the frame.",
						$"{interaction.Performer.ExpensiveName()} removes the {gunSlot.ItemObject.ExpensiveName()} from the frame.");
					Spawn.ServerPrefab(proximityPrefab, objectBehaviour.registerTile.WorldPosition, transform.parent);

					stateful.ServerChangeState(gunAddedState);
				}

				return;
			}

			//Add metal or unscrew
			if (CurrentState == screwState)
			{
				if (Validations.HasItemTrait(interaction, CommonTraits.Instance.MetalSheet))
				{
					//Add metal
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start adding a metal cover to the turret frame...",
						$"{interaction.Performer.ExpensiveName()} starts adding a metal cover to the turret frame...",
						"You add a metal cover to the turret frame.",
						$"{interaction.Performer.ExpensiveName()} adds a metal cover to the turret frame.",
						() =>
						{
							Inventory.ServerConsume(interaction.HandSlot, 1);
							stateful.ServerChangeState(secondMetalAddedState);
						});
				}
				else if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver))
				{
					//Unscrew
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start removing the bolts from the turret frame...",
						$"{interaction.Performer.ExpensiveName()} starts removing the bolts from the turret frame...",
						"You remove the bolts from the turret frame.",
						$"{interaction.Performer.ExpensiveName()} removes the bolts from the turret frame.",
						() =>
						{
							stateful.ServerChangeState(proxAddedState);
						});
				}

				return;
			}

			//Finish construction, or remove metal
			if (CurrentState == secondMetalAddedState)
			{
				if (Validations.HasUsedActiveWelder(interaction))
				{
					//Weld to finish turret
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start welding the outer metal cover to the turret frame...",
						$"{interaction.Performer.ExpensiveName()} starts welding the outer metal cover to the turret frame...",
						"You weld the outer metal cover to the turret frame.",
						$"{interaction.Performer.ExpensiveName()} welds the outer metal cover to the turret frame.",
						() =>
						{
							var spawnedTurret = Spawn.ServerPrefab(turretPrefab, objectBehaviour.registerTile.WorldPosition, transform.parent);

							if (spawnedTurret.Successful && spawnedTurret.GameObject.TryGetComponent<Turret>(out var turret))
							{
								turret.SetUpTurret(gunSlot.Item.GetComponent<Gun>(), gunSlot);
							}

							_ = Despawn.ServerSingle(gameObject);
						});
				}
				else if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar))
				{
					//remove metal
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start prying off the outer metal cover from the turret frame...",
						$"{interaction.Performer.ExpensiveName()} starts prying off the outer metal cover from the turret frame...",
						"You pry off the outer metal cover from the turret frame.",
						$"{interaction.Performer.ExpensiveName()} prys off the outer metal cover from the turret frame.",
						() =>
						{
							stateful.ServerChangeState(screwState);
						});
				}
			}
		}

		#endregion

		#region OnDestroy

		private void WhenDestroyed(DestructionInfo info)
		{
			integrity.OnWillDestroyServer.RemoveListener(WhenDestroyed);

			if (gunSlot.IsOccupied)
			{
				Inventory.ServerDrop(gunSlot);
			}

			//1-5
			Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, SpawnDestination.At(gameObject), UnityEngine.Random.Range(1, 6));

			if (CurrentState == initialState) return;

			//0-1
			Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, SpawnDestination.At(gameObject), UnityEngine.Random.Range(0, 2));

			if (CurrentState == metalAddedState) return;

			Spawn.ServerPrefab(proximityPrefab, SpawnDestination.At(gameObject), 1);

			if (CurrentState == proxAddedState) return;

			//0-1
			Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, SpawnDestination.At(gameObject), UnityEngine.Random.Range(0, 2));
		}

		#endregion

		//When a turret gets deconstructed or destroyed spawn and set up frame
		public void SetUp(Pickupable gun)
		{
			if (gun == null) return;

			if (gun.ItemSlot != null)
			{
				gunSlot.ItemStorage.ServerTryTransferFrom(gun.ItemSlot);
				return;
			}

			gunSlot.ItemStorage.ServerTryAdd(gun.gameObject);
		}

		//Examine to help build/deconstruct
		public string Examine(Vector3 worldPos = default(Vector3))
		{
			var newString = new StringBuilder();

			//Anchor or disassemble
			if (CurrentState == initialState)
			{
				newString.AppendLine(
					"Use a wrench to anchor to continue construction, or crowbar to disassemble the frame");
			}

			//Adding metal or unanchor
			if (CurrentState == anchoredState)
			{
				newString.AppendLine("Use a metal sheet to continue construction, or a wrench to unanchor the frame");
			}

			//Wrench or remove metal
			if (CurrentState == metalAddedState)
			{
				newString.AppendLine("Use a wrench to continue construction, or a welder to remove the metal from the frame");
			}

			//Add gun or unwrench
			if (CurrentState == wrenchState)
			{
				newString.AppendLine("Add an energy gun to continue construction, or use a wrench to remove the metal from the frame");
			}

			//Add prox or remove gun
			if (CurrentState == gunAddedState)
			{
				newString.AppendLine("Add an proximity sensor to continue construction, or use your hands to remove the energy gun from the frame");
			}

			//Screw or remove prox
			if (CurrentState == proxAddedState)
			{
				newString.AppendLine("Use a screwdriver to continue construction, or use your hands to remove the proximity sensor from the frame");
			}

			//Add metal or unscrew
			if (CurrentState == screwState)
			{
				newString.AppendLine("Use a metal sheet to continue construction, or use a screwdriver on the frame");
			}

			if (CurrentState == secondMetalAddedState)
			{
				newString.AppendLine("Use welder to finish construction, or use a crowbar to remove the outer cover of the frame");
			}

			return newString.ToString();
		}
	}
}

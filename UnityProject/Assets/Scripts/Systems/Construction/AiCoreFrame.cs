using System.Text;
using Objects.Other;
using Objects.Research;
using ScriptableObjects;
using UnityEngine;
using Weapons;

namespace Systems.Construction
{
	public class AiCoreFrame : MonoBehaviour, ICheckedInteractable<HandApply>, IExaminable
	{
		[SerializeField] private StatefulState initialState = null;
		[SerializeField] private StatefulState anchoredState = null;
		[SerializeField] private StatefulState circuitAddedState = null;
		[SerializeField] private StatefulState screwState = null;
		[SerializeField] private StatefulState wireAddedState = null;
		[SerializeField] private StatefulState brainAddedState = null;
		[SerializeField] private StatefulState glassState = null;

		[SerializeField] private ItemTrait aiCoreCircuitBoardTrait = null;
		[SerializeField] private GameObject aiCoreCircuitBoardPrefab = null;

		[SerializeField] private GameObject aiCorePrefab = null;

		private Stateful stateful;
		private StatefulState CurrentState => stateful.CurrentState;
		private UniversalObjectPhysics objectBehaviour;
		private Integrity integrity;
		private SpriteHandler spriteHandler;

		private void Awake()
		{
			stateful = GetComponent<Stateful>();
			objectBehaviour = GetComponent<UniversalObjectPhysics>();
			integrity = GetComponent<Integrity>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
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
				       Validations.HasUsedActiveWelder(interaction);
			}

			//Adding Circuit board or unanchor
			if (CurrentState == anchoredState)
			{
				return Validations.HasItemTrait(interaction, aiCoreCircuitBoardTrait) ||
				       Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench);
			}

			//Screwdriver or remove Circuit board
			if (CurrentState == circuitAddedState)
			{
				return Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver) ||
				       interaction.HandObject == null;
			}

			//Add wire or unscrew
			if (CurrentState == screwState)
			{
				return Validations.HasItemTrait(interaction, CommonTraits.Instance.Cable) ||
					Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver);
			}

			//Add brain, or skip brain and add reinforced glass or remove wire
			if (CurrentState == wireAddedState)
			{
				//TODO enable brain stuff
				return //Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.MMI / or Positron) ||
				       Validations.HasItemTrait(interaction, CommonTraits.Instance.ReinforcedGlassSheet) ||
				       Validations.HasItemTrait(interaction, CommonTraits.Instance.Wirecutter);
			}

			//Add reinforced glass or remove brain
			if (CurrentState == brainAddedState)
			{
				return Validations.HasItemTrait(interaction, CommonTraits.Instance.ReinforcedGlassSheet) ||
				       Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar);
			}

			//Screw to finish or remove glass
			if (CurrentState == glassState)
			{
				return Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver) ||
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
					if (ServerValidations.IsAnchorBlocked(interaction) == false)
					{
						//wrench in place
						ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
							"You start wrenching the Ai core frame into place...",
							$"{interaction.Performer.ExpensiveName()} starts wrenching the Ai core frame into place...",
							"You wrench the Ai core frame into place.",
							$"{interaction.Performer.ExpensiveName()} wrenches the Ai core frame into place.",
							() =>
							{
								objectBehaviour.ServerSetAnchored(true, interaction.Performer);

								stateful.ServerChangeState(anchoredState);
							});

						return;
					}

					Chat.AddExamineMsgFromServer(interaction.Performer, "Unable to anchor Ai core frame here");
				}
				else if (Validations.HasUsedActiveWelder(interaction))
				{
					//Deconstruct
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start deconstructing the Ai core frame...",
						$"{interaction.Performer.ExpensiveName()} starts deconstructing the Ai core frame...",
						"You deconstruct the Ai core frame.",
						$"{interaction.Performer.ExpensiveName()} deconstructs the Ai core frame.",
						() =>
						{
							Spawn.ServerPrefab(CommonPrefabs.Instance.Plasteel, SpawnDestination.At(gameObject), 5);
							_ = Despawn.ServerSingle(gameObject);
						});
				}

				return;
			}

			//Adding Circuit board or unanchor
			if (CurrentState == anchoredState)
			{
				if (Validations.HasItemTrait(interaction, aiCoreCircuitBoardTrait))
				{
					//Add Circuit board
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start adding the circuit board to the Ai core frame...",
						$"{interaction.Performer.ExpensiveName()} starts adding a circuit board to the Ai core frame...",
						"You add a circuit board to the Ai core frame.",
						$"{interaction.Performer.ExpensiveName()} adds a circuit board to the Ai core frame.",
						() =>
						{
							Inventory.ServerConsume(interaction.HandSlot, 1);
							stateful.ServerChangeState(circuitAddedState);
							spriteHandler.ChangeSprite(1);
						});
				}
				else if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench))
				{
					//Unanchor
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start unwrenching the Ai core frame from the floor...",
						$"{interaction.Performer.ExpensiveName()} starts unwrenching the Ai core frame from the floor...",
						"You unwrench the Ai core frame from the floor.",
						$"{interaction.Performer.ExpensiveName()} unwrenches the Ai core frame from the floor.",
						() =>
						{
							objectBehaviour.ServerSetAnchored(false, interaction.Performer);
							stateful.ServerChangeState(initialState);
						});
				}

				return;
			}

			//Screwdriver or remove Circuit board
			if (CurrentState == circuitAddedState)
			{
				if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver))
				{
					//Screwdriver
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start screwing in the circuit board...",
						$"{interaction.Performer.ExpensiveName()} starts screwing in the circuit board...",
						"You screw in the circuit board.",
						$"{interaction.Performer.ExpensiveName()} screws in the circuit board.",
						() =>
						{
							stateful.ServerChangeState(screwState);
							spriteHandler.ChangeSprite(2);
						});
				}
				else if (interaction.HandObject == null)
				{
					//Remove Circuit board
					Chat.AddActionMsgToChat(interaction, "You remove the circuit board from the frame",
						$"{interaction.Performer.ExpensiveName()} removes the circuit board from the frame");

					Spawn.ServerPrefab(aiCoreCircuitBoardPrefab, SpawnDestination.At(gameObject));
					stateful.ServerChangeState(anchoredState);
					spriteHandler.ChangeSprite(0);
				}

				return;
			}

			//Add wire or unscrew
			if (CurrentState == screwState)
			{
				if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Cable))
				{
					//Add wire
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start adding wire to the Ai core frame...",
						$"{interaction.Performer.ExpensiveName()} starts adding wire to the Ai core frame...",
						"You add wire to the Ai core frame.",
						$"{interaction.Performer.ExpensiveName()} adds wire to the Ai core frame.",
						() =>
						{
							Inventory.ServerConsume(interaction.HandSlot, 1);
							stateful.ServerChangeState(wireAddedState);
							spriteHandler.ChangeSprite(3);
						});
				}
				else if  (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver))
				{
					//Remove unscrew
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start unscrewing in the circuit board...",
						$"{interaction.Performer.ExpensiveName()} starts unscrewing in the circuit board...",
						"You remove unscrew the circuit board.",
						$"{interaction.Performer.ExpensiveName()} unscrews the circuit board.",
						() =>
						{
							stateful.ServerChangeState(circuitAddedState);
							spriteHandler.ChangeSprite(1);
						});
				}

				return;
			}

			//Add brain, or skip brain and add reinforced glass or remove wire
			if (CurrentState == wireAddedState)
			{
				//TODO add brain adding interaction
				// if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.brain))
				// {
				// 	//Add brain
				// 	Chat.AddActionMsgToChat(interaction, $"You place the {interaction.UsedObject.ExpensiveName()} inside the turret frame.",
				// 		$"{interaction.Performer.ExpensiveName()} places the {interaction.UsedObject.ExpensiveName()} inside the turret frame.");
				// 	_ = Despawn.ServerSingle(interaction.UsedObject);
				// 	stateful.ServerChangeState(brainAddedState);

				//	spriteHandler.ChangeSprite(4);
				// }
				/*else */if (Validations.HasItemTrait(interaction, CommonTraits.Instance.ReinforcedGlassSheet))
				{
					//Skip adding brain and add reinforced glass straight away instead
					if (Validations.HasUsedAtLeast(interaction, 2) == false)
					{
						Chat.AddExamineMsgFromServer(interaction.Performer, "You need to use 2 reinforced glass sheets");
						return;
					}

					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start adding the reinforced glass to the front of the Ai core...",
						$"{interaction.Performer.ExpensiveName()} starts add reinforced glass to the front of the Ai core...",
						"You add reinforced glass to the front of the Ai core.",
						$"{interaction.Performer.ExpensiveName()} adds reinforced glass to the front of the Ai core.",
						() =>
						{
							Inventory.ServerConsume(interaction.HandSlot, 2);
							stateful.ServerChangeState(glassState);
							spriteHandler.ChangeSprite(5);
						});
				}
				else if  (Validations.HasItemTrait(interaction, CommonTraits.Instance.Wirecutter))
				{
					//Remove wire
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start cutting out the wire...",
						$"{interaction.Performer.ExpensiveName()} starts cutting out the wire...",
						"You cut out the wire.",
						$"{interaction.Performer.ExpensiveName()} cuts out the wire.",
						() =>
						{
							Spawn.ServerPrefab(CommonPrefabs.Instance.SingleCableCoil, SpawnDestination.At(gameObject), 1);
							stateful.ServerChangeState(screwState);
							spriteHandler.ChangeSprite(2);
						});
				}

				return;
			}

			//Add reinforced glass or remove brain
			if (CurrentState == brainAddedState)
			{
				if (Validations.HasItemTrait(interaction, CommonTraits.Instance.ReinforcedGlassSheet))
				{
					//Add reinforced glass
					if (Validations.HasUsedAtLeast(interaction, 2) == false)
					{
						Chat.AddExamineMsgFromServer(interaction.Performer, "You need to use 2 reinforced glass sheets");
						return;
					}

					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start adding the reinforced glass to the front of the Ai core...",
						$"{interaction.Performer.ExpensiveName()} starts add reinforced glass to the front of the Ai core...",
						"You add reinforced glass to the front of the Ai core.",
						$"{interaction.Performer.ExpensiveName()} adds reinforced glass to the front of the Ai core.",
						() =>
						{
							Inventory.ServerConsume(interaction.HandSlot, 2);
							stateful.ServerChangeState(glassState);
							spriteHandler.ChangeSprite(5);
						});
				}
				else if  (Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar))
				{
					//Remove brain
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start removing the MMI/Positron Brain from the Ai core...",
						$"{interaction.Performer.ExpensiveName()} starts removing the MMI/Positron Brain from the Ai core...",
						"You remove the MMI/Positron Brain from the Ai core.",
						$"{interaction.Performer.ExpensiveName()} removes the MMI/Positron Brain from the Ai core.",
						() =>
						{
							//TODO remove brain logic
							stateful.ServerChangeState(wireAddedState);
							spriteHandler.ChangeSprite(3);
						});
				}

				return;
			}

			//Screw to finish or remove glass
			if (CurrentState == glassState)
			{
				if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver))
				{
					//Finish
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start screwing in the glass to the frame...",
						$"{interaction.Performer.ExpensiveName()} starts screwing in the glass to the frame...",
						"You screw in the glass to the frame.",
						$"{interaction.Performer.ExpensiveName()} screws in the glass to the frame.",
						() =>
						{
							var newCore = Spawn.ServerPrefab(aiCorePrefab, SpawnDestination.At(gameObject), 1);

							if (newCore.Successful)
							{
								//TODO set up ai core when we have brain
								newCore.GameObject.GetComponent<AiVessel>().SetLinkedPlayer(null);
							}

							_ = Despawn.ServerSingle(gameObject);
						});
				}
				else if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar))
				{
					//Crowbar out glass
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start removing the reinforced glass from the Ai core frame...",
						$"{interaction.Performer.ExpensiveName()} starts removing removing the reinforced glass from the Ai core frame...",
						"You remove the reinforced glass from the Ai core frame.",
						$"{interaction.Performer.ExpensiveName()} removes the reinforced glass from the Ai core frame.",
						() =>
						{
							//TODO check for brain
							// if ()
							// {
							// 	stateful.ServerChangeState(brainAddedState);
							//	spriteHandler.ChangeSprite(4);
							// 	return;
							// }

							Spawn.ServerPrefab(CommonPrefabs.Instance.ReinforcedGlassSheet, SpawnDestination.At(gameObject), 2);
							stateful.ServerChangeState(wireAddedState);
							spriteHandler.ChangeSprite(3);
						});
				}
			}
		}

		#endregion

		#region OnDestroy

		private void WhenDestroyed(DestructionInfo info)
		{
			integrity.OnWillDestroyServer.RemoveListener(WhenDestroyed);

			//1-4
			Spawn.ServerPrefab(CommonPrefabs.Instance.Plasteel, SpawnDestination.At(gameObject), UnityEngine.Random.Range(1, 5));

			if (CurrentState == initialState) return;

			//0-1
			Spawn.ServerPrefab(aiCoreCircuitBoardPrefab, SpawnDestination.At(gameObject), UnityEngine.Random.Range(0, 2));

			if (CurrentState == circuitAddedState) return;

			//0-1
			Spawn.ServerPrefab(CommonPrefabs.Instance.SingleCableCoil, SpawnDestination.At(gameObject), UnityEngine.Random.Range(0, 2));

			if (CurrentState == wireAddedState) return;

			//TODO always spawn brain
			//Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, SpawnDestination.At(gameObject), UnityEngine.Random.Range(0, 1));

			if (CurrentState == brainAddedState) return;

			//0-2
			Spawn.ServerPrefab(CommonPrefabs.Instance.ReinforcedGlassSheet, SpawnDestination.At(gameObject), UnityEngine.Random.Range(0, 3));
		}

		#endregion

		public void SetUp()
		{
			stateful.ServerChangeState(glassState);
			spriteHandler.ChangeSprite(5);
		}

		//Examine to help build/deconstruct
		public string Examine(Vector3 worldPos = default(Vector3))
		{
			var newString = new StringBuilder();

			//Anchor or disassemble
			if (CurrentState == initialState)
			{
				newString.AppendLine(
					"Use a wrench to anchor to continue construction, or welder to disassemble the frame");
			}

			//Adding circuit board or unanchor
			if (CurrentState == anchoredState)
			{
				newString.AppendLine("Use a Ai circuit board to continue construction, or a wrench to unanchor the frame");
			}

			//Screw or remove circuit board
			if (CurrentState == circuitAddedState)
			{
				newString.AppendLine("Use a screwdriver to continue construction, or remove the circuit board with your hands");
			}

			//Add wire or unscrew
			if (CurrentState == screwState)
			{
				newString.AppendLine("Add wire to continue construction, or use a screwdriver to unscrew the circuit board");
			}

			//Add brain, or add reinforced glass or remove wire
			if (CurrentState == wireAddedState)
			{
				newString.AppendLine("Add an MMI or add reinforced glass to continue construction, or use wirecutters to cut out the wire from the frame");
			}

			//Add reinforced glass or remove brain
			if (CurrentState == brainAddedState)
			{
				newString.AppendLine("Add reinforced glass to continue construction, or use a crowbar to remove the MMI from the frame");
			}

			//Screw to finish or remove reinforce glass
			if (CurrentState == glassState)
			{
				newString.AppendLine("Use screwdriver to finish construction, or use a crowbar to remove the glass in the frame");
			}

			return newString.ToString();
		}
	}
}

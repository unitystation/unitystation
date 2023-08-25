using UnityEngine;
using Mirror;
using Doors;
using Items.Construction;
using ScriptableObjects;
using Core.Editor.Attributes;
using Systems.Clearance;

namespace Objects.Construction
{
	public class AirlockAssembly : NetworkBehaviour, ICheckedInteractable<HandApply>, IExaminable
	{
		[SerializeField ]
		[Tooltip("Game object which represents the fill layer of this airlock")]
		private GameObject overlayFill = null;

		[SerializeField ]
		[Tooltip("Game object which represents the hacking panel layer for this airlock")]
		private GameObject overlayHacking = null;

		[Tooltip("Airlock to spawn.")]
		[SerializeField]
		private GameObject airlockToSpawn = null;
		public GameObject AirlockToSpawn => airlockToSpawn;

		[Tooltip("Airlock windowed to spawn.")]
		[SerializeField]
		private GameObject airlockWindowedToSpawn = null;
		public GameObject AirlockWindowedToSpawn => airlockWindowedToSpawn;

		[Tooltip("Material of which the airlock is made")]
		[SerializeField]
		private GameObject airlockMaterial = null;

		[SerializeField] private StatefulState initialState = null;
		[SerializeField] private StatefulState wrenchedState = null;
		[SerializeField] private StatefulState cablesAddedState = null;
		[SerializeField] private StatefulState electronicsAddedState = null;

		private ItemSlot airlockElectronicsSlot;
		private Stateful stateful;

		private SpriteHandler overlayFillHandler;
		private SpriteHandler overlayHackingHandler;
		private StatefulState CurrentState => stateful.CurrentState;
		private UniversalObjectPhysics objectBehaviour;
		private Integrity integrity;

		private bool glassAdded = false;

		private void Awake()
		{
			airlockElectronicsSlot = GetComponent<ItemStorage>().GetIndexedItemSlot(0);
			stateful = GetComponent<Stateful>();
			objectBehaviour = GetComponent<UniversalObjectPhysics>();

			overlayFillHandler = overlayFill.GetComponent<SpriteHandler>();
			overlayHackingHandler = overlayHacking.GetComponent<SpriteHandler>();

			if (!CustomNetworkManager.IsServer) return;

			integrity = GetComponent<Integrity>();

			integrity.OnWillDestroyServer.AddListener(WhenDestroyed);
		}

		/// <summary>
		/// Client Side interaction
		/// </summary>
		/// <param name="interaction"></param>
		/// <param name="side"></param>
		/// <returns></returns>
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (!Validations.IsTarget(gameObject, interaction)) return false;

			//different logic depending on state
			if (CurrentState == initialState)
			{
				//wrench the airlock or deconstruct
				return Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench) ||
					  Validations.HasUsedActiveWelder(interaction);
			}
			else if (CurrentState == wrenchedState)
			{
				//add 1 cables or unwrench the airlock
				return (Validations.HasItemTrait(interaction, CommonTraits.Instance.Cable) && Validations.HasUsedAtLeast(interaction, 1)) ||
					Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench) || (Validations.HasItemTrait(interaction, CommonTraits.Instance.GlassSheet) &&
					Validations.HasUsedAtLeast(interaction, 1) && !glassAdded && airlockWindowedToSpawn);
			}
			else if (CurrentState == cablesAddedState)
			{
				//add airlock electronics or cut cables
				return Validations.HasItemTrait(interaction, CommonTraits.Instance.Wirecutter) ||
					Validations.HasUsedComponent<AirlockElectronics>(interaction);
			}
			else if (CurrentState == electronicsAddedState)
			{
				//screw in or pry off airlock electronics
				return Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver) ||
					   Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar);
			}

			return false;
		}

		/// <summary>
		/// What the server does if the interaction is valid on client
		/// </summary>
		/// <param name="interaction"></param>
		public void ServerPerformInteraction(HandApply interaction)
		{
			if (CurrentState == initialState)
			{
				InitialStateInteraction(interaction);
			}
			else if (CurrentState == wrenchedState)
			{
				WrenchedStateInteraction(interaction);
			}
			else if (CurrentState == cablesAddedState)
			{
				CablesAddedStateInteraction(interaction);
			}
			else if (CurrentState == electronicsAddedState)
			{
				ElectronicsAddedStateInteraction(interaction);
			}
		}

		/// <summary>
		/// Stage 1, Wrench down to continue construction, or welder to destroy airlock.
		/// </summary>
		/// <param name="interaction"></param>
		private void InitialStateInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench))
			{
				if (!ServerValidations.IsAnchorBlocked(interaction))
				{
					//wrench in place
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start to secure the airlock assembly to the floor...",
						$"{interaction.Performer.ExpensiveName()} starts to secure the airlock assembly to the floor...",
						"You secure the airlock assembly.",
						$"{interaction.Performer.ExpensiveName()} secures the airlock assembly to the floor.",
						() => objectBehaviour.ServerSetAnchored(true, interaction.Performer));
					stateful.ServerChangeState(wrenchedState);
				}
				else
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, "Unable to secure airlock assembly");
				}
			}
			else if (Validations.HasUsedActiveWelder(interaction))
			{
				//deconsruct, spawn 4 metals
				ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
					"You start to disassemble the airlock assembly...",
					$"{interaction.Performer.ExpensiveName()} starts to disassemble the airlock assembly...",
					"You disassemble the airlock assembly.",
					$"{interaction.Performer.ExpensiveName()} disassembles the airlock assembly.",
					() =>
					{
						Spawn.ServerPrefab(airlockMaterial, SpawnDestination.At(gameObject), 4);
						_ = Despawn.ServerSingle(gameObject);
					});
			}
		}

		/// <summary>
		/// Stage 2, Add cable to continue, or use wrench to go back to stage 1.
		/// </summary>
		/// <param name="interaction"></param>
		private void WrenchedStateInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Cable) &&
									 Validations.HasUsedAtLeast(interaction, 1))
			{
				//add 1 cable
				ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
					"You start adding cables to the airlock assembly...",
					$"{interaction.Performer.ExpensiveName()} starts adding cables to the airlock assembly...",
					"You add cables to the airlock.",
					$"{interaction.Performer.ExpensiveName()} adds cables to the airlock assembly.",
					() =>
					{
						Inventory.ServerConsume(interaction.HandSlot, 1);
						stateful.ServerChangeState(cablesAddedState);
						overlayHackingHandler.ChangeSprite((int)Panel.WiresAdded);
					});
			}
			else if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench))
			{
				//unwrench
				ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
					"You start to unsecure the airlock assembly from the floor...",
					$"{interaction.Performer.ExpensiveName()} starts to unsecure the airlock assembly from the floor...",
					"You unsecure the airlock assembly.",
					$"{interaction.Performer.ExpensiveName()} unsecures the airlock assembly from the floor.",
					() => objectBehaviour.ServerSetAnchored(false, interaction.Performer));
				stateful.ServerChangeState(initialState);
			}
			else if (Validations.HasItemTrait(interaction, CommonTraits.Instance.GlassSheet) &&
						 Validations.HasUsedAtLeast(interaction, 1) && !glassAdded && airlockWindowedToSpawn)
			{
				//add glass
				ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
					"You start to install glass into the airlock assembly...",
					$"{interaction.Performer.ExpensiveName()} starts to install glass into the airlock assembly...",
					"You install glass windows into the airlock assembly.",
					$"{interaction.Performer.ExpensiveName()} installs glass windows into the airlock assembly.",
					() =>
					{
						Inventory.ServerConsume(interaction.HandSlot, 1);
						overlayFillHandler.ChangeSprite((int)Fill.GlassFill);
						glassAdded = true;
					});
			}
		}

		/// <summary>
		/// Stage 3, add airlock electronics which contains data of access, or use wirecutters to move to stage 2
		/// </summary>
		/// <param name="interaction"></param>
		private void CablesAddedStateInteraction(HandApply interaction)
		{
			if (Validations.HasUsedComponent<AirlockElectronics>(interaction) && airlockElectronicsSlot.IsEmpty)
			{
				ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
					"You start to install electronics into the airlock assembly...",
					$"{interaction.Performer.ExpensiveName()} starts to install the electronics into the airlock assembly...",
					"You install the airlock electronics.",
					$"{interaction.Performer.ExpensiveName()} installs the electronics into the airlock assembly.",
					() =>
					{
						if(Inventory.ServerTransfer(interaction.HandSlot, airlockElectronicsSlot))
						{
							stateful.ServerChangeState(electronicsAddedState);
							overlayHackingHandler.ChangeSprite((int)Panel.ElectronicsAdded);
						}
					});
			}
			else if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Wirecutter))
			{
				//cut out cables
				ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
					"You start to cut the cables from the airlock assembly...",
					$"{interaction.Performer.ExpensiveName()} starts to cut the cables from the airlock assembly...",
					"You cut the cables from the airlock assembly.",
					$"{interaction.Performer.ExpensiveName()} cuts the cables from the airlock assembly.",
					() =>
					{
						Spawn.ServerPrefab(CommonPrefabs.Instance.SingleCableCoil, SpawnDestination.At(gameObject));
						stateful.ServerChangeState(wrenchedState);
						overlayHackingHandler.ChangeSprite((int)Panel.EmptyPanel);
					});
			}
		}

		/// <summary>
		/// Stage 4, screw in the airlock electronics to finish, or crowbar the electronics out to go back to stage 3.
		/// </summary>
		/// <param name="interaction"></param>
		private void ElectronicsAddedStateInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver) && airlockElectronicsSlot.IsOccupied)
			{
				//screw in the airlock electronics
				ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
					$"You start to screw the {airlockElectronicsSlot.ItemObject.ExpensiveName()} into place...",
					$"{interaction.Performer.ExpensiveName()} starts to screw the {airlockElectronicsSlot.ItemObject.ExpensiveName()} into place...",
					"You finish the airlock.",
					$"{interaction.Performer.ExpensiveName()} finishes the airlock.",
					() =>
					{
						if (glassAdded && airlockWindowedToSpawn)
						{
							ServerSpawnAirlock(airlockWindowedToSpawn);
						}
						else
						{
							ServerSpawnAirlock(airlockToSpawn);
						}
						_ = Despawn.ServerSingle(gameObject);
					});
			}
			else if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar) && airlockElectronicsSlot.IsOccupied)
			{
				//Crowbar the electronics out
				ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
					"You start to remove electronics from the airlock assembly...",
					$"{interaction.Performer.ExpensiveName()} starts to remove electronics from the airlock assembly...",
					"You remove the airlock electronics from the airlock assembly.",
					$"{interaction.Performer.ExpensiveName()} removes the electronics from the airlock assembly.",
					() =>
					{
						Inventory.ServerDrop(airlockElectronicsSlot);
						stateful.ServerChangeState(cablesAddedState);
						overlayHackingHandler.ChangeSprite((int)Panel.WiresAdded);
					});
			}
		}

		/// <summary>
		/// Spawn airlock and add access from airlock Electronics.
		/// </summary>
		/// <param name="airlockPrefab"></param>
		private void ServerSpawnAirlock(GameObject airlockPrefab)
		{
			var airlock = Spawn.ServerPrefab(airlockPrefab, SpawnDestination.At(gameObject)).GameObject;
			if (airlockElectronicsSlot.IsOccupied)
			{
				AccessRestrictions airlockAccess = airlock.GetComponentInChildren<AccessRestrictions>();
				GameObject airlockElectronics = airlockElectronicsSlot.ItemObject;
				AirlockElectronics electronics = airlockElectronics.GetComponent<AirlockElectronics>();
				airlockAccess.clearanceRestriction = electronics.CurrentClearance;
			}
		}

		public string Examine(Vector3 worldPos)
		{
			string msg = "";
			if (CurrentState == initialState)
			{
				msg = "Use a wrench to secure the airlock assembly to the floor, or a welder to deconstruct it.\n";
			}

			if (CurrentState == wrenchedState)
			{
				msg = "Add one wire to continue construction or use wrench to unsecure the airlock assembly.\n";
				if (!glassAdded)
				{
					msg += "You can add glass to make a window in the airlock.\n";
				}
			}

			if (CurrentState == cablesAddedState)
			{
				msg = "Add the airlock electronics to continue construction, or use a wirecutter to remove cables.\n";
			}

			if (CurrentState == electronicsAddedState)
			{
				msg = "Use a screwdriver to finish construction or use crowbar to remove the airlock electronics.\n";
			}

			return msg;
		}

		public void WhenDestroyed(DestructionInfo info)
		{
			if (airlockElectronicsSlot.IsOccupied)
			{
				Inventory.ServerDrop(airlockElectronicsSlot);
			}

			if (glassAdded)
			{
				Spawn.ServerPrefab(CommonPrefabs.Instance.GlassShard, SpawnDestination.At(gameObject));
			}

			if (CurrentState != wrenchedState && CurrentState != initialState)
			{
				//0-1
				Spawn.ServerPrefab(CommonPrefabs.Instance.SingleCableCoil, SpawnDestination.At(gameObject), UnityEngine.Random.Range(0, 2));
			}

			//1-3
			Spawn.ServerPrefab(airlockMaterial, SpawnDestination.At(gameObject), UnityEngine.Random.Range(1, 4));

			integrity.OnWillDestroyServer.RemoveListener(WhenDestroyed);
		}

		/// <summary>
		/// Creating an airlock assembly from a deconstructed airlock.
		/// </summary>
		/// <param name="airlockElectronicsPrefab">prefab to create airlock electronics</param>
		/// <param name="airlockClearance">clearance for installation in the airlock electronics</param>
		/// <param name="isWindowed">add glass or not</param>
		public void ServerInitFromComputer(GameObject airlockElectronicsPrefab, Clearance airlockClearance, bool isWindowed)
		{
			//create the airlock electronics
			var airlockElectronics = Spawn.ServerPrefab(airlockElectronicsPrefab, SpawnDestination.At(gameObject)).GameObject;
			airlockElectronics.GetComponent<AirlockElectronics>().CurrentClearance = airlockClearance;

			objectBehaviour.SetIsNotPushable(true);
			stateful.ServerChangeState(cablesAddedState);
			overlayHackingHandler.ChangeSprite((int)Panel.WiresAdded);
			if (isWindowed)
			{
				overlayFillHandler.ChangeSprite((int)Fill.GlassFill);
				glassAdded = true;
			}
		}

		public enum Panel
		{
			NoPanel,
			EmptyPanel,
			WiresAdded,
			ElectronicsAdded,
		}
		public enum Fill
		{
			NoFill,
			MetalFill,
			GlassFill,
		}
	}
}
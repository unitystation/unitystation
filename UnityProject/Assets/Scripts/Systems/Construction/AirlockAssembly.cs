using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Items.Construction;
using ScriptableObjects;
using Core.Editor.Attributes;

namespace Objects.Construction
{
	public class AirlockAssembly : NetworkBehaviour, ICheckedInteractable<HandApply>, IExaminable
	{
		[SerializeField, PrefabModeOnly]
		[Tooltip("Game object which represents the fill layer of this door")]
		private GameObject overlayFill = null;

		[SerializeField, PrefabModeOnly]
		[Tooltip("Game object which represents the hacking panel layer for this door")]
		private GameObject overlayHacking = null;

		[SerializeField] private StatefulState initialState = null;
		[SerializeField] private StatefulState wrenchedState = null;
		[SerializeField] private StatefulState cablesAddedState = null;
		[SerializeField] private StatefulState electronicsAddedState = null;

		private ItemSlot airlockElectronicsSlot;
		private Stateful stateful;

		private SpriteHandler overlayFillHandler;
		private SpriteHandler overlayHackingHandler;
		private StatefulState CurrentState => stateful.CurrentState;
		private ObjectBehaviour objectBehaviour;
		private Integrity integrity;

		private void Awake()
		{
			airlockElectronicsSlot = GetComponent<ItemStorage>().GetIndexedItemSlot(0);
			stateful = GetComponent<Stateful>();
			objectBehaviour = GetComponent<ObjectBehaviour>();

			overlayFillHandler = overlayFill.GetComponent<SpriteHandler>();
			overlayHackingHandler = overlayHacking.GetComponent<SpriteHandler>();

			if (!CustomNetworkManager.IsServer) return;

			integrity = GetComponent<Integrity>();

			//integrity.OnWillDestroyServer.AddListener(WhenDestroyed);
		}

		/// <summary>
		/// Client Side interaction
		/// </summary>
		/// <param name="interaction"></param>
		/// <param name="side"></param>
		/// <returns></returns>
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			if (!Validations.IsTarget(gameObject, interaction)) return false;

			//different logic depending on state
			if (CurrentState == initialState)
			{
				//wrench the airlock or deconstruct
				return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench) ||
					  Validations.HasUsedActiveWelder(interaction);
			}
			else if (CurrentState == wrenchedState)
			{
				//add 9 cables or unwrench the airlock
				return (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Cable) && Validations.HasUsedAtLeast(interaction, 9)) ||
					Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench);
			}
			else if (CurrentState == cablesAddedState)
			{
				//add airlock electronics or cut cables
				return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wirecutter) ||
					Validations.HasUsedComponent<AirlockElectronics>(interaction);
			}
			else if (CurrentState == electronicsAddedState)
			{
				//screw in or pry off airlock electronics
				return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver) ||
					   Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Crowbar);
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
			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench))
			{
				if (!ServerValidations.IsAnchorBlocked(interaction))
				{
					//wrench in place
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start wrenching the airlock into place...",
						$"{interaction.Performer.ExpensiveName()} starts wrenching the airlock into place...",
						"You wrench the airlock into place.",
						$"{interaction.Performer.ExpensiveName()} wrenches the airlock into place.",
						() => objectBehaviour.ServerSetAnchored(true, interaction.Performer));
					stateful.ServerChangeState(wrenchedState);
				}
				else
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, "Unable to wrench frame");
				}
			}
			else if (Validations.HasUsedActiveWelder(interaction))
			{
				//deconsruct, spawn 5 metals
				ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
					"You start deconstructing the frame...",
					$"{interaction.Performer.ExpensiveName()} starts deconstructing the frame...",
					"You deconstruct the frame.",
					$"{interaction.Performer.ExpensiveName()} deconstructs the frame.",
					() =>
					{
						Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, SpawnDestination.At(gameObject), 5);
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
			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Cable) &&
									 Validations.HasUsedAtLeast(interaction, 9))
			{
				//add 5 cables
				ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
					"You start adding cables to the airlock...",
					$"{interaction.Performer.ExpensiveName()} starts adding cables to the airlock...",
					"You add cables to the airlock.",
					$"{interaction.Performer.ExpensiveName()} adds cables to the airlock.",
					() =>
					{
						Inventory.ServerConsume(interaction.HandSlot, 9);
						stateful.ServerChangeState(cablesAddedState);
						overlayHackingHandler.ChangeSprite((int)Panel.WiresAdded);
					});
			}
			else if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench))
			{
				//unwrench
				ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
					"You start to unfasten the airlock...",
					$"{interaction.Performer.ExpensiveName()} starts to unfasten the airlock...",
					"You unfasten the airlock.",
					$"{interaction.Performer.ExpensiveName()} unfastens the airlock.",
					() => objectBehaviour.ServerSetAnchored(false, interaction.Performer));
				stateful.ServerChangeState(initialState);
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
					"You start adding electronics to the airlock...",
					$"{interaction.Performer.ExpensiveName()} starts adding electronics to the airlock...",
					"You add electronics to the airlock.",
					$"{interaction.Performer.ExpensiveName()} adds electronics to the airlock.",
					() =>
					{
						if(Inventory.ServerTransfer(interaction.HandSlot, airlockElectronicsSlot))
						{
							stateful.ServerChangeState(electronicsAddedState);
							overlayHackingHandler.ChangeSprite((int)Panel.ElectronicsAdded);
						}
					});
			}
			else if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wirecutter))
			{
				//cut out cables
				Chat.AddActionMsgToChat(interaction, $"You remove the cables.",
					$"{interaction.Performer.ExpensiveName()} removes the cables.");
				ToolUtils.ServerPlayToolSound(interaction);
				Spawn.ServerPrefab(CommonPrefabs.Instance.SingleCableCoil, SpawnDestination.At(gameObject), 9);
				stateful.ServerChangeState(wrenchedState);
				overlayHackingHandler.ChangeSprite((int)Panel.EmptyPanel);
			}
		}

		/// <summary>
		/// Stage 4, screw in the airlock electronics to finish, or crowbar the electronics out to go back to stage 3.
		/// </summary>
		/// <param name="interaction"></param>
		private void ElectronicsAddedStateInteraction(HandApply interaction)
		{
			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver) && airlockElectronicsSlot.IsOccupied)
			{
				//screw in the airlock electronics
				Chat.AddActionMsgToChat(interaction, $"You screw {airlockElectronicsSlot.ItemObject.ExpensiveName()} into place.",
					$"{interaction.Performer.ExpensiveName()} screws {airlockElectronicsSlot.ItemObject.ExpensiveName()} into place.");
				ToolUtils.ServerPlayToolSound(interaction);
			}
			else if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Crowbar) && airlockElectronicsSlot.IsOccupied)
			{
				//Crowbar the electronics out
				Chat.AddActionMsgToChat(interaction, $"You remove the {airlockElectronicsSlot.ItemObject.ExpensiveName()} from the airlock.",
					$"{interaction.Performer.ExpensiveName()} removes the {airlockElectronicsSlot.ItemObject.ExpensiveName()} from the airlcok.");
				ToolUtils.ServerPlayToolSound(interaction);
				Inventory.ServerDrop(airlockElectronicsSlot);
				stateful.ServerChangeState(cablesAddedState);
				overlayHackingHandler.ChangeSprite((int)Panel.WiresAdded);
			}
		}

		public string Examine(Vector3 worldPos)
		{
			string msg = "";
			if (CurrentState == initialState)
			{
				msg = "Use a wrench to secure the airlock to the floor, or a welder to deconstruct it.\n";
			}

			if (CurrentState == wrenchedState)
			{
				msg = " Add 9 wires to continue construction, or use wrench to unfasten the airlock.\n";
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
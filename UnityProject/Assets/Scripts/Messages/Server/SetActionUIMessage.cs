using System;
using Mirror;
using UnityEngine;
using Systems.Spells;
using ScriptableObjects.Systems.Spells;
using UI.Action;

namespace Messages.Server
{
	/// <summary>
	/// ues to set ActionUI For a client
	/// </summary>
	public class SetActionUIMessage : ServerMessage<SetActionUIMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public ushort actionListID;
			public short spellListIndex;
			public int SpriteLocation;
			public int ComponentLocation;
			public uint NetObject;
			public bool showAlert;
			public float cooldown;
			public ushort ComponentID;
			public UpdateType ProposedAction;
		}

		public override void Process(NetMessage msg)
		{
			IActionGUI action = null;
			if (msg.actionListID != 0)
			{
				//SO action singleton ID
				action = UIActionSOSingleton.Instance.FromID(msg.actionListID);
			}
			else if (msg.spellListIndex >= 0)
			{
				//SpellList singleton index
				var spellData = SpellList.Instance.FromIndex(msg.spellListIndex);

				if (UIActionManager.HasActionData(spellData, out action) == false)
				{
					// no need to instantiate a spell if server asks to hide one anyway
					if (msg.ProposedAction == UpdateType.StateChange && msg.showAlert == false) return;

					action = spellData.AddToPlayer(PlayerManager.LocalPlayerScript);
				}
			}
			else
			{
				// Action pre-placed on a networked object
				LoadNetworkObject(msg.NetObject);
				var actions = NetworkObject.GetComponentsInChildren(DeserializeType(msg.ComponentID));
				if ((actions.Length > msg.ComponentLocation))
				{
					action = (actions[msg.ComponentLocation] as IActionGUI);
				}
			}

			if (action != null)
			{
				switch (msg.ProposedAction)
				{
					case UpdateType.FrontIcon:
						UIActionManager.SetSprite(action, msg.SpriteLocation);
						break;
					case UpdateType.BackgroundIcon:
						UIActionManager.SetBackground(action, msg.SpriteLocation);
						break;
					case UpdateType.StateChange:
						UIActionManager.ToggleClient(action, msg.showAlert);
						break;
					case UpdateType.Cooldown:
						UIActionManager.SetCooldownLocal(action, msg.cooldown);
						break;
				}

				return;
			}

			IActionGUIMulti actionMulti = null;

			// Action pre-placed on a networked object
			LoadNetworkObject(msg.NetObject);
			var actionsOther = NetworkObject.GetComponentsInChildren(DeserializeType(msg.ComponentID));
			if ((actionsOther.Length > msg.ComponentLocation))
			{
				actionMulti = (actionsOther[msg.ComponentLocation] as IActionGUIMulti);
			}

			if(actionMulti == null) return;

			var actionData = actionMulti.ActionData[msg.spellListIndex];

			switch (msg.ProposedAction)
			{
				case UpdateType.FrontIcon:
					UIActionManager.SetSprite(actionMulti, actionData, msg.SpriteLocation);
					break;
				case UpdateType.BackgroundIcon:
					UIActionManager.SetBackground(actionMulti, actionData, msg.SpriteLocation);
					break;
				case UpdateType.StateChange:
					UIActionManager.ToggleServer(actionMulti, actionData, msg.showAlert);
					break;
				case UpdateType.Cooldown:
					UIActionManager.SetCooldownLocal(actionMulti, actionData, msg.cooldown);
					break;
			}
		}

		private static NetMessage _Send(GameObject recipient,
			IActionGUI action,
			UpdateType ProposedAction,
			bool show = false,
			float cooldown = 0,
			int location = 0)
		{
			// SO action singleton ID
			if (action is UIActionScriptableObject actionFromSO)
			{
				NetMessage msg = new NetMessage
				{
					actionListID = UIActionSOSingleton.ActionsTOID[actionFromSO],
					showAlert = show,
					cooldown = cooldown,
					SpriteLocation = location,
					ProposedAction = ProposedAction,
					ComponentID = SerializeType(actionFromSO.GetType()),
					spellListIndex = -1
				};
				SendTo(recipient, msg);
				return msg;
			}
			// SpellList singleton index
			else if (action is Spell spellAction)
			{
				NetMessage msg = new NetMessage
				{
					spellListIndex = spellAction.SpellData.Index,
					showAlert = show,
					cooldown = cooldown,
					SpriteLocation = location,
					ProposedAction = ProposedAction,
					ComponentID = SerializeType(spellAction.GetType()),
				};
				SendTo(recipient, msg);
				return msg;
			}
			else
			{
				// Action pre-placed on a networked object
				var netObject = (action as Component).GetComponent<NetworkIdentity>();
				var type = action.GetType();
				var foundActions = netObject.GetComponentsInChildren(type);
				var componentLocation = 0;
				bool isFound = false;
				foreach (var foundAction in foundActions)
				{
					if ((foundAction as IActionGUI) == action)
					{
						isFound = true;
						break;
					}

					componentLocation++;
				}

				if (isFound)
				{
					NetMessage msg = new NetMessage
					{
						NetObject = netObject.netId,
						ComponentLocation = componentLocation,
						ComponentID = SerializeType(type),
						cooldown = cooldown,
						showAlert = show,
						SpriteLocation = location,
						ProposedAction = ProposedAction,
						spellListIndex = -1
					};
					SendTo(recipient, msg);
					return msg;
				}
				else
				{
					Logger.LogError("Failed to find IActionGUI on NetworkIdentity", Category.UserInput);
				}
			}

			return new NetMessage();
		}

		public static NetMessage SetAction(GameObject recipient, IActionGUI iServerActionGUI, bool _showAlert)
		{
			return _Send(recipient, iServerActionGUI, UpdateType.StateChange, _showAlert);
		}

		public static NetMessage SetAction(GameObject recipient, IActionGUI iServerActionGUI, float cooldown)
		{
			return _Send(recipient, iServerActionGUI, UpdateType.Cooldown, cooldown: cooldown);
		}

		public static NetMessage SetSprite(GameObject recipient, IActionGUI iServerActionGUI, int FrontIconlocation)
		{
			return _Send(recipient, iServerActionGUI, UpdateType.FrontIcon, location: FrontIconlocation);
		}

		public static NetMessage SetBackgroundSprite(GameObject recipient, IActionGUI iServerActionGUI,
			int FrontIconlocation)
		{
			return _Send(recipient, iServerActionGUI, UpdateType.BackgroundIcon, location: FrontIconlocation);
		}

		public static NetMessage SetAction(GameObject recipient, IActionGUIMulti iServerActionGUIMulti, ActionData actionChosen, bool _showAlert)
		{
			return _Send(recipient, iServerActionGUIMulti, actionChosen, UpdateType.StateChange, _showAlert);
		}

		public static NetMessage SetAction(GameObject recipient, IActionGUIMulti iServerActionGUIMulti, ActionData actionChosen, float cooldown)
		{
			return _Send(recipient, iServerActionGUIMulti, actionChosen, UpdateType.Cooldown, cooldown: cooldown);
		}

		private static NetMessage _Send(GameObject recipient,
			IActionGUIMulti action,
			ActionData actionChosen,
			UpdateType ProposedAction,
			bool show = false,
			float cooldown = 0,
			int location = 0)
		{
			// SO action singleton ID
			if (action is UIActionScriptableObject actionFromSO)
			{
				NetMessage msg = new NetMessage
				{
					actionListID = UIActionSOSingleton.ActionsTOID[actionFromSO],
					showAlert = show,
					cooldown = cooldown,
					SpriteLocation = location,
					ProposedAction = ProposedAction,
					ComponentID = SerializeType(actionFromSO.GetType()),
					spellListIndex = RequestGameAction.FindIndex(action, actionChosen)
				};
				SendTo(recipient, msg);
				return msg;
			}
			// SpellList singleton index
			else if (action is Spell spellAction)
			{
				NetMessage msg = new NetMessage
				{
					spellListIndex = spellAction.SpellData.Index,
					showAlert = show,
					cooldown = cooldown,
					SpriteLocation = location,
					ProposedAction = ProposedAction,
					ComponentID = SerializeType(spellAction.GetType()),
				};
				SendTo(recipient, msg);
				return msg;
			}
			else
			{
				// Action pre-placed on a networked object
				var netObject = (action as Component).GetComponent<NetworkIdentity>();
				var type = action.GetType();
				var foundActions = netObject.GetComponentsInChildren(type);
				var componentLocation = 0;
				bool isFound = false;
				foreach (var foundAction in foundActions)
				{
					if ((foundAction as IActionGUI) == action)
					{
						isFound = true;
						break;
					}

					componentLocation++;
				}

				if (isFound)
				{
					NetMessage msg = new NetMessage
					{
						NetObject = netObject.netId,
						ComponentLocation = componentLocation,
						ComponentID = SerializeType(type),
						cooldown = cooldown,
						showAlert = show,
						SpriteLocation = location,
						ProposedAction = ProposedAction,
						spellListIndex = RequestGameAction.FindIndex(action, actionChosen)
					};
					SendTo(recipient, msg);
					return msg;
				}
				else
				{
					Logger.LogError("Failed to find IActionGUI on NetworkIdentity", Category.UserInput);
				}
			}

			return new NetMessage();
		}

		private static ushort SerializeType(Type type)
		{
			return RequestGameAction.componentTypeToComponentID[type];
		}

		private static Type DeserializeType(ushort id)
		{
			return RequestGameAction.componentIDToComponentType[id];
		}

		public enum UpdateType
		{
			StateChange,
			BackgroundIcon,
			FrontIcon,
			Cooldown,
		}
	}
}

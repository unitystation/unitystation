using System;
using Mirror;
using UnityEngine;
using Systems.Spells;
using ScriptableObjects.Systems.Spells;

/// <summary>
/// ues to set ActionUI For a client
/// </summary>
public class SetActionUIMessage : ServerMessage
{
	public class SetActionUIMessageNetMessage : NetworkMessage
	{
		public ushort actionListID;
		public short spellListIndex = -1;
		public int SpriteLocation;
		public int ComponentLocation;
		public uint NetObject;
		public bool showAlert;
		public float cooldown;
		public ushort ComponentID;
		public UpdateType ProposedAction;
	}

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as SetActionUIMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		IActionGUI action = null;
		if (newMsg.actionListID != 0)
		{
			//SO action singleton ID
			action = UIActionSOSingleton.Instance.FromID(newMsg.actionListID);
		}
		else if (newMsg.spellListIndex >= 0)
		{
			//SpellList singleton index
			var spellData = SpellList.Instance.FromIndex(newMsg.spellListIndex);

			if (!UIActionManager.HasActionData(spellData, out action))
			{
				if (newMsg.ProposedAction == UpdateType.StateChange && newMsg.showAlert == false)
				{ //no need to instantiate a spell if server asks to hide one anyway
					return;
				}

				action = spellData.AddToPlayer(PlayerManager.LocalPlayerScript);
			}
		}
		else
		{
			//Action pre-placed on a networked object
			LoadNetworkObject(newMsg.NetObject);
			var actions = NetworkObject.GetComponentsInChildren(DeserializeType(newMsg.ComponentID));
			if ((actions.Length > newMsg.ComponentLocation))
			{
				action = (actions[newMsg.ComponentLocation] as IActionGUI);
			}
		}

		if (action != null)
		{
			switch (newMsg.ProposedAction)
			{
				case UpdateType.FrontIcon:
					UIActionManager.SetSprite(action, newMsg.SpriteLocation);
					break;
				case UpdateType.BackgroundIcon:
					UIActionManager.SetBackground(action, newMsg.SpriteLocation);
					break;
				case UpdateType.StateChange:
					UIActionManager.ToggleLocal(action, newMsg.showAlert);
					break;
				case UpdateType.Cooldown:
					UIActionManager.SetCooldownLocal(action, newMsg.cooldown);
					break;
			}
		}
	}

	private static SetActionUIMessageNetMessage _Send(GameObject recipient,
		IActionGUI action,
		UpdateType ProposedAction,
		bool show = false,
		float cooldown = 0,
		int location = 0)
	{
		//SO action singleton ID
		if (action is UIActionScriptableObject actionFromSO)
		{
			SetActionUIMessageNetMessage msg = new SetActionUIMessageNetMessage
			{
				actionListID = UIActionSOSingleton.ActionsTOID[actionFromSO],
				showAlert = show,
				cooldown = cooldown,
				SpriteLocation = location,
				ProposedAction = ProposedAction,
				ComponentID = SerializeType(actionFromSO.GetType()),
			};
			new SetActionUIMessage().SendTo(recipient, msg);
			return msg;
		}
		//SpellList singleton index
		else if (action is Spell spellAction)
		{
			SetActionUIMessageNetMessage msg = new SetActionUIMessageNetMessage
			{
				spellListIndex = spellAction.SpellData.Index,
				showAlert = show,
				cooldown = cooldown,
				SpriteLocation = location,
				ProposedAction = ProposedAction,
				ComponentID = SerializeType(spellAction.GetType()),
			};
			new SetActionUIMessage().SendTo(recipient, msg);
			return msg;
		}
		else
		{
			//Action pre-placed on a networked object
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
				SetActionUIMessageNetMessage msg = new SetActionUIMessageNetMessage
				{
					NetObject = netObject.netId,
					ComponentLocation = componentLocation,
					ComponentID = SerializeType(type),
					cooldown = cooldown,
					showAlert = show,
					SpriteLocation = location,
					ProposedAction = ProposedAction
				};
				new SetActionUIMessage().SendTo(recipient, msg);
				return msg;
			}
			else
			{
				Logger.LogError("Failed to find IActionGUI on NetworkIdentity");
			}
		}

		return null;
	}

	public static SetActionUIMessageNetMessage SetAction(GameObject recipient, IActionGUI iServerActionGUI, bool _showAlert)
	{
		return (_Send(recipient, iServerActionGUI, UpdateType.StateChange, _showAlert));
	}

	public static SetActionUIMessageNetMessage SetAction(GameObject recipient, IActionGUI iServerActionGUI, float cooldown)
	{
		return _Send(recipient, iServerActionGUI, UpdateType.Cooldown, cooldown: cooldown);
	}

	public static SetActionUIMessageNetMessage SetSprite(GameObject recipient, IActionGUI iServerActionGUI, int FrontIconlocation)
	{
		return (_Send(recipient, iServerActionGUI, UpdateType.FrontIcon, location: FrontIconlocation));
	}

	public static SetActionUIMessageNetMessage SetBackgroundSprite(GameObject recipient, IActionGUI iServerActionGUI,
		int FrontIconlocation)
	{
		return (_Send(recipient, iServerActionGUI, UpdateType.BackgroundIcon, location: FrontIconlocation));
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

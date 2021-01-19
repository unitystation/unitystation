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
	public ushort actionListID;
	public short spellListIndex = -1;
	public int SpriteLocation;
	public int ComponentLocation;
	public uint NetObject;
	public bool showAlert;
	public float cooldown;
	public ushort ComponentID;
	public UpdateType ProposedAction;

	public override void Process()
	{
		IActionGUI action = null;
		if (actionListID != 0)
		{
			//SO action singleton ID
			action = UIActionSOSingleton.Instance.FromID(actionListID);
		}
		else if (spellListIndex >= 0)
		{
			//SpellList singleton index
			var spellData = SpellList.Instance.FromIndex(spellListIndex);

			if (!UIActionManager.HasActionData(spellData, out action))
			{
				if (ProposedAction == UpdateType.StateChange && showAlert == false)
				{ //no need to instantiate a spell if server asks to hide one anyway
					return;
				}

				action = spellData.AddToPlayer(PlayerManager.LocalPlayerScript);
			}
		}
		else
		{
			//Action pre-placed on a networked object
			LoadNetworkObject(NetObject);
			var actions = NetworkObject.GetComponentsInChildren(DeserializeType(ComponentID));
			if ((actions.Length > ComponentLocation))
			{
				action = (actions[ComponentLocation] as IActionGUI);
			}
		}

		if (action != null)
		{
			switch (ProposedAction)
			{
				case UpdateType.FrontIcon:
					UIActionManager.SetSprite(action, SpriteLocation);
					break;
				case UpdateType.BackgroundIcon:
					UIActionManager.SetBackground(action, SpriteLocation);
					break;
				case UpdateType.StateChange:
					UIActionManager.ToggleLocal(action, showAlert);
					break;
				case UpdateType.Cooldown:
					UIActionManager.SetCooldownLocal(action, cooldown);
					break;
			}
		}
	}

	private static SetActionUIMessage _Send(GameObject recipient,
		IActionGUI action,
		UpdateType ProposedAction,
		bool show = false,
		float cooldown = 0,
		int location = 0)
	{
		//SO action singleton ID
		if (action is UIActionScriptableObject actionFromSO)
		{
			SetActionUIMessage msg = new SetActionUIMessage
			{
				actionListID = UIActionSOSingleton.ActionsTOID[actionFromSO],
				showAlert = show,
				cooldown = cooldown,
				SpriteLocation = location,
				ProposedAction = ProposedAction,
				ComponentID = SerializeType(actionFromSO.GetType()),
			};
			msg.SendTo(recipient);
			return msg;
		}
		//SpellList singleton index
		else if (action is Spell spellAction)
		{
			SetActionUIMessage msg = new SetActionUIMessage
			{
				spellListIndex = spellAction.SpellData.Index,
				showAlert = show,
				cooldown = cooldown,
				SpriteLocation = location,
				ProposedAction = ProposedAction,
				ComponentID = SerializeType(spellAction.GetType()),
			};
			msg.SendTo(recipient);
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
				SetActionUIMessage msg = new SetActionUIMessage
				{
					NetObject = netObject.netId,
					ComponentLocation = componentLocation,
					ComponentID = SerializeType(type),
					cooldown = cooldown,
					showAlert = show,
					SpriteLocation = location,
					ProposedAction = ProposedAction
				};
				msg.SendTo(recipient);
				return msg;
			}
			else
			{
				Logger.LogError("Failed to find IActionGUI on NetworkIdentity");
			}
		}

		return null;
	}

	public static SetActionUIMessage SetAction(GameObject recipient, IActionGUI iServerActionGUI, bool _showAlert)
	{
		return (_Send(recipient, iServerActionGUI, UpdateType.StateChange, _showAlert));
	}

	public static SetActionUIMessage SetAction(GameObject recipient, IActionGUI iServerActionGUI, float cooldown)
	{
		return _Send(recipient, iServerActionGUI, UpdateType.Cooldown, cooldown: cooldown);
	}

	public static SetActionUIMessage SetSprite(GameObject recipient, IActionGUI iServerActionGUI, int FrontIconlocation)
	{
		return (_Send(recipient, iServerActionGUI, UpdateType.FrontIcon, location: FrontIconlocation));
	}

	public static SetActionUIMessage SetBackgroundSprite(GameObject recipient, IActionGUI iServerActionGUI,
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

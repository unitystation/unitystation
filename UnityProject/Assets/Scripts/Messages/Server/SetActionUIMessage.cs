using System;
using System.Collections.Generic;
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
			public string SpriteName;
			public ushort actionListID;
			public short spellListIndex;
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
					case UpdateType.StateChange:
						UIActionManager.ToggleClient(action, msg.showAlert, msg.SpriteName);
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
				case UpdateType.StateChange:
					UIActionManager.MultiToggleClient(actionMulti, actionData, msg.showAlert, msg.SpriteName);
					break;
				case UpdateType.Cooldown:
					UIActionManager.SetCooldownMultiLocal(actionMulti, actionData, msg.cooldown);
					break;
			}
		}

		private static NetMessage _Send(
			GameObject recipient,
			string ID,
			IActionGUI action,
			UpdateType ProposedAction,
			bool show = false,
			float cooldown = 0,
			SpriteHandlerManager.SpriteChange Change = null)
		{
			if (Change != null)
			{
				if (SpriteHandlerManager.Instance.SpecialQueueChanges.ContainsKey(ID))
				{
					SpriteHandlerManager.Instance.SpecialQueueChanges[ID].MergeInto(Change);
				}
				else
				{
					SpriteHandlerManager.Instance.SpecialQueueChanges[ID] = Change;
				}
			}


			if (ProposedAction == UpdateType.Invalid) return new NetMessage();

			// SO action singleton ID
			if (action is UIActionScriptableObject actionFromSO)
			{
				NetMessage msg = new NetMessage
				{
					actionListID = UIActionSOSingleton.ActionsTOID[actionFromSO],
					showAlert = show,
					cooldown = cooldown,
					ProposedAction = ProposedAction,
					ComponentID = SerializeType(actionFromSO.GetType()),
					spellListIndex = -1,
					SpriteName = ID
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
					ProposedAction = ProposedAction,
					ComponentID = SerializeType(spellAction.GetType()),
					SpriteName = ID
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
						ProposedAction = ProposedAction,
						spellListIndex = -1,
						SpriteName = ID
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

		public static NetMessage SetAction(string ID , GameObject recipient, IActionGUI iServerActionGUI, bool _showAlert)
		{
			return _Send(recipient, ID, iServerActionGUI, UpdateType.StateChange, _showAlert);
		}

		public static NetMessage SetAction(string ID , GameObject recipient, IActionGUI iServerActionGUI, float cooldown)
		{
			return _Send( recipient,ID, iServerActionGUI, UpdateType.Cooldown, cooldown: cooldown);
		}

		public static NetMessage SetSprite(string ID,GameObject recipient, IActionGUI iServerActionGUI, int FrontIconlocation)
		{
			ID += "F";
			var Change = new SpriteHandlerManager.SpriteChange()
			{
				CataloguePage = FrontIconlocation
			};

			if (SpriteHandlerManager.SpecialPresentSprites.ContainsKey(ID) &&
			    SpriteHandlerManager.SpecialPresentSprites[ID] != null)
			{
				SpriteHandlerManager.SpecialPresentSprites[ID].ChangeSprite(FrontIconlocation, false);
			}
			return _Send(recipient, ID, iServerActionGUI, UpdateType.Invalid, Change:Change);
		}

		public static NetMessage SetSpriteSO(string ID, GameObject recipient, IActionGUI iServerActionGUI, SpriteDataSO spriteDataSO, List<Color> palette = null)
		{
			ID += "F";
			var Change = new SpriteHandlerManager.SpriteChange()
			{
				PresentSpriteSet = spriteDataSO.SetID,
				Palette = palette
			};
			if (SpriteHandlerManager.SpecialPresentSprites.ContainsKey(ID) &&
			    SpriteHandlerManager.SpecialPresentSprites[ID] != null)
			{
				SpriteHandlerManager.SpecialPresentSprites[ID].SetSpriteSO(spriteDataSO);
				SpriteHandlerManager.SpecialPresentSprites[ID].SetPaletteOfCurrentSprite(palette);
			}

			return _Send(recipient,ID , iServerActionGUI, UpdateType.Invalid, Change:Change );
		}

		public static NetMessage SetBackgroundSprite(string ID, GameObject recipient, IActionGUI iServerActionGUI,
			int BackIconlocation)
		{
			ID += "B";
			var Change = new SpriteHandlerManager.SpriteChange()
			{
				CataloguePage = BackIconlocation
			};

			if (SpriteHandlerManager.SpecialPresentSprites.ContainsKey(ID) &&
			    SpriteHandlerManager.SpecialPresentSprites[ID] != null)
			{
				SpriteHandlerManager.SpecialPresentSprites[ID].ChangeSprite(BackIconlocation, false);
			}

			return _Send(recipient,ID , iServerActionGUI, UpdateType.Invalid, Change : Change);
		}

		public static NetMessage SetMultiAction(string ID,GameObject recipient, IActionGUIMulti iServerActionGUIMulti, ActionData actionChosen, bool _showAlert)
		{
			return _Send(recipient,ID,  iServerActionGUIMulti, actionChosen, UpdateType.StateChange, _showAlert);
		}

		public static NetMessage SetMultiAction(string ID,GameObject recipient, IActionGUIMulti iServerActionGUIMulti, ActionData actionChosen, float cooldown)
		{
			return _Send(recipient,ID , iServerActionGUIMulti, actionChosen, UpdateType.Cooldown, cooldown: cooldown);
		}

		public static NetMessage SetMultiSpriteSO(string ID,GameObject recipient, IActionGUIMulti iServerActionGUI,  ActionData actionChosen, SpriteDataSO spriteDataSO, List<Color> palette = null)
		{
			ID += "F";
			var Change = new SpriteHandlerManager.SpriteChange()
			{
				PresentSpriteSet = spriteDataSO.SetID,
				Palette = palette
			};

			if (SpriteHandlerManager.SpecialPresentSprites.ContainsKey(ID) &&
			    SpriteHandlerManager.SpecialPresentSprites[ID] != null)
			{
				SpriteHandlerManager.SpecialPresentSprites[ID].SetSpriteSO(spriteDataSO);
				SpriteHandlerManager.SpecialPresentSprites[ID].SetPaletteOfCurrentSprite(palette);
			}


			return _Send(recipient,ID , iServerActionGUI, actionChosen, UpdateType.Invalid,  Change: Change);
		}

		public static NetMessage SetMultiSprite(string ID,GameObject recipient, IActionGUIMulti iServerActionGUI,  ActionData actionChosen, int FrontIconlocation)
		{
			ID += "F";
			var Change = new SpriteHandlerManager.SpriteChange()
			{
				CataloguePage = FrontIconlocation
			};

			if (SpriteHandlerManager.SpecialPresentSprites.ContainsKey(ID) &&
			    SpriteHandlerManager.SpecialPresentSprites[ID] != null)
			{
				SpriteHandlerManager.SpecialPresentSprites[ID].ChangeSprite(FrontIconlocation, false);
			}
			return _Send(recipient, ID, iServerActionGUI, actionChosen, UpdateType.Invalid, Change: Change);
		}

		public static NetMessage SetMultiBackgroundSprite(string ID,GameObject recipient,  IActionGUIMulti iServerActionGUI,  ActionData actionChosen,
			int BackIconlocation)
		{
			ID += "B";
			var Change = new SpriteHandlerManager.SpriteChange()
			{
				CataloguePage = BackIconlocation
			};
			if (SpriteHandlerManager.SpecialPresentSprites.ContainsKey(ID) &&
			    SpriteHandlerManager.SpecialPresentSprites[ID] != null)
			{
				SpriteHandlerManager.SpecialPresentSprites[ID].ChangeSprite(BackIconlocation, false);
			}

			return _Send(recipient,ID , iServerActionGUI, actionChosen,  UpdateType.Invalid, Change: Change);
		}

		private static NetMessage _Send(
			GameObject recipient,
			string ID,
			IActionGUIMulti action,
			ActionData actionChosen,
			UpdateType ProposedAction,
			bool show = false,
			float cooldown = 0,
			SpriteHandlerManager.SpriteChange Change = null)
		{

			if (Change != null)
			{
				if (SpriteHandlerManager.Instance.SpecialQueueChanges.ContainsKey(ID))
				{
					SpriteHandlerManager.Instance.SpecialQueueChanges[ID].MergeInto(Change);
				}
				else
				{
					SpriteHandlerManager.Instance.SpecialQueueChanges[ID] = Change;
				}
			}

			if (ProposedAction == UpdateType.Invalid) return new NetMessage();

			// SO action singleton ID
			if (action is UIActionScriptableObject actionFromSO)
			{
				NetMessage msg = new NetMessage
				{
					actionListID = UIActionSOSingleton.ActionsTOID[actionFromSO],
					showAlert = show,
					cooldown = cooldown,
					ProposedAction = ProposedAction,
					ComponentID = SerializeType(actionFromSO.GetType()),
					spellListIndex = RequestGameAction.FindIndex(action, actionChosen),
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
					ProposedAction = ProposedAction,
					ComponentID = SerializeType(spellAction.GetType()),
					SpriteName = ID
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
						ProposedAction = ProposedAction,
						spellListIndex = RequestGameAction.FindIndex(action, actionChosen),
						SpriteName = ID
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
			Cooldown,
			Invalid,
		}
	}
}

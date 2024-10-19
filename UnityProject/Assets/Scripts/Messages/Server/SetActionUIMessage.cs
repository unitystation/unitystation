using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Systems.Spells;
using ScriptableObjects.Systems.Spells;
using UI.Core.Action;
using Changeling;
using Logs;
using AdminTools;
using Messages.Server.AdminTools;
using Core.Admin.Logs;

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
			public short changelingAbilityListIndex;
			public bool isMulti;
			public int ComponentLocation;
			public uint NetObject;
			public uint NetObjectOn;
			public bool showAlert;
			public float cooldown;
			public ushort ComponentID;
			public UpdateType ProposedAction;
		}

		public override void Process(NetMessage msg)
		{
			if (msg.isMulti == false)
			{
				//Do action GUI's
				ProcessActionGUI(msg);
				return;
			}
		}

		private void ProcessActionGUI(NetMessage msg)
		{
			IGameActionHolderSingle action = null;
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

					//Loads what game object this Spell is on
					LoadNetworkObject(msg.NetObjectOn);
					action = spellData.AddToPlayer(NetworkObject.GetComponent<Mind>());
				}
			}
			else if (msg.changelingAbilityListIndex >= 0)
			{
				var abilityData = ChangelingAbilityList.Instance.FromIndex(msg.changelingAbilityListIndex);

				if (UIActionManager.HasActionData(abilityData, out action) == false)
				{
					// no need to instantiate a action if server asks to hide one anyway
					if (msg.ProposedAction == UpdateType.StateChange && msg.showAlert == false) return;

					//Loads what game object this Action is on
					LoadNetworkObject(msg.NetObjectOn);
					action = abilityData.AddToPlayer(NetworkObject.GetComponent<Mind>());
				}
			}
			else
			{
				// Action pre-placed on a networked object
				LoadNetworkObject(msg.NetObject);
				var actions = new Component[]{};
				try
				{
					var typeToFind = DeserializeType(msg.ComponentID);
					actions = NetworkObject.GetComponentsInChildren(typeToFind);
				}
				catch (Exception e)
				{
					Loggy.LogError($"[SetActionUIMessage] {e}");
				}

				if (actions.Length > msg.ComponentLocation)
				{
					action = (actions[msg.ComponentLocation] as IGameActionHolderSingle);
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
			}
		}

		private static NetMessage _Send(
			GameObject recipient,
			string ID,
			IGameActionHolderSingle action,
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
					changelingAbilityListIndex = -1,
					SpriteName = ID,
					NetObjectOn = recipient.NetId()
				};
				SendTo(recipient, msg);
				return msg;
			}
			// SpellList singleton index
			else if (action is Spell spellAction)
			{
				if (spellAction.SpellData.Index == -1)
				{
					Loggy.LogError(
						"spellAction.SpellData.Index is -1 Make sure you've added your spell to SpellList!!!!!!!!!!!");
				}

				NetMessage msg = new NetMessage
				{
					spellListIndex = spellAction.SpellData.Index,
					changelingAbilityListIndex = -1,
					showAlert = show,
					cooldown = cooldown,
					ProposedAction = ProposedAction,
					ComponentID = SerializeType(spellAction.GetType()),
					SpriteName = ID,
					NetObjectOn = recipient.NetId()
				};
				SendTo(recipient, msg);
				return msg;
			}
			else if (action is ChangelingAbility changelingAbility)
			{
				NetMessage msg = new NetMessage
				{
					spellListIndex = -1,
					changelingAbilityListIndex = changelingAbility.AbilityData.Index,
					showAlert = show,
					cooldown = cooldown,
					ProposedAction = ProposedAction,
					ComponentID = SerializeType(changelingAbility.GetType()),
					SpriteName = ID,
					NetObjectOn = recipient.NetId()
				};
				SendTo(recipient, msg);
				return msg;
			}
			else
			{
				// Action pre-placed on a networked object
				var netObject = (action as Component).GetComponent<NetworkIdentity>();
				if (netObject != null)
				{
					var type = action.GetType();
					var foundActions = netObject.GetComponentsInChildren(type);
					var componentLocation = 0;
					bool isFound = false;
					foreach (var foundAction in foundActions)
					{
						if ((foundAction as IGameActionHolderSingle) == action)
						{
							isFound = true;
							break;
						}

						componentLocation++;
					}

					if (isFound == false) return new NetMessage();
					NetMessage msg = new NetMessage
					{
						NetObject = netObject.netId,
						ComponentLocation = componentLocation,
						ComponentID = SerializeType(type),
						cooldown = cooldown,
						showAlert = show,
						ProposedAction = ProposedAction,
						spellListIndex = -1,
						changelingAbilityListIndex = -1,
						SpriteName = ID,
						NetObjectOn = recipient.NetId()
					};
					SendTo(recipient, msg);
					return msg;
				}
				else
				{
					Loggy.LogError("Failed to find IGameActionHolderSingle on NetworkIdentity", Category.UserInput);
				}
			}

			return new NetMessage();
		}

		public static NetMessage SetAction(string ID , GameObject recipient, IGameActionHolderSingle iServerActionGUI, bool _showAlert)
		{
			return _Send(recipient, ID, iServerActionGUI, UpdateType.StateChange, _showAlert);
		}

		public static NetMessage SetAction(string ID , GameObject recipient, IGameActionHolderSingle iServerActionGUI, float cooldown)
		{
			return _Send( recipient,ID, iServerActionGUI, UpdateType.Cooldown, cooldown: cooldown);
		}

		public static NetMessage SetSprite(string ID,GameObject recipient, IGameActionHolderSingle iServerActionGUI, int FrontIconlocation)
		{
			ID += "F";
			var Change = new SpriteHandlerManager.SpriteChange()
			{
				CataloguePage = FrontIconlocation
			};

			if (SpriteHandlerManager.SpecialPresentSprites.ContainsKey(ID) &&
			    SpriteHandlerManager.SpecialPresentSprites[ID] != null)
			{
				SpriteHandlerManager.SpecialPresentSprites[ID].SetCatalogueIndexSprite(FrontIconlocation, false);
			}
			return _Send(recipient, ID, iServerActionGUI, UpdateType.Invalid, Change:Change);
		}

		public static NetMessage SetSpriteSO(string ID, GameObject recipient, IGameActionHolderSingle iServerActionGUI, SpriteDataSO spriteDataSO, List<Color> palette = null)
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

		public static NetMessage SetBackgroundSprite(string ID, GameObject recipient, IGameActionHolderSingle iServerActionGUI,
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
				SpriteHandlerManager.SpecialPresentSprites[ID].SetCatalogueIndexSprite(BackIconlocation, false);
			}

			return _Send(recipient,ID , iServerActionGUI, UpdateType.Invalid, Change : Change);
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

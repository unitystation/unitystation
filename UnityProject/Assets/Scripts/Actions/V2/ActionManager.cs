using System;
using System.Collections.Generic;
using Actions.V2.UI;
using Logs;
using Mirror;
using UnityEngine;

namespace Actions.V2
{
	public class ActionManager : NetworkBehaviour
	{
		[field: SerializeField] public OnType ActionButtonOnType { get; private set; } = OnType.Body;
		[field: SerializeField] public float ActionButtonRefreshRate { get; private set; } = 0.75f;

		private readonly SyncList<ActionButtonData> ActionButtons = new SyncList<ActionButtonData>();
		private readonly SyncList<CooldownInfo> ActionCooldowns = new();
		private readonly Dictionary<string, (ActionButtonData Data, Action<Vector2> Action)> ServerActionRegistry = new();
		private readonly Dictionary<string, (ActionButtonData Data, Action<Vector2> Action)> ClientActionRegistry = new();
		private NetworkIdentity cachedNetIdentity;

		private const float MINIMUM_COOLDOWN_TIME = 0.085f;

		[Serializable]
		public class CooldownInfo : NetworkMessage
		{
			public string ActionId { get; private set; }
			public DateTime CooldownEnd { get; private set; }

			public CooldownInfo() { }
			public CooldownInfo(string actionId, DateTime cooldownEnd)
			{
				ActionId = actionId;
				CooldownEnd = cooldownEnd;
			}

			public void Serialize(NetworkWriter writer)
			{
				writer.WriteString(ActionId);
				writer.WriteLong(CooldownEnd.Ticks);
			}

			public void Deserialize(NetworkReader reader)
			{
				ActionId = reader.ReadString();
				CooldownEnd = new DateTime(reader.ReadLong());
			}
		}

		public enum OnType
		{
			Body,
			Mind
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			ActionButtons.Callback += OnActionButtonsChanged;
		}

		private void Start()
		{
			if (CustomNetworkManager.IsHeadless == false) UpdateManager.Add(UpdateMe, ActionButtonRefreshRate);
			cachedNetIdentity = gameObject.NetWorkIdentity();
		}

		private void OnDestroy()
		{
			if (CustomNetworkManager.IsHeadless == false) UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
			ActionButtons.Callback -= OnActionButtonsChanged;
			ClearCooldowns();
		}

		private void UpdateMe()
		{
			if (cachedNetIdentity == null) cachedNetIdentity = gameObject.NetWorkIdentity();
			switch (ActionButtonOnType)
			{
				case OnType.Body:
					UICheckBody();
					break;
				case OnType.Mind:
					UICheckMind();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void UICheckMind()
		{
			if (PlayerManager.LocalMindScript && PlayerManager.LocalMindScript.gameObject.NetWorkIdentity() ==
			    cachedNetIdentity)
			{
				ActionButtonManager.Instance.RefreshButtonsMind(ActionButtons, cachedNetIdentity);
			}
			else
			{
				ActionButtonManager.Instance.DeleteSpawnedMindUIActions(cachedNetIdentity);
			}
		}

		private void UICheckBody()
		{
			if (PlayerManager.LocalMindScript?.GetRelatedBodies().Contains(cachedNetIdentity) == true)
			{
				ActionButtonManager.Instance.RefreshButtonsBody(ActionButtons, cachedNetIdentity);
			}
			else
			{
				ActionButtonManager.Instance.DeleteSpawnedBodyUIActions(cachedNetIdentity);
			}
		}

		private void OnActionButtonsChanged(SyncList<ActionButtonData>.Operation op, int index, ActionButtonData oldItem, ActionButtonData newItem)
		{
			// Notify UI system on client to refresh buttons
			UpdateMe();
			this.netIdentity.isDirty = true;
		}

		public void RegisterNewAction(ActionButtonData newData, Action<Vector2> logic)
		{
			newData.TrackingObject = gameObject.NetWorkIdentity();
			switch (newData.TriggerType)
			{
				case ActionTriggerType.ServerOnly:
					ServerAddAction(newData, logic);
					break;
				case ActionTriggerType.ClientOnly:
					ClientAddAction(newData, logic);
					break;
				case ActionTriggerType.Both:
				default:
					ServerAddAction(newData, logic);
					ClientAddAction(newData, logic);
					break;
			}

			if (CustomNetworkManager.IsServer)
			{
				ActionButtons.Add(newData);
				this.netIdentity.isDirty = true;
			}
		}

		/// <summary>
		/// Registers a new action with the given parameters.
		/// </summary>
		/// <param name="newID">the ID that will be used to trigger said command</param>
		/// <param name="displayName">The UI name</param>
		/// <param name="desc">The description of the action that will be executed</param>
		/// <param name="triggerType">Do you want to execute this command on the server? the client? or both? (Server always recommended)</param>
		/// <param name="Icon">The graphics used for the button</param>
		/// <param name="logic">The actual function that will be run when pressing the button</param>
		/// <param name="canBeUsedWhileGhosting">[Mind Action Manger Only] - Can this action be used while ghosting?</param>
		/// <param name="cooldownTime">How long before we can run this command again?</param>
		public void RegisterNewAction(string newID, string displayName, string desc, ActionTriggerType triggerType,
			List<SpriteDataSO> Icon, Action<Vector2> logic, bool canBeUsedWhileGhosting = false, float cooldownTime = 0f)
		{
			var ActionData = new ActionButtonData
			{
				ID = newID,
				DisplayName = displayName,
				Description = desc,
				TriggerType = triggerType,
				CooldownTime = cooldownTime,
				AnimatedIconCatalogue = Icon,
				CanUseWhileGhosting = canBeUsedWhileGhosting,
				TrackingObject = gameObject.NetWorkIdentity()
			};

			switch (triggerType)
			{
				case ActionTriggerType.ServerOnly:
					ServerAddAction(ActionData, logic);
					break;
				case ActionTriggerType.ClientOnly:
					ClientAddAction(ActionData, logic);
					break;
				case ActionTriggerType.Both:
				default:
					ServerAddAction(ActionData, logic);
					ClientAddAction(ActionData, logic);
					break;
			}
			ActionButtons.Add(ActionData);
			this.cachedNetIdentity.isDirty = true;
		}

		public void UnregisterAction(ActionButtonData data)
		{
			switch (data.TriggerType)
			{
				case ActionTriggerType.ServerOnly:
					ServerRemoveAction(data.ID);
					break;
				case ActionTriggerType.ClientOnly:
					ClientRemoveAction(data.ID);
					break;
				case ActionTriggerType.Both:
				default:
					ServerRemoveAction(data.ID);
					ClientRemoveAction(data.ID);
					break;
			}
		}

		[Command]
		public void CmdTriggerAction(string actionId, Vector2 mouseLocation)
		{
			if (IsActionOnCooldown(actionId)) return;
			if (ServerActionRegistry.TryGetValue(actionId, out var found) == false) return;
			try
			{
				found.Action?.Invoke(mouseLocation);
				if (found.Data.CooldownTime > MINIMUM_COOLDOWN_TIME)
				{
					AddCooldown(actionId, found.Data.CooldownTime);
				}
			}
			catch (Exception e)
			{
				Loggy.Error(e.ToString());
			}
		}

		[Client]
		public void TriggerClientAction(string actionId, Vector2 mouseLocation)
		{
			if (IsActionOnCooldown(actionId)) return;
			if (ClientActionRegistry.TryGetValue(actionId, out var found))
			{
				found.Action?.Invoke(mouseLocation);
			}
		}

		[Server]
		public void ServerAddAction(ActionButtonData actionData, Action<Vector2> newAction)
		{
			Debug.Log("adding action to serverActionRegistry: " + actionData);
			if (ServerActionRegistry.ContainsKey(actionData.ID) == false)
			{
				ServerActionRegistry.Add(actionData.ID, (actionData, newAction));
			}
			else
			{
				Debug.LogWarning("Action already exists: " + actionData);
			}
		}

		[Server]
		public void ServerRemoveAction(string actionId)
		{
			ActionButtons.RemoveAll(a =>
			{
				var hasItem = a.ID == actionId;
				if (hasItem)
				{
					ServerActionRegistry.Remove(actionId);
				}
				return hasItem;
			});
			if (cachedNetIdentity == null)
			{
				cachedNetIdentity = gameObject.NetWorkIdentity();
			}
			cachedNetIdentity.isDirty = true;
		}

		[Client]
		public void ClientAddAction(ActionButtonData actionId, Action<Vector2> newAction)
		{
			Debug.Log("adding action to clientActionRegistry: " + actionId);
			if (ClientActionRegistry.ContainsKey(actionId.ID) == false)
			{
				ClientActionRegistry.Add(actionId.ID, (actionId, newAction));
			}
			else
			{
				Debug.LogWarning("Action already exists: " + actionId);
			}
		}

		[Client]
		private void ClientRemoveAction(string dataID)
		{
			ActionButtons.RemoveAll(a =>
			{
				var hasItem = a.ID == dataID;
				if (hasItem)
				{
					ClientActionRegistry.Remove(dataID);
				}
				return hasItem;
			});
			this.cachedNetIdentity.isDirty = true;
		}

		private void ClearCooldowns()
		{
			var now = DateTime.UtcNow;
			var keysToRemove = new List<CooldownInfo>();

			foreach (var kvp in ActionCooldowns)
			{
				if (kvp.CooldownEnd <= now)
				{
					keysToRemove.Add(kvp);
				}
			}

			foreach (var key in keysToRemove)
			{
				ActionCooldowns.Remove(key);
				this.cachedNetIdentity.isDirty = true;
			}


		}

		private void AddCooldown(string actionId, float cooldownTime)
		{
			if (cooldownTime <= 0.085f) return;
			var cooldownEnd = DateTime.UtcNow.AddSeconds(cooldownTime);
			if (ActionCooldowns.Find(x => x.ActionId == actionId) is { } _)
			{
				return;
			}
			ActionCooldowns.Add(new CooldownInfo(actionId, cooldownEnd));
		}

		private bool IsActionOnCooldown(string actionId)
		{
			var isUnderCooldown = ActionCooldowns.Find(tuple => tuple.ActionId == actionId);
			if (isUnderCooldown == null) return false;
			if (isUnderCooldown.CooldownEnd <= DateTime.UtcNow)
			{
				ActionCooldowns.Remove(isUnderCooldown);
				this.cachedNetIdentity.isDirty = true;
				return false; // Cooldown has expired
			}
			else
			{
				Chat.AddExamineMsg(gameObject,
					$"This action is still on cooldown, remaining time: {Math.Round((isUnderCooldown.CooldownEnd - DateTime.UtcNow).TotalSeconds, 2)} seconds.");
				return true;
			}
		}

		[Client]
		public float GetRemainingCooldown(string actionId)
		{
			var isUnderCooldown = ActionCooldowns.Find(tuple => tuple.ActionId == actionId);
			if (isUnderCooldown == null) return 0f;
			var remaining = (isUnderCooldown.CooldownEnd - DateTime.UtcNow).TotalSeconds;
			return remaining > 0 ? (float)remaining : 0f;
		}
	}
}
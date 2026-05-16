using System;
using System.Collections.Generic;
using Logs;
using Mirror;
using UnityEngine;
using US13.Actions.V2.UI;
using US13.Core.Chat;
using US13.Managers.NetworkManagement;
using US13.Managers.UpdateManager;
using US13.Player;
using Util;

namespace US13.Actions.V2
{
	public class ActionManager : NetworkBehaviour
	{
		[field: SerializeField] public OnType ActionButtonOnType { get; private set; } = OnType.Body;
		[field: SerializeField] public float ActionButtonRefreshRate { get; private set; } = 0.75f;

		private readonly SyncList<ActionButtonData> ActionButtons = new SyncList<ActionButtonData>();
		private readonly SyncList<CooldownInfo> ActionCooldowns = new SyncList<CooldownInfo>();
		private readonly Dictionary<string, (ActionButtonData Data, Action<Vector2> Action)> ServerActionRegistry = new();
		private readonly Dictionary<string, (ActionButtonData Data, Action<Vector2> Action)> ClientActionRegistry = new();
		private NetworkIdentity cachedNetIdentity;

		private const float MINIMUM_COOLDOWN_TIME = 0.085f;

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
			ServerRemoveAllActions();
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
			if (PlayerManager.LocalMindScript == null ||
			    PlayerManager.LocalMindScript.gameObject.NetWorkIdentity() != cachedNetIdentity)
			{
				return;
			}
			ActionButtonManager.Instance.RefreshButtonsMind(ActionButtons, cachedNetIdentity);
		}

		private void UICheckBody()
		{
			if (PlayerManager.LocalMindScript?.GetRelatedBodies().Contains(cachedNetIdentity) == false)
			{
				return;
			}
			ActionButtonManager.Instance.RefreshButtonsBody(ActionButtons, cachedNetIdentity);
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

			if (CustomNetworkManager.IsServer && ActionButtons.Contains(newData) == false)
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
				if (found.Data.CooldownTime > MINIMUM_COOLDOWN_TIME)
				{
					AddCooldown(actionId, found.Data.CooldownTime);
				}
				found.Action?.Invoke(mouseLocation);
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
			if (cachedNetIdentity == false) cachedNetIdentity = gameObject.NetWorkIdentity();
			cachedNetIdentity.isDirty = true;
		}

		[Server]
		public void ServerRemoveAllActions()
		{
			ActionButtons.Clear();
			if (cachedNetIdentity == false) cachedNetIdentity = gameObject.NetWorkIdentity();
			cachedNetIdentity.isDirty = true;
		}

		[Server]
		public void ServerEndCooldown(string actionId)
		{
			ActionCooldowns.RemoveAll(x => x.ActionId.Equals(actionId, StringComparison.InvariantCulture));
			this.cachedNetIdentity.isDirty = true;
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
			ActionCooldowns.RemoveAll(x => x.GetCooldownEnd() <= DateTime.UtcNow);
			this.cachedNetIdentity.isDirty = true;
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
			var isUnderCooldown = ActionCooldowns.Find(tuple => tuple.ActionId.Equals(actionId, StringComparison.InvariantCulture));
			if (isUnderCooldown == null) return false;
			if (isUnderCooldown.GetCooldownEnd() <= DateTime.UtcNow)
			{
				ActionCooldowns.Remove(isUnderCooldown);
				this.cachedNetIdentity.isDirty = true;
				return false; // Cooldown has expired
			}
			Chat.AddExamineMsg(gameObject, $"This action is still on cooldown, remaining time: {Math.Round((isUnderCooldown.GetCooldownEnd() - DateTime.UtcNow).TotalSeconds, 2)} seconds.");
            return true;
		}

		[Client]
		public float GetRemainingCooldown(string actionId)
		{
			var isUnderCooldown = ActionCooldowns.Find(tuple => tuple is { ActionId: not null } && tuple.ActionId.Equals(actionId, StringComparison.InvariantCulture));
			if (isUnderCooldown == null) return 0.0f;
			var remaining = (isUnderCooldown.GetCooldownEnd() - DateTime.UtcNow).TotalSeconds;
			return Mathf.Max((float)remaining, 0.0f);
		}
	}
}
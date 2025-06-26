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
		[field: SerializeField] public float ActionButtonRefreshRate { get; private set; } = 5f;

		public SyncList<ActionButtonData> ActionButtons = new SyncList<ActionButtonData>();
		private readonly Dictionary<string, (ActionButtonData Data, Action Action)> ServerActionRegistry = new();
		private readonly Dictionary<string, (ActionButtonData Data, Action Action)> ClientActionRegistry = new();

		private readonly Dictionary<string, DateTime> ActionCooldowns = new Dictionary<string, DateTime>();

		private const float MINIMUM_COOLDOWN_TIME = 0.085f;

		public override void OnStartClient()
		{
			base.OnStartClient();
			ActionButtons.Callback += OnActionButtonsChanged;
		}

		private void Start()
		{
			if (CustomNetworkManager.IsHeadless == false) UpdateManager.Add(UpdateMe, ActionButtonRefreshRate);
		}

		private void OnDestroy()
		{
			if (CustomNetworkManager.IsHeadless == false) UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
			ActionButtons.Callback -= OnActionButtonsChanged;
			ClearCooldowns();
		}

		private void UpdateMe()
		{
			if (PlayerManager.LocalPlayerObject == gameObject)
			{
				ActionButtonManager.Instance.RefreshButtons(ActionButtons);
			}
		}

		private void OnActionButtonsChanged(SyncList<ActionButtonData>.Operation op, int index, ActionButtonData oldItem, ActionButtonData newItem)
		{
			// Notify UI system on client to refresh buttons
			if (isLocalPlayer)
			{
				ActionButtonManager.Instance.RefreshButtons(ActionButtons);
			}
		}

		public void RegisterNewAction(ActionButtonData newData, Action logic)
		{
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
			ActionButtons.Add(newData);
		}

		public void RegisterNewAction(string newID, string displayName, string desc, ActionTriggerType triggerType,
			Sprite Icon, Action logic, float cooldownTime = 0f)
		{
			var ActionData = new ActionButtonData
			{
				ID = newID,
				DisplayName = displayName,
				Description = desc,
				TriggerType = triggerType,
				Icon = Icon,
				CooldownTime = cooldownTime
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
		public void CmdTriggerAction(string actionId)
		{
			if (ServerActionRegistry.TryGetValue(actionId, out var found))
			{
				Debug.Log($"Server executing action: {actionId}");

				try
				{
					found.Action?.Invoke();
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
			else
			{
				Debug.Log($"server action not found: {actionId}");
			}
		}

		[Client]
		public void TriggerClientAction(string actionId)
		{
			if (ClientActionRegistry.TryGetValue(actionId, out var found))
			{
				found.Action?.Invoke();
			}
		}

		[Server]
		public void ServerAddAction(ActionButtonData actionData, Action newAction)
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
		}

		[Client]
		public void ClientAddAction(ActionButtonData actionId, Action newAction)
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
		}

		private void ClearCooldowns()
		{
			var now = DateTime.UtcNow;
			var keysToRemove = new List<string>();

			foreach (var kvp in ActionCooldowns)
			{
				if (kvp.Value <= now)
				{
					keysToRemove.Add(kvp.Key);
				}
			}

			foreach (var key in keysToRemove)
			{
				ActionCooldowns.Remove(key);
			}
		}

		private void AddCooldown(string actionId, float cooldownTime)
		{
			if (cooldownTime <= 0.085f) return;
			var cooldownEnd = DateTime.UtcNow.AddSeconds(cooldownTime);
			ActionCooldowns[actionId] = cooldownEnd;
		}
	}
}
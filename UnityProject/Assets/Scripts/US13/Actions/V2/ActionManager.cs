using System;
using System.Collections.Generic;
using Logs;
using Mirror;
using UnityEngine;
using US13.Actions.V2.UI;
using US13.Core.Chat;
using US13.Managers.NetworkManagement;
using US13.Player;
using Util;

namespace US13.Actions.V2
{
	public class ActionManager : NetworkBehaviour
	{
		[field: SerializeField] public OnType ActionButtonOnType { get; private set; } = OnType.Body;

		private readonly SyncList<ActionButtonData> ActionButtons = new();
		private readonly SyncList<CooldownInfo> ActionCooldowns = new();

		private readonly Dictionary<string, (ActionButtonData Data, Action<Vector2> Action)> ServerActionRegistry = new();
		private readonly Dictionary<string, (ActionButtonData Data, Action<Vector2> Action)> ClientActionRegistry = new();

		private NetworkIdentity cachedNetIdentity;

		private const float MINIMUM_COOLDOWN_TIME = 0.085f;

		/// <summary>
		/// Raised whenever the available actions/UI state needs to be refreshed.
		/// Other systems can invoke this when Body/Mind ownership or relationships change.
		/// </summary>
		public event Action ActionsChanged;


		public enum OnType
		{
			Body,
			Mind
		}

		public override void OnStartClient()
		{
			base.OnStartClient();

			ActionButtons.Callback += OnActionButtonsChanged;

			// Initial UI population.
			RefreshUI();
		}

		private void Start()
		{
			cachedNetIdentity = gameObject.NetWorkIdentity();
			switch (ActionButtonOnType)
			{
				case OnType.Body:
				case OnType.Mind:
				default:
					var player = GetComponent<PlayerScript>();
					if (player)
					{
						player.OnBodyControlledByPlayer.AddListener(RefreshBodyUI);
						player.OnBodyUnControlledByPlayer.AddListener(RefreshBodyUI);
					}
					break;
			}
		}

		private void OnDestroy()
		{
			ActionButtons.Callback -= OnActionButtonsChanged;
			ClearCooldowns();

			if (CustomNetworkManager.IsServer) ServerRemoveAllActions();
			ActionsChanged = null;
		}

		/// <summary>
		/// Refreshes the UI if this ActionManager is relevant to the local player.
		/// </summary>
		public void RefreshUI()
		{
			if (cachedNetIdentity == null) cachedNetIdentity = gameObject.NetWorkIdentity();
			switch (ActionButtonOnType)
			{
				case OnType.Body:
					RefreshBodyUI();
					break;
				case OnType.Mind:
					RefreshMindUI();
					break;
				default:
					RefreshBodyUI();
					RefreshMindUI();
					break;
			}
			ActionsChanged?.Invoke();
		}

		[Client]
		private void RefreshMindUI()
		{
			if (PlayerManager.LocalMindScript == null) return;
			if (PlayerManager.LocalMindScript.gameObject.NetWorkIdentity() != cachedNetIdentity) return;
			if (ActionButtonManager.Instance == null) return;
			ActionButtonManager.Instance.RefreshButtonsMind(ActionButtons, cachedNetIdentity);
		}

		[Client]
		private void RefreshBodyUI()
		{
			if (PlayerManager.LocalMindScript == null) return;
			if (PlayerManager.LocalMindScript.GetRelatedBodies().Contains(cachedNetIdentity) == false) return;
			if (ActionButtonManager.Instance == null) return;

			if (PlayerManager.LocalMindScript.IsGhosting)
			{
				ActionButtonManager.Instance.RefreshButtonsBody(new(), cachedNetIdentity);
			}
			else
			{
				ActionButtonManager.Instance.RefreshButtonsBody(ActionButtons, cachedNetIdentity);
			}
		}


		private void OnActionButtonsChanged(SyncList<ActionButtonData>.Operation op, int index, ActionButtonData oldItem, ActionButtonData newItem)
		{
			RefreshUI();
		}

		public void NotifyContextChanged()
		{
			RefreshUI();
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
		public void RegisterNewAction(string newID, string displayName, string desc, ActionTriggerType triggerType, List<SpriteDataSO> Icon,
			Action<Vector2> logic, bool canBeUsedWhileGhosting = false,
			float cooldownTime = 0f)
		{
			var actionData = new ActionButtonData
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
					ServerAddAction(actionData, logic);
					break;

				case ActionTriggerType.ClientOnly:
					ClientAddAction(actionData, logic);
					break;

				case ActionTriggerType.Both:
				default:
					ServerAddAction(actionData, logic);
					ClientAddAction(actionData, logic);
					break;
			}

			if (CustomNetworkManager.IsServer && ActionButtons.Contains(actionData) == false)
			{
				ActionButtons.Add(actionData);
			}
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
			if (ServerActionRegistry.TryGetValue(actionId, out (ActionButtonData Data, Action<Vector2> Action) found) == false) return;

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

			if (ClientActionRegistry.TryGetValue(actionId, out (ActionButtonData Data, Action<Vector2> Action) found))
			{
				found.Action?.Invoke(mouseLocation);
			}
		}

		[Server]
		public void ServerAddAction(ActionButtonData actionData, Action<Vector2> newAction)
		{
			if (!ServerActionRegistry.ContainsKey(actionData.ID))
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
				bool hasItem = a.ID == actionId;

				if (hasItem) ServerActionRegistry.Remove(actionId);

				return hasItem;
			});
		}

		[Server]
		public void ServerRemoveAllActions()
		{
			ActionButtons.Clear();
			ServerActionRegistry.Clear();
		}

		[Server]
		public void ServerEndCooldown(string actionId)
		{
			ActionCooldowns.RemoveAll(x => x.ActionId.Equals(actionId, StringComparison.InvariantCulture));
		}

		[Client]
		public void ClientAddAction(ActionButtonData actionData, Action<Vector2> newAction)
		{
			Debug.Log("Adding action to clientActionRegistry: " + actionData);

			if (ClientActionRegistry.ContainsKey(actionData.ID) == false)
			{
				ClientActionRegistry.Add(actionData.ID, (actionData, newAction));
			}
			else
			{
				Debug.LogWarning("Action already exists: " + actionData);
			}
		}

		[Client]
		private void ClientRemoveAction(string dataID)
		{
			ClientActionRegistry.Remove(dataID);

			// Only the server owns the SyncList.
			// The SyncList change will arrive through Mirror and
			// OnActionButtonsChanged will refresh the UI.
		}

		private void ClearCooldowns()
		{
			if (ActionCooldowns == null) return;

			ActionCooldowns.RemoveAll(x => x.GetCooldownEnd() <= DateTime.UtcNow);
		}

		private void AddCooldown(string actionId, float cooldownTime)
		{
			if (cooldownTime <= MINIMUM_COOLDOWN_TIME) return;

			DateTime cooldownEnd = DateTime.UtcNow.AddSeconds(cooldownTime);

			if (ActionCooldowns.Find(x => x.ActionId == actionId) is not null) return;

			ActionCooldowns.Add(new CooldownInfo(actionId, cooldownEnd));
		}

		private bool IsActionOnCooldown(string actionId)
		{
			CooldownInfo isUnderCooldown =
				ActionCooldowns.Find(tuple => tuple.ActionId.Equals(
					actionId,
					StringComparison.InvariantCulture
					)
				);

			if (isUnderCooldown == null) return false;

			if (isUnderCooldown.GetCooldownEnd() <= DateTime.UtcNow)
			{
				ActionCooldowns.Remove(isUnderCooldown);

				// No periodic update is required.
				// The next query will see the cooldown as expired.
				return false;
			}

			Chat.AddExamineMsg(gameObject,
				"This action is still on cooldown, remaining time: "
				+ $"{Math.Round((isUnderCooldown.GetCooldownEnd() - DateTime.UtcNow).TotalSeconds, 2)} seconds.");

			return true;
		}

		[Client]
		public float GetRemainingCooldown(string actionId)
		{
			CooldownInfo isUnderCooldown = ActionCooldowns.Find(tuple => tuple is { ActionId: not null } &&
			                                                             tuple.ActionId.Equals(
				                                                             actionId,
				                                                             StringComparison.InvariantCulture
				                                                             )
			                                                             );

			if (isUnderCooldown == null) return 0.0f;

			double remaining = (isUnderCooldown.GetCooldownEnd() - DateTime.UtcNow).TotalSeconds;

			return Mathf.Max((float)remaining, 0.0f);
		}
	}
}
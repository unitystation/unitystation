using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Messages.Server;
using UnityEngine;
using Util;

namespace UI.Action
{
	/// <summary>
	/// Used to set the action From the client
	/// </summary>
	public class UIActionManager : MonoBehaviour
	{
		public GameObject Panel;
		public GameObject TooltipPrefab;

		public ActionTooltip TooltipInstance => tooltipInstance == null
			? tooltipInstance = Instantiate(TooltipPrefab, transform.parent).GetComponent<ActionTooltip>()
			: tooltipInstance;

		private ActionTooltip tooltipInstance;

		private static UIActionManager uIActionManager;
		public static UIActionManager Instance => FindUtils.LazyFindObject(ref uIActionManager);

		/// <summary>
		/// Returns true if an action that is aimable is active.
		/// </summary>
		public bool IsAiming => HasActiveAction && ActiveAction.ActionData.IsAimable;

		public UIAction UIAction;
		public List<UIAction> PooledUIAction = new List<UIAction>();

		public Dictionary<IAction, List<UIAction>> DicIActionGUI = new Dictionary<IAction, List<UIAction>>();

		public UIAction ActiveAction { get; set; }
		public bool HasActiveAction => ActiveAction != null;

		#region IActionGUI

		/// <summary>
		/// Set the action button visibility, locally (clientside)
		/// </summary>
		public static void ToggleLocal(IActionGUI iActionGUI, bool show)
		{

			if (show)
			{
				if (Instance.DicIActionGUI.ContainsKey(iActionGUI))
				{
					Logger.Log("iActionGUI Already added", Category.UI);
					return;
				}
				Show(iActionGUI);
			}
			else
			{
				Hide(iActionGUI);
			}
		}

		/// <summary>
		/// Set the action button visibility for the given player, with network sync
		/// </summary>
		public static void Toggle(IActionGUI iActionGUI, bool show, GameObject recipient)
		{
			SetActionUIMessage.SetAction(recipient, iActionGUI, show);
		}

		public static bool HasActionData(ActionData actionData, [CanBeNull] out IActionGUI actionInstance)
		{
			foreach (var key in Instance.DicIActionGUI.Keys)
			{
				if (key is IActionGUI keyI && keyI.ActionData == actionData)
				{
					actionInstance = keyI;
					return true;
				}
			}

			actionInstance = null;
			return false;
		}

		public static void SetSprite(IActionGUI iActionGUI, Sprite sprite)
		{
			if (Instance.DicIActionGUI.ContainsKey(iActionGUI))
			{
				var _UIAction = Instance.DicIActionGUI[iActionGUI][0];
				_UIAction.IconFront.SetSprite(sprite);
			}
			else
			{
				Logger.Log("iActionGUI Not present", Category.UI);
			}
		}

		/// <summary>
		/// Sets the sprite of the action button.
		/// </summary>
		public static void SetSpriteSO(IActionGUI iActionGUI, SpriteDataSO sprite, bool networked = true, List<Color> palette = null)
		{
			Debug.Assert(!(sprite.IsPalette && palette == null), "Paletted sprites should never be set without a palette");

			if (Instance.DicIActionGUI.ContainsKey(iActionGUI))
			{
				var _UIAction = Instance.DicIActionGUI[iActionGUI][0];
				_UIAction.IconFront.SetSpriteSO(sprite, networked: networked);
				_UIAction.IconFront.SetPaletteOfCurrentSprite(palette);
			}
			else
			{
				Logger.Log("iActionGUI Not present", Category.UI);
			}
		}

		public static void SetSprite(IActionGUI iActionGUI, int Location)
		{
			if (Instance.DicIActionGUI.ContainsKey(iActionGUI))
			{
				var _UIAction = Instance.DicIActionGUI[iActionGUI][0];
				_UIAction.IconFront.ChangeSprite(Location);
			}
			else
			{

				Logger.Log("iActionGUI Not present", Category.UI);
			}
		}

		public static void SetBackground(IActionGUI iActionGUI, int Location)
		{
			if (Instance.DicIActionGUI.ContainsKey(iActionGUI))
			{
				var _UIAction = Instance.DicIActionGUI[iActionGUI][0];
				_UIAction.IconBackground.ChangeSprite(Location);
			}
			else
			{

				Logger.Log("iActionGUI Not present", Category.UI);
			}
		}

		public static void SetBackground(IActionGUI iActionGUI, Sprite sprite)
		{
			if (Instance.DicIActionGUI.ContainsKey(iActionGUI))
			{
				var _UIAction = Instance.DicIActionGUI[iActionGUI][0];
				_UIAction.IconBackground.SetSprite(sprite);
			}
			else
			{
				Logger.Log("iActionGUI not present!", Category.UI);
			}
		}

		public static void SetCooldownLocal(IActionGUI iActionGUI, float cooldown)
		{
			if (Instance.DicIActionGUI.ContainsKey(iActionGUI))
			{
				var _UIAction = Instance.DicIActionGUI[iActionGUI][0];
				_UIAction.CooldownOpacity.LeanScaleY(0f, cooldown).setFrom(1f);

				if (cooldown > 5)
				{
					Instance.StartCoroutine(Instance.CooldownCountdown(_UIAction, cooldown));
				}
			}
			else
			{
				Logger.Log("iActionGUI not present!", Category.UI);
			}
		}

		public static void SetCooldown(IActionGUI iActionGUI, float cooldown, GameObject recipient)
		{
			SetActionUIMessage.SetAction(recipient, iActionGUI, cooldown);
		}

		public static void Show(IActionGUI iActionGUI)
		{
			foreach (var actionButton in Instance.DicIActionGUI)
			{
				//Remove old button from list. Don't spawn the same button if it already exists!
				if (actionButton.Key is IActionGUI keyI && actionButton.Value[0].ActionData == iActionGUI.ActionData)
				{
					Hide(keyI);
					break;
				}
			}

			UIAction _UIAction;
			if (Instance.PooledUIAction.Count > 0)
			{
				_UIAction = Instance.PooledUIAction[0];
				Instance.PooledUIAction.RemoveAt(0);
			}
			else
			{
				_UIAction = Instantiate(Instance.UIAction);
				_UIAction.transform.SetParent(Instance.Panel.transform, false);
			}

			if (Instance.DicIActionGUI.ContainsKey(iActionGUI) == false)
			{
				Instance.DicIActionGUI.Add(iActionGUI, new List<UIAction>());
			}

			Instance.DicIActionGUI[iActionGUI].Add(_UIAction);
			_UIAction.SetUp(iActionGUI);

		}

		public static void Hide(IActionGUI iAction)
		{
			if (Instance.DicIActionGUI.ContainsKey(iAction))
			{
				var _UIAction = Instance.DicIActionGUI[iAction][0];
				_UIAction.Pool();
				Instance.PooledUIAction.Add(_UIAction);
				Instance.DicIActionGUI.Remove(iAction);
			}
			else
			{
				Logger.Log("iActionGUI Not present", Category.UI);
			}
		}

		#endregion

		#region Events

		public void AimClicked(Vector3 clickPosition)
		{
			if (HasActiveAction == false) return;
			ActiveAction.RunActionWithClick(clickPosition);
		}

		public void OnRoundEnd()
		{
			foreach (var _Actions in DicIActionGUI)
			{
				foreach (var _Action in _Actions.Value)
				{
					_Action.Pool();
				}
			}

			DicIActionGUI = new Dictionary<IAction, List<UIAction>>();
		}

		public void OnPlayerDie()
		{
			CheckEvent(Event.PlayerDied);
		}

		public void OnPlayerSpawn()
		{
			CheckEvent(Event.PlayerSpawned);
		}

		public void LoggedOut()
		{
			CheckEvent(Event.LoggedOut);
		}

		public void RoundStarted()
		{
			CheckEvent(Event.RoundStarted);
		}

		public void GhostSpawned()
		{
			CheckEvent(Event.GhostSpawned);
		}

		public void PlayerRejoined()
		{
			CheckEvent(Event.PlayerRejoined);
		}

		private void CheckEvent(Event @event)
		{
			var toRemove = new List<IAction>();
			foreach (var action in DicIActionGUI)
			{
				if (action.Key is IActionGUI keyI && keyI.ActionData != null && keyI.ActionData.DisableOnEvent.Contains(@event))
				{
					action.Value[0].Pool();
					toRemove.Add(keyI);
					continue;
				}

				var count = 0;
				if (action.Key is IActionGUIMulti keyIM && keyIM.ActionData != null)
				{
					foreach (var actionData in action.Value)
					{
						if(actionData.ActionData.DisableOnEvent.Contains(@event) == false) continue;
						count++;
						actionData.Pool();

					}

					if (count == action.Value.Count)
					{
						toRemove.Add(keyIM);
					}
				}
			}

			foreach (var remove in toRemove)
			{
				DicIActionGUI.Remove(remove);
			}
		}

		public static void ClearAllActions()
		{
			if(Instance.DicIActionGUI.Count == 0) return;
			for (int i = Instance.DicIActionGUI.Count - 1; i > -1; i--)
			{
				if (Instance.DicIActionGUI.ElementAt(i).Key is IActionGUI iActionGui)
				{
					Hide(iActionGui);
					continue;
				}

				if (Instance.DicIActionGUI.ElementAt(i).Key is IActionGUIMulti iActionGuiMulti)
				{
					foreach (var actionData in iActionGuiMulti.ActionData)
					{
						Hide(iActionGuiMulti, actionData);
					}
				}
			}
		}

		private void OnEnable()
		{
			EventManager.AddHandler(Event.RoundEnded, OnRoundEnd);
			EventManager.AddHandler(Event.PlayerDied, OnPlayerDie);
			EventManager.AddHandler(Event.PlayerSpawned, OnPlayerSpawn);

			EventManager.AddHandler(Event.LoggedOut, LoggedOut);
			EventManager.AddHandler(Event.RoundStarted, RoundStarted);
			EventManager.AddHandler(Event.GhostSpawned, GhostSpawned);
			EventManager.AddHandler(Event.PlayerRejoined, PlayerRejoined);

		}

		private void OnDisable()
		{
			EventManager.RemoveHandler(Event.RoundEnded, OnRoundEnd);
			EventManager.RemoveHandler(Event.PlayerDied, OnPlayerDie);
			EventManager.RemoveHandler(Event.PlayerSpawned, OnPlayerSpawn);

			EventManager.RemoveHandler(Event.LoggedOut, LoggedOut);
			EventManager.RemoveHandler(Event.RoundStarted, RoundStarted);
			EventManager.RemoveHandler(Event.GhostSpawned, GhostSpawned);
			EventManager.RemoveHandler(Event.PlayerRejoined, PlayerRejoined);
		}

		#endregion Events

		private IEnumerator CooldownCountdown(UIAction action, float cooldown)
		{
			while ((cooldown -= Time.deltaTime) > 0)
			{
				if (action == null || action.CooldownNumber == null) yield break;

				action.CooldownNumber.text = Mathf.CeilToInt(cooldown).ToString();
				yield return WaitFor.EndOfFrame;
			}

			action.CooldownNumber.text = default;
		}

		#region IActionGUIMulti

		/// <summary>
		/// Set the action button visibility, locally (clientside)
		/// </summary>
		public static void ToggleLocal(IActionGUIMulti iActionGUIMulti, ActionData actionData, bool show)
		{
			if (show)
			{
				if (Instance.DicIActionGUI.ContainsKey(iActionGUIMulti))
				{
					Logger.Log("iActionGUIMulti Already added", Category.UI);
					return;
				}

				Show(iActionGUIMulti, actionData);
			}
			else
			{
				Hide(iActionGUIMulti, actionData);
			}
		}

		/// <summary>
		/// Set the action button visibility for the given player, with network sync
		/// </summary>
		public static void Toggle(IActionGUIMulti iActionGUIMulti, ActionData actionData, bool show, GameObject recipient)
		{
			SetActionUIMessage.SetAction(recipient, iActionGUIMulti, actionData, show);
		}

		public static void SetSprite(IActionGUIMulti iActionGUIMulti, ActionData actionData, Sprite sprite)
		{
			if (Instance.DicIActionGUI.ContainsKey(iActionGUIMulti))
			{
				var uiActions = Instance.DicIActionGUI[iActionGUIMulti];

				foreach (var action in uiActions)
				{
					if(action.ActionData != actionData) continue;

					action.IconFront.SetSprite(sprite);
				}
			}
			else
			{
				Logger.Log("iActionGUIMulti Not present", Category.UI);
			}
		}

		/// <summary>
		/// Sets the sprite of the action button.
		/// </summary>
		public static void SetSpriteSO(IActionGUIMulti iActionGUIMulti, ActionData actionData, SpriteDataSO sprite, bool networked = true, List<Color> palette = null)
		{
			Debug.Assert(!(sprite.IsPalette && palette == null), "Paletted sprites should never be set without a palette");

			if (Instance.DicIActionGUI.ContainsKey(iActionGUIMulti))
			{
				var uiActions = Instance.DicIActionGUI[iActionGUIMulti];

				foreach (var action in uiActions)
				{
					if(action.ActionData != actionData) continue;

					action.IconFront.SetSpriteSO(sprite, networked: networked);
					action.IconFront.SetPaletteOfCurrentSprite(palette);
				}
			}
			else
			{
				Logger.Log("iActionGUIMulti Not present", Category.UI);
			}
		}

		public static void SetSprite(IActionGUIMulti iActionGUIMulti, ActionData actionData, int Location)
		{
			if (Instance.DicIActionGUI.ContainsKey(iActionGUIMulti))
			{
				var uiActions = Instance.DicIActionGUI[iActionGUIMulti];

				foreach (var action in uiActions)
				{
					if(action.ActionData != actionData) continue;

					action.IconFront.ChangeSprite(Location);
				}
			}
			else
			{

				Logger.Log("iActionGUIMulti Not present", Category.UI);
			}
		}

		public static void SetBackground(IActionGUIMulti iActionGUIMulti, ActionData actionData, int Location)
		{
			if (Instance.DicIActionGUI.ContainsKey(iActionGUIMulti))
			{
				var uiActions = Instance.DicIActionGUI[iActionGUIMulti];

				foreach (var action in uiActions)
				{
					if(action.ActionData != actionData) continue;

					action.IconBackground.ChangeSprite(Location);
				}
			}
			else
			{

				Logger.Log("iActionGUIMulti Not present", Category.UI);
			}
		}

		public static void SetBackground(IActionGUIMulti iActionGUIMulti, ActionData actionData, Sprite sprite)
		{
			if (Instance.DicIActionGUI.ContainsKey(iActionGUIMulti))
			{
				var uiActions = Instance.DicIActionGUI[iActionGUIMulti];

				foreach (var action in uiActions)
				{
					if(action.ActionData != actionData) continue;

					action.IconBackground.SetSprite(sprite);
				}
			}
			else
			{
				Logger.Log("iActionGUIMulti not present!", Category.UI);
			}
		}

		public static void SetCooldownLocal(IActionGUIMulti iActionGUIMulti, ActionData actionData, float cooldown)
		{
			if (Instance.DicIActionGUI.ContainsKey(iActionGUIMulti))
			{
				var uiActions = Instance.DicIActionGUI[iActionGUIMulti];

				foreach (var action in uiActions)
				{
					if(action.ActionData != actionData) continue;

					action.CooldownOpacity.LeanScaleY(0f, cooldown).setFrom(1f);

					if (cooldown > 5)
					{
						Instance.StartCoroutine(Instance.CooldownCountdown(action, cooldown));
					}
				}
			}
			else
			{
				Logger.Log("iActionGUIMulti not present!", Category.UI);
			}
		}

		public static void SetCooldown(IActionGUIMulti iActionGUIMulti, ActionData actionData, float cooldown, GameObject recipient)
		{
			SetActionUIMessage.SetAction(recipient, iActionGUIMulti, actionData, cooldown);
		}

		public static void Show(IActionGUIMulti iActionGUIMulti, ActionData actionData)
		{
			foreach (var actionButton in Instance.DicIActionGUI)
			{
				//Remove old button from list. Don't spawn the same button if it already exists!
				if (actionButton.Key is IActionGUIMulti keyI && keyI == iActionGUIMulti)
				{
					foreach (var action in keyI.ActionData)
					{
						if(actionData != action) continue;

						Hide(keyI, actionData);
					}

					break;
				}
			}

			UIAction _UIAction;
			if (Instance.PooledUIAction.Count > 0)
			{
				_UIAction = Instance.PooledUIAction[0];
				Instance.PooledUIAction.RemoveAt(0);
			}
			else
			{
				_UIAction = Instantiate(Instance.UIAction);
				_UIAction.transform.SetParent(Instance.Panel.transform, false);
			}

			if (Instance.DicIActionGUI.ContainsKey(iActionGUIMulti) == false)
			{
				Instance.DicIActionGUI.Add(iActionGUIMulti, new List<UIAction>());
			}

			Instance.DicIActionGUI[iActionGUIMulti].Add(_UIAction);
			_UIAction.SetUp(iActionGUIMulti, actionData);
		}

		public static void Hide(IActionGUIMulti iActionGUIMulti, ActionData actionData)
		{
			if (Instance.DicIActionGUI.ContainsKey(iActionGUIMulti))
			{
				foreach (var actionButton in Instance.DicIActionGUI)
				{
					//Remove old button from list. Don't spawn the same button if it already exists!
					if (actionButton.Key is IActionGUIMulti keyI && keyI == iActionGUIMulti)
					{
						var count = 0;
						foreach (var action in actionButton.Value)
						{
							if(actionData != action.ActionData) continue;
							count++;

							action.Pool();
							Instance.PooledUIAction.Add(action);
						}

						if (count == actionButton.Value.Count)
						{
							Instance.DicIActionGUI.Remove(iActionGUIMulti);
						}
					}
				}
			}
			else
			{
				Logger.Log("iActionGUI Not present", Category.UI);
			}
		}

		#endregion
	}
}

using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Messages.Server;
using UnityEngine;

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
		public static UIActionManager Instance {
			get {
				if (!uIActionManager)
				{
					uIActionManager = FindObjectOfType<UIActionManager>();
				}

				return uIActionManager;
			}
		}

		/// <summary>
		/// Returns true if an action that is aimable is active.
		/// </summary>
		public bool IsAiming => HasActiveAction && ActiveAction.ActionData.IsAimable;

		public UIAction UIAction;
		public List<UIAction> PooledUIAction = new List<UIAction>();

		public Dictionary<IActionGUI, UIAction> DicIActionGUI = new Dictionary<IActionGUI, UIAction>();

		public UIAction ActiveAction { get; set; }
		public bool HasActiveAction => ActiveAction != null;

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
				if (key.ActionData == actionData)
				{
					actionInstance = key;
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
				var _UIAction = Instance.DicIActionGUI[iActionGUI];
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
				var _UIAction = Instance.DicIActionGUI[iActionGUI];
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
				var _UIAction = Instance.DicIActionGUI[iActionGUI];
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
				var _UIAction = Instance.DicIActionGUI[iActionGUI];
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
				var _UIAction = Instance.DicIActionGUI[iActionGUI];
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
				var _UIAction = Instance.DicIActionGUI[iActionGUI];
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
			Instance.DicIActionGUI[iActionGUI] = _UIAction;
			_UIAction.SetUp(iActionGUI);

		}

		public static void Hide(IActionGUI iActionGUI)
		{
			if (Instance.DicIActionGUI.ContainsKey(iActionGUI))
			{
				var _UIAction = Instance.DicIActionGUI[iActionGUI];
				_UIAction.Pool();
				Instance.PooledUIAction.Add(_UIAction);
				Instance.DicIActionGUI.Remove(iActionGUI);
			}
			else
			{
				Logger.Log("iActionGUI Not present", Category.UI);
			}
		}

		#region Events

		public void AimClicked(Vector3 clickPosition)
		{
			if (HasActiveAction == false) return;
			ActiveAction.RunActionWithClick(clickPosition);
		}

		public void OnRoundEnd()
		{
			foreach (var _Action in DicIActionGUI)
			{
				_Action.Value.Pool();
			}
			DicIActionGUI = new Dictionary<IActionGUI, UIAction>();
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

		public void CheckEvent(Event Event)
		{
			var TOremove = new List<IActionGUI>();
			foreach (var action in DicIActionGUI)
			{
				if (action.Key.ActionData == null)
				{
					Logger.LogWarningFormat("UIAction {0}: action data is null!", Category.UserInput, action.Key + ":" + action.Value);
					continue;
				}
				if (action.Key.ActionData.DisableOnEvent.Contains(Event))
				{
					action.Value.Pool();
					TOremove.Add(action.Key);
				}
			}
			foreach (var Remove in TOremove)
			{
				DicIActionGUI.Remove(Remove);
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
	}
}

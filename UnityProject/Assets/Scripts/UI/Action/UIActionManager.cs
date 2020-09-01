using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

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
	public static UIActionManager Instance
	{
		get
		{
			if (!uIActionManager)
			{
				uIActionManager = FindObjectOfType<UIActionManager>();
			}

			return uIActionManager;
		}
	}


	public UIAction UIAction;
	public List<UIAction> PooledUIAction = new List<UIAction>();

	public Dictionary<IActionGUI, UIAction> DicIActionGUI = new Dictionary<IActionGUI, UIAction>();


	/// <summary>
	/// Toggle it locally (clientside)
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
		else {
			Hide(iActionGUI);
		}
	}

	/// <summary>
	/// Toggle with network sync
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
		else {
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
		else {

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
		else {

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
		else {
			Logger.Log("iActionGUI Not present", Category.UI);
		}
	}

	public static void Show(IActionGUI iActionGUI)
	{
		UIAction _UIAction;
		if (Instance.PooledUIAction.Count > 0)
		{
			_UIAction = Instance.PooledUIAction[0];
			Instance.PooledUIAction.RemoveAt(0);
		}
		else {
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
		else {
			Logger.Log("iActionGUI Not present", Category.UI);
		}
	}

	public void OnRoundEnd()
	{
		foreach (var _Action in DicIActionGUI) {
			_Action.Value.Pool();
		}
		DicIActionGUI = new Dictionary<IActionGUI, UIAction>();
	}

	public void OnPlayerDie()
	{
		CheckEvent(EVENT.PlayerDied);
	}

	public void OnPlayerSpawn()
	{
		CheckEvent(EVENT.PlayerSpawned);
	}

	public void LoggedOut()
	{
		CheckEvent(EVENT.LoggedOut);
	}


	public void RoundStarted()
	{
		CheckEvent(EVENT.RoundStarted);
	}


	public void GhostSpawned()
	{
		CheckEvent(EVENT.GhostSpawned);
	}


	public void PlayerRejoined()
	{
		CheckEvent(EVENT.PlayerRejoined);
	}

	public void CheckEvent(EVENT Event)
	{
		var TOremove = new List<IActionGUI>();
		foreach (var action in DicIActionGUI)
		{
			if (action.Key.ActionData == null)
			{
				Logger.LogWarningFormat("UIAction {0}: action data is null!", Category.UIAction, action.Key+":"+action.Value );
				continue;
			}
			if (action.Key.ActionData.DisableOnEvent.Contains(Event)) {
				action.Value.Pool();
				TOremove.Add(action.Key);
			}
		}
		foreach (var Remove in TOremove) {
			DicIActionGUI.Remove(Remove);
		}
	}




	private void OnEnable()
	{
		EventManager.AddHandler(EVENT.RoundEnded, OnRoundEnd);
		EventManager.AddHandler(EVENT.PlayerDied, OnPlayerDie);
		EventManager.AddHandler(EVENT.PlayerSpawned, OnPlayerSpawn);

		EventManager.AddHandler(EVENT.LoggedOut, LoggedOut);
		EventManager.AddHandler(EVENT.RoundStarted, RoundStarted);
		EventManager.AddHandler(EVENT.GhostSpawned, GhostSpawned);
		EventManager.AddHandler(EVENT.PlayerRejoined, PlayerRejoined);

	}

	private void OnDisable()
	{
		EventManager.RemoveHandler(EVENT.RoundEnded, OnRoundEnd);
		EventManager.RemoveHandler(EVENT.PlayerDied, OnPlayerDie);
		EventManager.RemoveHandler(EVENT.PlayerSpawned, OnPlayerSpawn);

		EventManager.RemoveHandler(EVENT.LoggedOut, LoggedOut);
		EventManager.RemoveHandler(EVENT.RoundStarted, RoundStarted);
		EventManager.RemoveHandler(EVENT.GhostSpawned, GhostSpawned);
		EventManager.RemoveHandler(EVENT.PlayerRejoined, PlayerRejoined);
	}

}

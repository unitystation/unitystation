using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Messages.Server;
using Shared.Util;
using UnityEngine;
using UnityEngine.SceneManagement;
using Util;

namespace UI.Action
{
	/// <summary>
	/// Used to set the action From the client
	/// </summary>
	public class UIActionManager : MonoBehaviour
	{

		public class ActionAndData
		{
			public ActionData ActionData;
			public string ID;
		}


		public Dictionary<Mind, List<IActionGUI>> ActivePlayerActions = new Dictionary<Mind, List<IActionGUI>>();
		public Dictionary<IActionGUI, Mind> IActionGUIToMind = new Dictionary<IActionGUI, Mind>();

		public Dictionary<IActionGUI, string> IActionGUIToID = new Dictionary<IActionGUI, string>();
		public Dictionary<IActionGUI, string> ClientIActionGUIToID = new Dictionary<IActionGUI, string>();


		public Dictionary<Mind, Dictionary<IActionGUIMulti, List<ActionData>>> MultiActivePlayerActions = new Dictionary<Mind, Dictionary<IActionGUIMulti, List<ActionData>>>();

		public Dictionary<IActionGUIMulti, Mind> MultiIActionGUIToMind = new Dictionary<IActionGUIMulti, Mind>();

		public Dictionary<IActionGUIMulti, Dictionary<ActionData, string>> MultiIActionGUIToID = new Dictionary<IActionGUIMulti, Dictionary<ActionData, string>>();

		public Dictionary<IActionGUIMulti, Dictionary<ActionData, string>> ClientMultiIActionGUIToID = new Dictionary<IActionGUIMulti, Dictionary<ActionData, string>>();


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


		public void UpdatePlayer(Mind mind)
		{
			if (ActivePlayerActions.ContainsKey(mind))
			{
				foreach (var IactionGUI in ActivePlayerActions[mind])
				{
					Show("", IactionGUI, mind);
				}
			}


			if (MultiActivePlayerActions.ContainsKey(mind))
			{
				foreach (var MultiIactionGUI in MultiActivePlayerActions[mind])
				{
					foreach (var AD in MultiIactionGUI.Value)
					{
						ShowMulti("",mind, MultiIactionGUI.Key, AD);
					}
				}
			}

			SpriteHandlerManager.Instance.UpdateSpecialNewPlayer(mind.CurrentPlayScript.connectionToClient);
		}


		#region IActionGUI

		/// <summary>
		/// Set the action button visibility, locally (clientside)
		/// </summary>
		public static void ToggleServer(Mind RelatedMind, IActionGUI iActionGUI, bool show)
		{
			Instance.InstantToggleServer(RelatedMind, iActionGUI, show);
		}

		private void InstantToggleServer(Mind RelatedMind, IActionGUI iActionGUI, bool show)
		{
			if (CustomNetworkManager.IsServer == false) return;
			if (ActivePlayerActions.ContainsKey(RelatedMind) == false)
			{
				ActivePlayerActions[RelatedMind] = new List<IActionGUI>();
			}


			if (show)
			{
				if (ActivePlayerActions[RelatedMind].Contains(iActionGUI))
				{
					Logger.LogError("iActionGUI Already present on mind");
					return;
				}

				if (Instance.DicIActionGUI.ContainsKey(iActionGUI))
				{
					Logger.Log("iActionGUI Already added", Category.UI);
					return;
				}

				var IDString = (RelatedMind.GetHashCode().ToString() + iActionGUI.GetHashCode().ToString());

				SpriteHandlerManager.RegisterSpecialHandler(IDString+"F"); //Front icon
				SpriteHandlerManager.RegisterSpecialHandler(IDString+"B"); //back icon
				IActionGUIToID[iActionGUI] = IDString;
				IActionGUIToMind[iActionGUI] = RelatedMind;
				ActivePlayerActions[RelatedMind].Add(iActionGUI);

				Show(IDString, iActionGUI, RelatedMind);
			}
			else
			{
				if (ActivePlayerActions[RelatedMind].Contains(iActionGUI) == false)
				{
					Logger.LogError("iActionGUI Not present on mind");
					return;
				}

				var IDString = IActionGUIToID[iActionGUI];
				SpriteHandlerManager.UnRegisterSpecialHandler(IDString+"F"); //Front icon
				SpriteHandlerManager.UnRegisterSpecialHandler(IDString+"B"); //back icon
				Instance.IActionGUIToID.Remove(iActionGUI);


				IActionGUIToID.Remove(iActionGUI);
				ActivePlayerActions[RelatedMind].Remove(iActionGUI);
				IActionGUIToMind.Remove(iActionGUI);
				Hide(iActionGUI, RelatedMind);
			}
		}


		public static void ToggleClient(IActionGUI iActionGUI, bool show, string ID) //Internal use only!! reeee
		{
			if (show)
			{
				if (Instance.DicIActionGUI.ContainsKey(iActionGUI))
				{
					Logger.Log("iActionGUI Already added", Category.UI);
					return;
				}

				Show(ID, iActionGUI, null);
			}
			else
			{
				Hide(iActionGUI, null);
			}
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

		public static void SetClientSpriteSO(IActionGUI iActionGUI, SpriteDataSO sprite,
			List<Color> palette = null)
		{
			Debug.Assert(!(sprite.IsPalette && palette == null),
				"Paletted sprites should never be set without a palette");

			if (Instance.DicIActionGUI.ContainsKey(iActionGUI))
			{
				var _UIAction = Instance.DicIActionGUI[iActionGUI][0];
				_UIAction.IconFront.SetSpriteSO(sprite, networked: false);
				_UIAction.IconFront.SetPaletteOfCurrentSprite(palette);
			}
			else
			{
				Logger.Log("iActionGUI Not present", Category.UI);
			}
		}

		/// <summary>
		/// Sets the sprite of the action button.
		/// </summary>
		public static void SetServerSpriteSO(IActionGUI iActionGUI, SpriteDataSO sprite,
			List<Color> palette = null)
		{
			if (Instance.IActionGUIToMind.ContainsKey(iActionGUI) == false)
			{
				Logger.LogError($"iActionGUI {iActionGUI} Not present To any mind");
				return;
			}

			//Send message
			SetActionUIMessage.SetSpriteSO(Instance.IActionGUIToID[iActionGUI], Instance.IActionGUIToMind[iActionGUI].body.gameObject, iActionGUI, sprite,
				palette);
		}

		public static void SetClientSprite(IActionGUI iActionGUI, int Location)
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

		public static void SetServerSprite(IActionGUI iActionGUI, int Location)
		{
			if (Instance.IActionGUIToMind.ContainsKey(iActionGUI) == false)
			{
				Logger.LogError($"iActionGUI {iActionGUI} Not present To any mind");
				return;
			}

			SetActionUIMessage.SetSprite(Instance.IActionGUIToID[iActionGUI], Instance.IActionGUIToMind[iActionGUI].body.gameObject, iActionGUI, Location);
		}


		public static void SetClientBackground(IActionGUI iActionGUI, int Location)
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

		public static void SetServerBackground(IActionGUI iActionGUI, int Location)
		{
			if (Instance.IActionGUIToMind.ContainsKey(iActionGUI) == false)
			{
				Logger.LogError($"iActionGUI {iActionGUI} Not present To any mind");
				return;
			}

			SetActionUIMessage.SetBackgroundSprite(Instance.IActionGUIToID[iActionGUI], Instance.IActionGUIToMind[iActionGUI].body.gameObject, iActionGUI,
				Location);
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
			SetActionUIMessage.SetAction(Instance.IActionGUIToID[iActionGUI], recipient, iActionGUI, cooldown);
		}

		private static void Show(string ID ,IActionGUI iActionGUI, Mind RelatedMind)
		{
			if (CustomNetworkManager.IsServer && RelatedMind != null)
			{
				//Send message
				SetActionUIMessage.SetAction(Instance.IActionGUIToID[iActionGUI], RelatedMind.CurrentPlayScript.gameObject, iActionGUI, true);
			}

			if (RelatedMind == null)
			{
				foreach (var actionButton in Instance.DicIActionGUI)
				{
					//Remove old button from list. Don't spawn the same button if it already exists!
					if (actionButton.Key is IActionGUI keyI &&
					    actionButton.Value[0].ActionData == iActionGUI.ActionData)
					{
						Hide(keyI, null);
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

				Instance.ClientIActionGUIToID[iActionGUI] = ID;
				SpriteHandlerManager.RegisterSpecialHandler(ID+"F", _UIAction.IconFront); //Front icon
				SpriteHandlerManager.RegisterSpecialHandler(ID+"B", _UIAction.IconBackground); //back icon

				if (Instance.DicIActionGUI.ContainsKey(iActionGUI) == false)
				{
					Instance.DicIActionGUI.Add(iActionGUI, new List<UIAction>());
				}

				Instance.DicIActionGUI[iActionGUI].Add(_UIAction);
				_UIAction.SetUp(iActionGUI);
			}
		}

		private static void Hide( IActionGUI iAction, Mind RelatedMind)
		{
			if (CustomNetworkManager.IsServer && RelatedMind != null)
			{
				//Send message
				SetActionUIMessage.SetAction("", RelatedMind.CurrentPlayScript.gameObject, iAction, false);
			}

			if (RelatedMind == null)
			{
				//Client stuff
				if (Instance.DicIActionGUI.ContainsKey(iAction))
				{
					var _UIAction = Instance.DicIActionGUI[iAction][0];
					var ID = Instance.ClientIActionGUIToID[iAction];
					SpriteHandlerManager.UnRegisterSpecialHandler(ID+"F"); //Front icon
					SpriteHandlerManager.UnRegisterSpecialHandler(ID+"B"); //back icon

					_UIAction.Pool();
					Instance.PooledUIAction.Add(_UIAction);
					Instance.DicIActionGUI.Remove(iAction);
				}
				else
				{
					Logger.Log("iActionGUI Not present", Category.UI);
				}
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
			ClearAllActionsClient();
			RequestIconsUIActionRefresh.Send();
		}

		public static void ClearAllActionsServer(Scene oldScene, Scene newScene)
		{
			Instance.IActionGUIToMind.Clear();
			Instance.ActivePlayerActions.Clear();
			Instance.MultiIActionGUIToMind.Clear();
			Instance.MultiActivePlayerActions.Clear();

			Instance.IActionGUIToID.Clear();
			Instance.MultiIActionGUIToID.Clear();
		}

		public static void ClearAllActionsClient()
		{
			if (Instance.DicIActionGUI.Count == 0) return;
			for (int i = Instance.DicIActionGUI.Count - 1; i > -1; i--)
			{
				if (Instance.DicIActionGUI.ElementAt(i).Key is IActionGUI iActionGui)
				{
					Hide(iActionGui, null);
					continue;
				}

				if (Instance.DicIActionGUI.ElementAt(i).Key is IActionGUIMulti iActionGuiMulti)
				{
					foreach (var actionData in iActionGuiMulti.ActionData)
					{
						HideMulti(null, iActionGuiMulti, actionData);
					}
				}
			}
		}

		private void OnEnable()
		{
			SceneManager.activeSceneChanged += ClearAllActionsServer;
			SceneManager.activeSceneChanged -= ClearAllActionsServer;

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
		/// Set the action button visibility and syncs to client
		/// </summary>
		public static void ToggleMultiServer(Mind relatedMind, IActionGUIMulti iActionGUIMulti, ActionData actionData,
			bool show)
		{
			if(relatedMind == null) return;

			Instance.InstantMultiToggleServer(relatedMind, iActionGUIMulti, actionData, show);
		}

		private void InstantMultiToggleServer(Mind relatedMind, IActionGUIMulti iActionGUIMulti, ActionData actionData,
			bool show)
		{
			if (CustomNetworkManager.IsServer == false) return;

			if (MultiActivePlayerActions.ContainsKey(relatedMind) == false)
			{
				MultiActivePlayerActions[relatedMind] = new Dictionary<IActionGUIMulti, List<ActionData>>();
			}

			if (show)
			{
				MultiActivePlayerActions[relatedMind][iActionGUIMulti] = new List<ActionData>();


				if (MultiActivePlayerActions[relatedMind][iActionGUIMulti].Contains(actionData))
				{
					Logger.LogError("ActionData Already present on mind");
					return;
				}


				if (Instance.DicIActionGUI.ContainsKey(iActionGUIMulti))
				{
					Logger.Log("iActionGUIMulti Already added", Category.UI);
					return;
				}

				var IDString = (relatedMind.GetHashCode().ToString() + iActionGUIMulti.GetHashCode().ToString() + actionData.GetHashCode().ToString());

				SpriteHandlerManager.RegisterSpecialHandler(IDString+"F"); //Front icon
				SpriteHandlerManager.RegisterSpecialHandler(IDString+"B"); //back icon

				if (MultiIActionGUIToID.ContainsKey(iActionGUIMulti) == false)
				{
					MultiIActionGUIToID[iActionGUIMulti] =
						new SerializableDictionaryBase.Dictionary<ActionData, string>();
				}
				MultiIActionGUIToID[iActionGUIMulti][actionData] = IDString;

				MultiIActionGUIToMind[iActionGUIMulti] = relatedMind;
				MultiActivePlayerActions[relatedMind][iActionGUIMulti].Add(actionData);
				ShowMulti(IDString, relatedMind, iActionGUIMulti, actionData);
			}
			else
			{
				if (MultiActivePlayerActions[relatedMind].ContainsKey(iActionGUIMulti) == false)
				{
					Logger.LogError("iActionGUIMulti Not present on mind");
					return;
				}

				if (MultiActivePlayerActions[relatedMind][iActionGUIMulti].Contains(actionData) == false)
				{
					Logger.LogError("actionData Not present on mind");
					return;
				}

				var idString = MultiIActionGUIToID[iActionGUIMulti][actionData];
				SpriteHandlerManager.UnRegisterSpecialHandler(idString+"F"); //Front icon
				SpriteHandlerManager.UnRegisterSpecialHandler(idString+"B"); //back icon
				MultiIActionGUIToID[iActionGUIMulti].Remove(actionData);

				MultiActivePlayerActions[relatedMind][iActionGUIMulti].Remove(actionData);
				MultiIActionGUIToMind.Remove(iActionGUIMulti);
				HideMulti(relatedMind, iActionGUIMulti, actionData);
			}
		}


		public static void MultiToggleClient(IActionGUIMulti iActionGUIMulti, ActionData actionData, bool show, string ID)
		{
			if (show)
			{
				if (Instance.DicIActionGUI.ContainsKey(iActionGUIMulti))
				{
					Logger.Log("iActionGUIMulti Already added", Category.UI);
					return;
				}

				ShowMulti(ID, null, iActionGUIMulti, actionData);
			}
			else
			{
				HideMulti( null, iActionGUIMulti, actionData);
			}
		}

		/// <summary>
		/// Sets the sprite of the action button.
		/// </summary>
		public static void SetClientMultiSpriteSO(IActionGUIMulti iActionGUIMulti, ActionData actionData,
			SpriteDataSO sprite, List<Color> palette = null)
		{
			Debug.Assert(!(sprite.IsPalette && palette == null),
				"Paletted sprites should never be set without a palette");

			if (Instance.DicIActionGUI.ContainsKey(iActionGUIMulti))
			{
				var uiActions = Instance.DicIActionGUI[iActionGUIMulti];

				foreach (var action in uiActions)
				{
					if (action.ActionData != actionData) continue;

					action.IconFront.SetSpriteSO(sprite, networked: false);
					action.IconFront.SetPaletteOfCurrentSprite(palette);
				}
			}
			else
			{
				Logger.Log("iActionGUIMulti Not present", Category.UI);
			}
		}

		public static void SetServerMultiSpriteSO(IActionGUIMulti iActionGUIMulti, ActionData actionData,
			SpriteDataSO sprite, List<Color> palette = null)
		{
			if (Instance.MultiIActionGUIToMind.ContainsKey(iActionGUIMulti) == false)
			{
				Logger.LogError($"iActionGUI {iActionGUIMulti} Not present To any mind");
				return;
			}

			SetActionUIMessage.SetMultiSpriteSO(Instance.MultiIActionGUIToID[iActionGUIMulti][actionData],Instance.MultiIActionGUIToMind[iActionGUIMulti].body.gameObject,
				iActionGUIMulti, actionData, sprite, palette);
		}

		public static void SetClientMultiSprite(IActionGUIMulti iActionGUIMulti, ActionData actionData, int Location)
		{
			if (Instance.DicIActionGUI.ContainsKey(iActionGUIMulti))
			{
				var uiActions = Instance.DicIActionGUI[iActionGUIMulti];

				foreach (var action in uiActions)
				{
					if (action.ActionData != actionData) continue;

					action.IconFront.ChangeSprite(Location);
				}
			}
			else
			{
				Logger.Log("iActionGUIMulti Not present", Category.UI);
			}
		}

		public static void SetServerMultiSprite(IActionGUIMulti iActionGUIMulti, ActionData actionData, int Location)
		{
			if (Instance.MultiIActionGUIToMind.ContainsKey(iActionGUIMulti) == false)
			{
				Logger.LogError($"iActionGUI {iActionGUIMulti} Not present To any mind");
				return;
			}

			SetActionUIMessage.SetMultiSprite(Instance.MultiIActionGUIToID[iActionGUIMulti][actionData], Instance.MultiIActionGUIToMind[iActionGUIMulti].body.gameObject,
				iActionGUIMulti, actionData, Location);
		}


		public static void SetClientMultiBackground(IActionGUIMulti iActionGUIMulti, ActionData actionData,
			int Location)
		{
			if (Instance.DicIActionGUI.ContainsKey(iActionGUIMulti))
			{
				var uiActions = Instance.DicIActionGUI[iActionGUIMulti];

				foreach (var action in uiActions)
				{
					if (action.ActionData != actionData) continue;

					action.IconBackground.ChangeSprite(Location);
				}
			}
			else
			{
				Logger.Log("iActionGUIMulti Not present", Category.UI);
			}
		}

		public static void SetServerMultiBackground(IActionGUIMulti iActionGUIMulti, ActionData actionData,
			int Location)
		{
			if (Instance.MultiIActionGUIToMind.ContainsKey(iActionGUIMulti) == false)
			{
				Logger.LogError($"iActionGUI {iActionGUIMulti} Not present To any mind");
				return;
			}

			SetActionUIMessage.SetMultiBackgroundSprite(Instance.MultiIActionGUIToID[iActionGUIMulti][actionData], Instance.MultiIActionGUIToMind[iActionGUIMulti].body.gameObject,
				iActionGUIMulti, actionData, Location);
		}


		public static void SetCooldownMultiLocal(IActionGUIMulti iActionGUIMulti, ActionData actionData, float cooldown)
		{
			if (Instance.DicIActionGUI.ContainsKey(iActionGUIMulti))
			{
				var uiActions = Instance.DicIActionGUI[iActionGUIMulti];

				foreach (var action in uiActions)
				{
					if (action.ActionData != actionData) continue;

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

		public static void SetMultiCooldown(IActionGUIMulti iActionGUIMulti, ActionData actionData, float cooldown,
			GameObject recipient)
		{
			SetActionUIMessage.SetMultiAction(Instance.MultiIActionGUIToID[iActionGUIMulti][actionData],recipient, iActionGUIMulti, actionData, cooldown);
		}

		private static void ShowMulti(string ID, Mind RelatedMind, IActionGUIMulti iActionGUIMulti, ActionData actionData)
		{
			if (CustomNetworkManager.IsServer && RelatedMind != null)
			{
				//Send message
				SetActionUIMessage.SetMultiAction(Instance.MultiIActionGUIToID[iActionGUIMulti][actionData],RelatedMind.CurrentPlayScript.gameObject, iActionGUIMulti, actionData,
					true);
			}

			if (RelatedMind == null)
			{
				HideMulti(null, iActionGUIMulti, actionData);

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

				if (Instance.ClientMultiIActionGUIToID.ContainsKey(iActionGUIMulti) == false)
				{
					Instance.ClientMultiIActionGUIToID[iActionGUIMulti] = new Dictionary<ActionData, string>();
				}

				Instance.ClientMultiIActionGUIToID[iActionGUIMulti][actionData] = ID;
				SpriteHandlerManager.RegisterSpecialHandler(ID+"F", _UIAction.IconFront); //Front icon
				SpriteHandlerManager.RegisterSpecialHandler(ID+"B", _UIAction.IconBackground); //back icon

				_UIAction.SetUp(iActionGUIMulti, actionData);
			}
		}

		private static void HideMulti( Mind RelatedMind, IActionGUIMulti iActionGUIMulti, ActionData actionData)
		{
			if (CustomNetworkManager.IsServer && RelatedMind != null)
			{
				//Send message
				SetActionUIMessage.SetMultiAction("", RelatedMind.CurrentPlayScript.gameObject, iActionGUIMulti, actionData,
					false);
			}

			if (RelatedMind == null)
			{
				if (Instance.DicIActionGUI.ContainsKey(iActionGUIMulti))
				{
					var toRemove = new List<IAction>();
					foreach (var actionButton in Instance.DicIActionGUI)
					{
						//Remove old button from list. Don't spawn the same button if it already exists!
						if (actionButton.Key is IActionGUIMulti keyI && keyI == iActionGUIMulti)
						{
							var count = 0;
							foreach (var action in actionButton.Value)
							{
								if (actionData != action.ActionData) continue;
								count++;


								var ID = Instance.ClientMultiIActionGUIToID[iActionGUIMulti][actionData];
								SpriteHandlerManager.UnRegisterSpecialHandler(ID+"F"); //Front icon
								SpriteHandlerManager.UnRegisterSpecialHandler(ID+"B"); //back icon
								Instance.ClientMultiIActionGUIToID[iActionGUIMulti].Remove(actionData);


								action.Pool();
								Instance.PooledUIAction.Add(action);
							}

							if (count == actionButton.Value.Count)
							{
								toRemove.Add(iActionGUIMulti);
							}
						}
					}

					foreach (var remove in toRemove)
					{
						Instance.DicIActionGUI.Remove(remove);
					}
				}
				else
				{
					Logger.Log("iActionGUI Not present", Category.UI);
				}
			}
		}

		#endregion
	}
}
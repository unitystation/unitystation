using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Logs;
using Messages.Server;
using Mirror;
using Shared.Util;
using UI.Action;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Core.Action
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


		private Dictionary<GameObject, List<IActionGUI>> ActivePlayerActions = new Dictionary<GameObject, List<IActionGUI>>();
		private Dictionary<IActionGUI, GameObject> IActionGUIToMind = new Dictionary<IActionGUI, GameObject>();

		private Dictionary<IActionGUI, string> IActionGUIToID = new Dictionary<IActionGUI, string>();
		private Dictionary<IActionGUI, string> ClientIActionGUIToID = new Dictionary<IActionGUI, string>();


		public Dictionary<GameObject, Dictionary<IActionGUIMulti, List<ActionData>>> MultiActivePlayerActions = new Dictionary<GameObject, Dictionary<IActionGUIMulti, List<ActionData>>>();

		public Dictionary<IActionGUIMulti, GameObject> MultiIActionGUIToMind = new Dictionary<IActionGUIMulti, GameObject>();

		public Dictionary<IActionGUIMulti, Dictionary<ActionData, string>> MultiIActionGUIToID = new Dictionary<IActionGUIMulti, Dictionary<ActionData, string>>();

		public Dictionary<IActionGUIMulti, Dictionary<ActionData, string>> ClientMultiIActionGUIToID = new Dictionary<IActionGUIMulti, Dictionary<ActionData, string>>();


		public GameObject Panel;
		public GameObject TooltipPrefab;

		public void Clear()
		{
			Debug.Log("removed " + CleanupUtil.RidDictionaryOfDeadElements(IActionGUIToID, (u,k)=> u as MonoBehaviour != null) + " from UIActionManager.IActionGUIToID");
			Debug.Log("removed " + CleanupUtil.RidDictionaryOfDeadElements(ClientIActionGUIToID, (u, k) => u as MonoBehaviour != null) + " from UIActionManager.ClientIActionGUIToID");
			Debug.Log("removed " + CleanupUtil.RidDictionaryOfDeadElements(MultiIActionGUIToMind, (u, k) => u as MonoBehaviour != null) + " from UIActionManager.MultiIActionGUIToMind");
			Debug.Log("removed " + CleanupUtil.RidDictionaryOfDeadElements(MultiIActionGUIToID, (u, k) => u as MonoBehaviour != null) + " from UIActionManager.MultiIActionGUIToID");
			Debug.Log("removed " + CleanupUtil.RidDictionaryOfDeadElements(ClientMultiIActionGUIToID, (u, k) => u as MonoBehaviour != null) + " from UIActionManager.ClientMultiIActionGUIToID");
			Debug.Log("removed " + CleanupUtil.RidDictionaryOfDeadElements(ActivePlayerActions, (u, k) => u != null) + " from UIActionManager.ActivePlayerActions");
			Debug.Log("removed " + CleanupUtil.RidDictionaryOfDeadElements(Instance.DicIActionGUI, (u, k) => u as MonoBehaviour != null) + " from Instance.DicIActionGUI");
			Debug.Log("removed " + CleanupUtil.RidDictionaryOfDeadElements(Instance.IActionGUIToMind, (u, k) => u as MonoBehaviour != null) + " from Instance.IActionGUIToMind");

			{
				int internals = 0;

				foreach (var a in ActivePlayerActions)
				{
					internals += CleanupUtil.RidListOfDeadElements(a.Value, u => u as MonoBehaviour);
				}

				Debug.Log("removed " + internals + " from internals of UIActionManager.ActivePlayerActions");
			}

			{
				int internals = 0;

				foreach (var a in Instance.DicIActionGUI)
				{
					internals += CleanupUtil.RidListOfDeadElements(a.Value);
				}

				Debug.Log("removed " + internals + " from internals of Instance.DicIActionGUI");
			}

		}

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


		public void UpdatePlayer(GameObject Body, NetworkConnection requestedBy)
		{
			if (ActivePlayerActions.ContainsKey(Body))
			{
				foreach (var IactionGUI in ActivePlayerActions[Body])
				{
					Show("", IactionGUI, Body);
				}
			}


			if (MultiActivePlayerActions.ContainsKey(Body))
			{
				foreach (var MultiIactionGUI in MultiActivePlayerActions[Body])
				{
					foreach (var AD in MultiIactionGUI.Value)
					{
						ShowMulti("",Body, MultiIactionGUI.Key, AD);
					}
				}
			}

			SpriteHandlerManager.Instance.UpdateSpecialNewPlayer(requestedBy);
		}


		#region IActionGUI

		/// <summary>
		/// Set the action button visibility
		/// </summary>
		public static void ToggleServer(GameObject body, IActionGUI iActionGUI, bool show)
		{
			Instance.InstantToggleServer(body, iActionGUI, show);
		}

		private void InstantToggleServer(GameObject Body, IActionGUI iActionGUI, bool show)
		{
			if (CustomNetworkManager.IsServer == false) return;
			if (ActivePlayerActions.ContainsKey(Body) == false)
			{
				ActivePlayerActions[Body] = new List<IActionGUI>();
			}


			if (show)
			{
				if (ActivePlayerActions[Body].Contains(iActionGUI))
				{
					Loggy.LogError("iActionGUI Already present on mind");
					return;
				}

				var IDString = (Body.GetHashCode().ToString() + iActionGUI.GetHashCode().ToString());

				SpriteHandlerManager.RegisterSpecialHandler(IDString+"F"); //Front icon
				SpriteHandlerManager.RegisterSpecialHandler(IDString+"B"); //back icon
				IActionGUIToID[iActionGUI] = IDString;
				IActionGUIToMind[iActionGUI] = Body;
				ActivePlayerActions[Body].Add(iActionGUI);

				Show(IDString, iActionGUI, Body);
			}
			else
			{
				if (ActivePlayerActions[Body].Contains(iActionGUI) == false)
				{
					Loggy.LogError($"iActionGUI {iActionGUI?.ActionData.OrNull()?.Name}, not present on mind");
					return;
				}

				var IDString = IActionGUIToID[iActionGUI];
				SpriteHandlerManager.UnRegisterSpecialHandler(IDString+"F"); //Front icon
				SpriteHandlerManager.UnRegisterSpecialHandler(IDString+"B"); //back icon
				Instance.IActionGUIToID.Remove(iActionGUI);


				IActionGUIToID.Remove(iActionGUI);
				ActivePlayerActions[Body].Remove(iActionGUI);
				IActionGUIToMind.Remove(iActionGUI);
				Hide(iActionGUI, Body);
			}
		}


		public static void ToggleClient(IActionGUI iActionGUI, bool show, string ID) //Internal use only!! reeee
		{
			if (show)
			{
				if (Instance.DicIActionGUI.ContainsKey(iActionGUI))
				{
					Loggy.Log("iActionGUI Already added", Category.UI);
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
				Loggy.Log("iActionGUI Not present", Category.UI);
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
				Loggy.LogError($"iActionGUI {iActionGUI} Not present To any mind");
				return;
			}

			//Send message
			SetActionUIMessage.SetSpriteSO(Instance.IActionGUIToID[iActionGUI], Instance.IActionGUIToMind[iActionGUI], iActionGUI, sprite,
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
				Loggy.Log("iActionGUI Not present", Category.UI);
			}
		}

		public static void SetServerSprite(IActionGUI iActionGUI, int Location)
		{
			if (Instance.IActionGUIToMind.ContainsKey(iActionGUI) == false)
			{
				Loggy.LogError($"iActionGUI {iActionGUI} Not present To any mind");
				return;
			}

			SetActionUIMessage.SetSprite(Instance.IActionGUIToID[iActionGUI], Instance.IActionGUIToMind[iActionGUI], iActionGUI, Location);
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
				Loggy.Log("iActionGUI Not present", Category.UI);
			}
		}

		public static void SetServerBackground(IActionGUI iActionGUI, int Location)
		{
			if (Instance.IActionGUIToMind.ContainsKey(iActionGUI) == false)
			{
				Loggy.LogError($"iActionGUI {iActionGUI} Not present To any mind");
				return;
			}

			SetActionUIMessage.SetBackgroundSprite(Instance.IActionGUIToID[iActionGUI], Instance.IActionGUIToMind[iActionGUI], iActionGUI,
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
				Loggy.Log("iActionGUI not present!", Category.UI);
			}
		}

		public static void SetCooldown(IActionGUI iActionGUI, float cooldown, GameObject recipient)
		{
			SetActionUIMessage.SetAction(Instance.IActionGUIToID[iActionGUI], recipient, iActionGUI, cooldown);
		}

		private static void Show(string ID ,IActionGUI iActionGUI, GameObject body)
		{

			if (CustomNetworkManager.IsServer && body != null)
			{
				//Send message
				SetActionUIMessage.SetAction(Instance.IActionGUIToID[iActionGUI], body, iActionGUI, true);
			}

			if (body == null)
			{
				foreach (var actionButton in Instance.DicIActionGUI)
				{
					//Remove old button from list. Don't spawn the same button if it already exists!
					if (actionButton.Key is IActionGUI keyI &&
					    actionButton.Value[0].iAction == iActionGUI)
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
				SpriteHandlerManager.RegisterSpecialHandler(ID + "F", _UIAction.IconFront); //Front icon
				SpriteHandlerManager.RegisterSpecialHandler(ID + "B", _UIAction.IconBackground); //back icon

				if (Instance.DicIActionGUI.ContainsKey(iActionGUI) == false)
				{
					Instance.DicIActionGUI.Add(iActionGUI, new List<UIAction>());
				}

				Instance.DicIActionGUI[iActionGUI].Add(_UIAction);
				_UIAction.SetUp(iActionGUI);
			}
		}

		private static void Hide( IActionGUI iAction, GameObject Body)
		{
			if (CustomNetworkManager.IsServer && Body != null)
			{
				//Send message
				SetActionUIMessage.SetAction("", Body, iAction, false);
			}

			if (Body == null)
			{
				//Client stuff
				if (Instance.DicIActionGUI.ContainsKey(iAction) && Instance.ClientIActionGUIToID.ContainsKey(iAction))
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
					Loggy.Log("iActionGUI Not present", Category.UI);
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
			if (this == null)
			{
				Debug.LogError("damn hell");
			}

			foreach (var _Actions in DicIActionGUI)
			{
				foreach (var _Action in _Actions.Value)
				{
					_Action.Pool();
				}
			}

			DicIActionGUI = new Dictionary<IAction, List<UIAction>>();
		}

		public static void ClearAllActionsServer()
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
			if (Instance == null) return;
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
			EventManager.AddHandler(Event.RoundEnded, OnRoundEnd);

		}

		private void OnDisable()
		{
			EventManager.RemoveHandler(Event.RoundEnded, OnRoundEnd);
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
		public static void ToggleMultiServer(GameObject body, IActionGUIMulti iActionGUIMulti, ActionData actionData,
			bool show)
		{
			if(body == null) return;

			Instance.InstantMultiToggleServer(body, iActionGUIMulti, actionData, show);
		}

		private void InstantMultiToggleServer(GameObject body, IActionGUIMulti iActionGUIMulti, ActionData actionData,
			bool show)
		{
			if (CustomNetworkManager.IsServer == false) return;

			if (MultiActivePlayerActions.ContainsKey(body) == false)
			{
				MultiActivePlayerActions[body] = new Dictionary<IActionGUIMulti, List<ActionData>>();
			}

			if (MultiActivePlayerActions[body].ContainsKey(iActionGUIMulti) == false)
			{
				MultiActivePlayerActions[body][iActionGUIMulti] = new List<ActionData>();
			}

			if (show)
			{
				if (MultiActivePlayerActions[body][iActionGUIMulti].Contains(actionData))
				{
					Loggy.LogError($"ActionData: {actionData.OrNull()?.Name}, already present on mind");
					return;
				}

				var idString = $"{body.GetHashCode()}{iActionGUIMulti.GetHashCode()}{actionData.GetHashCode()}";

				SpriteHandlerManager.RegisterSpecialHandler(idString+"F"); //Front icon
				SpriteHandlerManager.RegisterSpecialHandler(idString+"B"); //back icon

				if (MultiIActionGUIToID.ContainsKey(iActionGUIMulti) == false)
				{
					MultiIActionGUIToID[iActionGUIMulti] = new Dictionary<ActionData, string>();
				}

				MultiIActionGUIToID[iActionGUIMulti][actionData] = idString;

				MultiIActionGUIToMind[iActionGUIMulti] = body;
				MultiActivePlayerActions[body][iActionGUIMulti].Add(actionData);
				ShowMulti(idString, body, iActionGUIMulti, actionData);
			}
			else
			{
				if (MultiActivePlayerActions[body].ContainsKey(iActionGUIMulti) == false)
				{
					Loggy.LogError("iActionGUIMulti Not present on mind");
					return;
				}

				if (MultiActivePlayerActions[body][iActionGUIMulti].Contains(actionData) == false)
				{
					Loggy.LogError("actionData Not present on mind");
					return;
				}

				var idString = MultiIActionGUIToID[iActionGUIMulti][actionData];
				SpriteHandlerManager.UnRegisterSpecialHandler(idString+"F"); //Front icon
				SpriteHandlerManager.UnRegisterSpecialHandler(idString+"B"); //back icon
				MultiIActionGUIToID[iActionGUIMulti].Remove(actionData);

				MultiActivePlayerActions[body][iActionGUIMulti].Remove(actionData);
				MultiIActionGUIToMind.Remove(iActionGUIMulti);
				HideMulti(body, iActionGUIMulti, actionData);
			}
		}


		public static void MultiToggleClient(IActionGUIMulti iActionGUIMulti, ActionData actionData, bool show, string ID)
		{
			if (show)
			{
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
				Loggy.Log("iActionGUIMulti Not present", Category.UI);
			}
		}

		public static void SetServerMultiSpriteSO(IActionGUIMulti iActionGUIMulti, ActionData actionData,
			SpriteDataSO sprite, List<Color> palette = null)
		{
			if (Instance.MultiIActionGUIToMind.ContainsKey(iActionGUIMulti) == false)
			{
				Loggy.LogError($"iActionGUI {iActionGUIMulti} Not present To any mind");
				return;
			}

			SetActionUIMessage.SetMultiSpriteSO(Instance.MultiIActionGUIToID[iActionGUIMulti][actionData],Instance.MultiIActionGUIToMind[iActionGUIMulti],
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
				Loggy.Log("iActionGUIMulti Not present", Category.UI);
			}
		}

		public static void SetServerMultiSprite(IActionGUIMulti iActionGUIMulti, ActionData actionData, int Location)
		{
			if (Instance.MultiIActionGUIToMind.ContainsKey(iActionGUIMulti) == false)
			{
				Loggy.LogError($"iActionGUI {iActionGUIMulti} Not present To any mind");
				return;
			}

			SetActionUIMessage.SetMultiSprite(Instance.MultiIActionGUIToID[iActionGUIMulti][actionData], Instance.MultiIActionGUIToMind[iActionGUIMulti],
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
				Loggy.Log("iActionGUIMulti Not present", Category.UI);
			}
		}

		public static void SetServerMultiBackground(IActionGUIMulti iActionGUIMulti, ActionData actionData,
			int Location)
		{
			if (Instance.MultiIActionGUIToMind.ContainsKey(iActionGUIMulti) == false)
			{
				Loggy.LogError($"iActionGUI {iActionGUIMulti} Not present To any mind");
				return;
			}

			SetActionUIMessage.SetMultiBackgroundSprite(Instance.MultiIActionGUIToID[iActionGUIMulti][actionData], Instance.MultiIActionGUIToMind[iActionGUIMulti],
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
				Loggy.Log("iActionGUIMulti not present!", Category.UI);
			}
		}

		public static void SetMultiCooldown(IActionGUIMulti iActionGUIMulti, ActionData actionData, float cooldown,
			GameObject recipient)
		{
			SetActionUIMessage.SetMultiAction(Instance.MultiIActionGUIToID[iActionGUIMulti][actionData],recipient, iActionGUIMulti, actionData, cooldown);
		}

		private static void ShowMulti(string ID, GameObject Body, IActionGUIMulti iActionGUIMulti, ActionData actionData)
		{
			if (CustomNetworkManager.IsServer && Body != null)
			{
				//Send message
				SetActionUIMessage.SetMultiAction(Instance.MultiIActionGUIToID[iActionGUIMulti][actionData],Body, iActionGUIMulti, actionData,
					true);
			}

			if (Body == null)
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

		private static void HideMulti( GameObject Body, IActionGUIMulti iActionGUIMulti, ActionData actionData)
		{
			if (CustomNetworkManager.IsServer && Body != null)
			{
				//Send message
				SetActionUIMessage.SetMultiAction("", Body, iActionGUIMulti, actionData,
					false);
			}

			if (Body == null)
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

								if (Instance.ClientMultiIActionGUIToID[iActionGUIMulti]
								    .TryGetValue(actionData, out var id))
								{
									SpriteHandlerManager.UnRegisterSpecialHandler(id+"F"); //Front icon
									SpriteHandlerManager.UnRegisterSpecialHandler(id+"B"); //back icon
								}
								else
								{
									Loggy.LogWarning("Failed to find ID", Category.UI);
								}

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
					Loggy.Log("iActionGUI Not present", Category.UI);
				}
			}
		}

		#endregion
	}
}
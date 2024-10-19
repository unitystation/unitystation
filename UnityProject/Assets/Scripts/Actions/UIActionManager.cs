using System;
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
		//might be unused
		/*public class ActionAndData
		{
			public ActionData ActionData;
			public string ID;
		}*/


		private Dictionary<GameObject, List<IGameActionHolderSingle>> ActivePlayerActions = new Dictionary<GameObject, List<IGameActionHolderSingle>>();
		private Dictionary<IGameActionHolderSingle, GameObject> IActionGUIToMind = new Dictionary<IGameActionHolderSingle, GameObject>();

		private Dictionary<IGameActionHolderSingle, string> IActionGUIToID = new Dictionary<IGameActionHolderSingle, string>();
		private Dictionary<IGameActionHolderSingle, string> ClientIActionGUIToID = new Dictionary<IGameActionHolderSingle, string>();

		/// <summary>
		/// The dict of all actions keyed to their UUID
		/// </summary>
		private Dictionary<string, IGameActionHolder> AllActionsByUUID = new(); //these need to be added to clear()
		/// <summary>
		/// A dict of action UUIDs keyed to the GameObject they belong to
		/// </summary>
		private Dictionary<GameObject, string> AllActionUUIDsByGameObject = new();

		public GameObject Panel;
		public GameObject TooltipPrefab;

		public void Clear()
		{
			Debug.Log("removed " + CleanupUtil.RidDictionaryOfDeadElements(IActionGUIToID, (u,k)=> u as MonoBehaviour != null) + " from UIActionManager.IActionGUIToID");
			Debug.Log("removed " + CleanupUtil.RidDictionaryOfDeadElements(ClientIActionGUIToID, (u, k) => u as MonoBehaviour != null) + " from UIActionManager.ClientIActionGUIToID");
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

		#region Clientside vars
		/// <summary>
		/// Returns true if an action that is aimable is active.
		/// </summary>
		public bool IsAiming => HasActiveAction && ActiveAction.ActionData.IsAimable;

		public UIAction UIAction;
		public List<UIAction> PooledUIAction = new List<UIAction>();

		public Dictionary<IGameActionHolder, List<UIAction>> DicIActionGUI = new Dictionary<IGameActionHolder, List<UIAction>>();

		public UIAction ActiveAction { get; set; }
		public bool HasActiveAction => ActiveAction != null;
		#endregion Clientside vars

		public void UpdatePlayer(GameObject Body, NetworkConnection requestedBy)
		{
			if (ActivePlayerActions.ContainsKey(Body))
			{
				foreach (var IactionGUI in ActivePlayerActions[Body])
				{
					Show("", IactionGUI, Body);
				}
			}
			SpriteHandlerManager.Instance.UpdateSpecialNewPlayer(requestedBy);
		}


		#region IGameActionHolder
		public static IGameActionHolder GetActionFromGuid(string key)
		{
			return Instance.AllActionsByUUID[key];
		}

		/// <summary>
		/// public wrapper for _RegisterAction()
		/// </summary>
		public static string RegisterAction(IGameActionHolder registeredAction)
		{
			if(Convert.ToBoolean(registeredAction.ActionGuid))
			{
				Loggy.LogError("UIActionManager.RegisterAction() being called on an action that already has a key, aborting.", Category.Actions);
				return registeredAction.ActionGuid;
			}
			return Instance._RegisterAction(registeredAction);
		}

		/// <summary>
		/// register an action holder with us, ideally you should call this ASAP after holder creation, returns the generated ActionKey
		/// </summary>
		private string _RegisterAction(IGameActionHolder registeredAction) //due to being wrapped we assume this is called in a safe context
		{
			string generatedGuid = Guid.NewGuid().ToString();
			AllActionsByUUID[generatedGuid] = registeredAction;
			RegisterTile registerTile = registeredAction.gameObject.GetComponent<RegisterTile>();
			if(!Convert.ToBoolean(registerTile))
				registerTile = registeredAction.gameObject.AddComponent<RegisterTile>();

			registerTile.OnDestroyed += onActionDestroyed;
			AllActionUUIDsByGameObject[registeredAction.gameObject] = generatedGuid;
			return generatedGuid;
		}

		private EventHandler<GameObject> onActionDestroyed = (object sender, GameObject gameObject) =>
		{
			UnregisterAction(Instance.AllActionsByUUID[Instance.AllActionUUIDsByGameObject[gameObject]]);
		};

		public static void UnregisterAction(IGameActionHolder unregisteredAction)
		{
			Instance.AllActionsByUUID.Remove(unregisteredAction.ActionGuid);
		}

		/// <summary>
		/// Set the action button visibility
		/// </summary>
		public static void ToggleServer(GameObject body, IGameActionHolderSingle iActionGUI, bool show)
		{
			Instance.InstantToggleServer(body, iActionGUI, show);
		}

		private void InstantToggleServer(GameObject Body, IGameActionHolderSingle iActionGUI, bool show)
		{
			if (CustomNetworkManager.IsServer == false || Body == null) return;
			if (ActivePlayerActions.ContainsKey(Body) == false)
			{
				ActivePlayerActions[Body] = new List<IGameActionHolderSingle>();
			}

			if (show)
			{
				if (ActivePlayerActions[Body].Contains(iActionGUI))
				{
					Loggy.LogWarning($"[UIActionManager/InstantToggleServer()] - iActionGUI Already present on mind for {Body.name}");
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
				if (ActivePlayerActions[Body].Contains(iActionGUI) == false || IActionGUIToID.ContainsKey(iActionGUI) == false)
				{
					Loggy.LogWarning($"iActionGUI {iActionGUI?.ActionData.OrNull()?.Name}, not present on mind", Category.UI);
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


		public static void ToggleClient(IGameActionHolderSingle iActionGUI, bool show, string ID) //Internal use only!! reeee
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


		public static bool HasActionData(ActionData actionData, [CanBeNull] out IGameActionHolderSingle actionInstance)
		{
			foreach (var key in Instance.DicIActionGUI.Keys)
			{
				if (key is IGameActionHolderSingle keyI && keyI.ActionData == actionData)
				{
					actionInstance = keyI;
					return true;
				}
			}

			actionInstance = null;
			return false;
		}

		public static void SetClientSpriteSO(IGameActionHolderSingle iActionGUI, SpriteDataSO sprite,
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
		public static void SetServerSpriteSO(IGameActionHolderSingle iActionGUI, SpriteDataSO sprite,
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

		public static void SetClientSprite(IGameActionHolderSingle iActionGUI, int Location)
		{
			if (Instance.DicIActionGUI.ContainsKey(iActionGUI))
			{
				var _UIAction = Instance.DicIActionGUI[iActionGUI][0];
				_UIAction.IconFront.SetCatalogueIndexSprite(Location);
			}
			else
			{
				Loggy.Log("iActionGUI Not present", Category.UI);
			}
		}

		public static void SetServerSprite(IGameActionHolderSingle iActionGUI, int Location)
		{
			if (Instance.IActionGUIToMind.ContainsKey(iActionGUI) == false)
			{
				Loggy.LogError($"iActionGUI {iActionGUI} Not present To any mind");
				return;
			}

			SetActionUIMessage.SetSprite(Instance.IActionGUIToID[iActionGUI], Instance.IActionGUIToMind[iActionGUI], iActionGUI, Location);
		}


		public static void SetClientBackground(IGameActionHolderSingle iActionGUI, int Location)
		{
			if (Instance.DicIActionGUI.ContainsKey(iActionGUI))
			{
				var _UIAction = Instance.DicIActionGUI[iActionGUI][0];
				_UIAction.IconBackground.SetCatalogueIndexSprite(Location);
			}
			else
			{
				Loggy.Log("iActionGUI Not present", Category.UI);
			}
		}

		public static void SetServerBackground(IGameActionHolderSingle iActionGUI, int Location)
		{
			if (Instance.IActionGUIToMind.ContainsKey(iActionGUI) == false)
			{
				Loggy.LogError($"iActionGUI {iActionGUI} Not present To any mind");
				return;
			}

			SetActionUIMessage.SetBackgroundSprite(Instance.IActionGUIToID[iActionGUI], Instance.IActionGUIToMind[iActionGUI], iActionGUI,
				Location);
		}


		public static void SetCooldownLocal(IGameActionHolderSingle iActionGUI, float cooldown)
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

		public static void SetCooldown(IGameActionHolderSingle iActionGUI, float cooldown, GameObject recipient)
		{
			SetActionUIMessage.SetAction(Instance.IActionGUIToID[iActionGUI], recipient, iActionGUI, cooldown);
		}

		private static void Show(string ID ,IGameActionHolderSingle iActionGUI, GameObject body)
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
					if (actionButton.Key is IGameActionHolderSingle keyI &&
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

		private static void Hide( IGameActionHolderSingle iAction, GameObject Body)
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
				Debug.LogError("UIActionManager set to null on round end.");
			}

			foreach (var _Actions in DicIActionGUI)
			{
				foreach (var _Action in _Actions.Value)
				{
					_Action.Pool();
				}
			}

			DicIActionGUI = new Dictionary<IGameActionHolder, List<UIAction>>();
		}

		public static void ClearAllActionsServer()
		{
			Instance.IActionGUIToMind.Clear();
			Instance.ActivePlayerActions.Clear();

			Instance.IActionGUIToID.Clear();
			Instance.AllActionsByUUID.Clear();
			Instance.AllActionUUIDsByGameObject.Clear();
		}

		public static void ClearAllActionsClient()
		{
			if (Instance == null) return;
			if (Instance.DicIActionGUI.Count == 0) return;
			for (int i = Instance.DicIActionGUI.Count - 1; i > -1; i--)
			{
				if (Instance.DicIActionGUI.ElementAt(i).Key is IGameActionHolderSingle iActionGui)
				{
					Hide(iActionGui, null);
					continue;
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
	}
}
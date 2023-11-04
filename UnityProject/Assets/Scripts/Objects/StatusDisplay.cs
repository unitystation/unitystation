using System;
using System.Collections;
using System.Collections.Generic;
using CustomInspectors;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Mirror;
using ScriptableObjects;
using Systems.Interaction;
using Managers;
using Doors;
using Logs;
using Shared.Systems.ObjectConnection;
using Systems.Clearance;


namespace Objects.Wallmounts
{
	/// <summary>
	/// Mounted monitor to show simple images or text
	/// Escape Shuttle channel is a priority one and will overtake other channels.
	/// </summary>
	public class StatusDisplay : ImnterfaceMultitoolGUI, IServerLifecycle, ICheckedInteractable<HandApply>,
		IMultitoolMasterable,
		IRightClickable, ICheckedInteractable<ContextMenuApply>, ICheckedInteractable<AiActivate>
	{
		public static readonly int MAX_CHARS_PER_PAGE = 18;

		private Coroutine blinkHandle;

		[SerializeField] private Text textField = default;

		[SyncVar(hook = nameof(SyncSprite))] public MountedMonitorState stateSync;

		[SyncVar(hook = nameof(SyncStatusText))]
		private string statusText = string.Empty;

		[SyncVar(hook = nameof(UpdateTextColor))]
		private Color currentTextColor;

		public bool hasCables = true;
		public SpriteHandler MonitorSpriteHandler;
		public SpriteHandler DisplaySpriteHandler;
		public Sprite openEmpty;
		public Sprite openCabled;
		public Sprite closedOff;
		public SpriteDataSO joeNews;
		public List<DoorController> doorControllers = new List<DoorController>();
		public List<DoorMasterController> NewdoorControllers = new List<DoorMasterController>();
		public CentComm centComm;
		public int currentTimerSeconds;
		public bool countingDown;

		[SerializeField] private Color normalTextColor;
		[SerializeField] private Color redAlertTextColor = Color.red;

		public enum MountedMonitorState
		{
			StatusText,
			Image,
			Off,
			NonScrewedPanel,
			OpenCabled,
			OpenEmpty
		};

		[SerializeField] private StatusDisplayChannel channel = StatusDisplayChannel.Command;

		private StatusDisplayChannel cachedChannel;

		[SerializeField] private MultitoolConnectionType conType = MultitoolConnectionType.DoorButton;
		public MultitoolConnectionType ConType => conType;
		int IMultitoolMasterable.MaxDistance => int.MaxValue;
		private bool multiMaster = true;
		public bool MultiMaster => multiMaster;

		[SerializeField] private AccessRestrictions accessRestrictions;

		public AccessRestrictions AccessRestrictions
		{
			get
			{
				if (accessRestrictions == null)
				{
					accessRestrictions = GetComponent<AccessRestrictions>();
				}

				return accessRestrictions;
			}
		}

		private const string CLEARANCE_LINK_FAIL_STRING = "This unit does not contain the required access to link such device.";
		private const string CLEARANCE_SETUP_NO_COMPONENT = "You wave the ID infront of the status display.. but nothing happens.";
		private const string CLEARANCE_OVERWRITE = "You wave the ID infront of the status display.. " +
		                                           "But it rejects it as clearance has already been setup for it!";
		private const string CLEARANCE_SETUP_SUCC = "You wave the ID infront of the status display " +
		                                                    "and it accepts your card's clearance.";

		public void OnSpawnServer(SpawnInfo info)
		{
			if (!info.SpawnItems)
			{
				hasCables = false;
				stateSync = MountedMonitorState.OpenEmpty;
				statusText = GameManager.Instance.CentComm.CommandStatusString;
			}

			if (doorControllers.Count > 0 || NewdoorControllers.Count > 0  )
			{
				OnTextBroadcastReceived(StatusDisplayChannel.DoorTimer);
			}

			SyncSprite(stateSync, stateSync);
			centComm = GameManager.Instance.CentComm;
			centComm.OnStatusDisplayUpdate.AddListener(OnTextBroadcastReceived);
			centComm.OnAlertLevelChange += ServerUpdateCurrentColor;
		}

		private void Start()
		{
			#if UNITY_EDITOR
			// Chances are we are trying to edit or test a scene and the managers aren't loaded.
			if (Application.isPlaying == false) return;
			#endif
			centComm = GameManager.Instance.CentComm;

		}

		/// <summary>
		/// cleaning up for reuse
		/// </summary>
		public void OnDespawnServer(DespawnInfo info)
		{
			centComm.OnStatusDisplayUpdate.RemoveListener(OnTextBroadcastReceived);
			channel = StatusDisplayChannel.Command;
			textField.text = string.Empty;
			centComm.OnAlertLevelChange -= ServerUpdateCurrentColor;
			this.TryStopCoroutine(ref blinkHandle);
		}

		/// <summary>
		/// SyncVar hook to show text on client.
		/// Text should be 2 pages max
		/// </summary>
		private void SyncStatusText(string oldText, string newText)
		{
			if (newText == null)
				return;

			//display font doesn't have lowercase chars!
			statusText = newText.ToUpper().Substring(0, Mathf.Min(newText.Length, MAX_CHARS_PER_PAGE * 2));
			if (stateSync == MountedMonitorState.StatusText)
			{
				this.RestartCoroutine(BlinkText(), ref blinkHandle);
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.Intent == Intent.Harm) return false;
			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (stateSync == MountedMonitorState.OpenCabled || stateSync == MountedMonitorState.OpenEmpty)
			{
				if (!hasCables && Validations.HasItemTrait(interaction, CommonTraits.Instance.Cable) &&
				    Validations.HasUsedAtLeast(interaction, 5))
				{
					//add 5 cables
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start adding cables to the frame...",
						$"{interaction.Performer.ExpensiveName()} starts adding cables to the frame...",
						"You add cables to the frame.",
						$"{interaction.Performer.ExpensiveName()} adds cables to the frame.",
						() =>
						{
							Inventory.ServerConsume(interaction.HandSlot, 5);
							hasCables = true;
							stateSync = MountedMonitorState.OpenCabled;
						});
				}
				else if (Validations.HasItemTrait(interaction, CommonTraits.Instance.GlassSheet) &&
				         Validations.HasUsedAtLeast(interaction, 2))
				{
					//add 2 glass
					ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
						"You start to put in the glass panel...",
						$"{interaction.Performer.ExpensiveName()} starts to put in the glass panel...",
						"You put in the glass panel.",
						$"{interaction.Performer.ExpensiveName()} puts in the glass panel.",
						() =>
						{
							Inventory.ServerConsume(interaction.HandSlot, 2);
							stateSync = MountedMonitorState.NonScrewedPanel;
						});
				}
				else if (hasCables && Validations.HasItemTrait(interaction, CommonTraits.Instance.Wirecutter))
				{
					//cut out cables
					Chat.AddActionMsgToChat(interaction, $"You remove the cables.",
						$"{interaction.Performer.ExpensiveName()} removes the cables.");
					ToolUtils.ServerPlayToolSound(interaction);
					Spawn.ServerPrefab(CommonPrefabs.Instance.SingleCableCoil, SpawnDestination.At(gameObject), 5);
					stateSync = MountedMonitorState.OpenEmpty;
					hasCables = false;
					currentTimerSeconds = 0;
					doorControllers.Clear();
					NewdoorControllers.Clear();
				}
			}
			else if (stateSync == MountedMonitorState.NonScrewedPanel)
			{
				if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar))
				{
					//remove glass
					Chat.AddActionMsgToChat(interaction, $"You remove the glass panel.",
						$"{interaction.Performer.ExpensiveName()} removes the glass panel.");
					ToolUtils.ServerPlayToolSound(interaction);
					Spawn.ServerPrefab(CommonPrefabs.Instance.GlassSheet, SpawnDestination.At(gameObject), 2);
					if (hasCables)
					{
						stateSync = MountedMonitorState.OpenCabled;
					}
					else
					{
						stateSync = MountedMonitorState.OpenEmpty;
					}
				}
				else if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver))
				{
					//screw in monitor, completing construction
					Chat.AddActionMsgToChat(interaction, $"You connect the monitor.",
						$"{interaction.Performer.ExpensiveName()} connects the monitor.");
					ToolUtils.ServerPlayToolSound(interaction);
					if (hasCables)
					{
						stateSync = MountedMonitorState.StatusText;
					}
				}
			}
			else
			{
				if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver))
				{
					//disconnect the monitor
					Chat.AddActionMsgToChat(interaction, $"You disconnect the monitor.",
						$"{interaction.Performer.ExpensiveName()} disconnect the monitor.");
					ToolUtils.ServerPlayToolSound(interaction);
					stateSync = MountedMonitorState.NonScrewedPanel;
				}

				if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Id))
				{
					if (AccessRestrictions == null)
					{
						Chat.AddExamineMsg(interaction.Performer, CLEARANCE_SETUP_NO_COMPONENT);
						return;
					}

					if (AccessRestrictions.CheckAccess(interaction.HandObject) == false)
					{
						Chat.AddExamineMsg(interaction.Performer, CLEARANCE_OVERWRITE);
						return;
					}

					AccessRestrictions.clearanceRestriction = Clearance.Security;
					Chat.AddExamineMsg(interaction.Performer, CLEARANCE_SETUP_SUCC);
				}
				else if (stateSync == MountedMonitorState.Image)
				{
					ChangeChannelMessage(interaction);
					stateSync = MountedMonitorState.StatusText;
				}
				else if (stateSync == MountedMonitorState.StatusText)
				{
					if (channel == StatusDisplayChannel.DoorTimer)
					{
						if (AccessRestrictions == null || AccessRestrictions.CheckAccess(interaction.Performer))
						{
							AddTime(60);
						}
						else
						{
							Chat.AddExamineMsg(interaction.Performer, $"Access Denied.");
							// Play sound
							SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.AccessDenied,
								gameObject.AssumedWorldPosServer(), sourceObj: gameObject);
						}
					}
					else
					{
						ChangeChannelMessage(interaction);
						stateSync = MountedMonitorState.Image;
					}
				}
			}
		}

		private void ChangeChannelMessage(HandApply interaction)
		{
			Chat.AddActionMsgToChat(interaction, $"You change the channel of the monitor.",
				$"{interaction.Performer.ExpensiveName()} changes the channel of the monitor.");
		}

		private IEnumerator BlinkText()
		{
			textField.text = statusText.Substring(0, Mathf.Min(statusText.Length, MAX_CHARS_PER_PAGE));
			textField.color = currentTextColor;

			yield return WaitFor.Seconds(3);

			int shownChars = textField.cachedTextGenerator.characterCount;
			if (shownChars >= statusText.Length)
			{
				yield break;
			}

			textField.text = statusText.Substring(shownChars);

			yield return WaitFor.Seconds(3);

			this.StartCoroutine(BlinkText(), ref blinkHandle);
		}

		private void ServerUpdateCurrentColor()
		{
			currentTextColor = centComm.CurrentAlertLevel == CentComm.AlertLevel.Red || centComm.CurrentAlertLevel == CentComm.AlertLevel.Delta
				? redAlertTextColor
				: normalTextColor;
		}

		private void UpdateTextColor(Color oldValue, Color newValue)
		{
			textField.color = newValue;
		}

		private void OnTextBroadcastReceived(StatusDisplayChannel broadcastedChannel)
		{
			textField.color = currentTextColor;
			if (broadcastedChannel == StatusDisplayChannel.DoorTimer)
			{
				statusText = FormatTime(currentTimerSeconds, "CELL\n");
				channel = broadcastedChannel;
				return;
			}

			if (channel == StatusDisplayChannel.DoorTimer)
				return;

			if (broadcastedChannel == StatusDisplayChannel.CachedChannel)
			{
				statusText = centComm.CommandStatusString;
				channel = cachedChannel;
				return;
			}

			if (broadcastedChannel == StatusDisplayChannel.EscapeShuttle)
			{
				statusText = centComm.EscapeShuttleTimeString;
				channel = broadcastedChannel;
				return;
			}

			if (channel == StatusDisplayChannel.EscapeShuttle)
				return;

			statusText = centComm.CommandStatusString;
			channel = broadcastedChannel;
			cachedChannel = channel;
		}

		public void LinkDoor(DoorController doorController)
		{
			Loggy.LogWarning("[Deprecated] - old doors have exploits and bugs related to them. " +
			                  "Please avoid using old doors whenever possible.");
			//TODO: Nuke this as it has an exploits that's not really worth anyone's time. Move all doors to V2.
			doorControllers.Add(doorController);
			OnTextBroadcastReceived(StatusDisplayChannel.DoorTimer);
			if (stateSync == MountedMonitorState.Image)
			{
				stateSync = MountedMonitorState.StatusText;
			}
		}

		public void NewLinkDoor(DoorMasterController doorController)
		{
			NewdoorControllers.Add(doorController);
			OnTextBroadcastReceived(StatusDisplayChannel.DoorTimer);
			if (stateSync == MountedMonitorState.Image)
			{
				stateSync = MountedMonitorState.StatusText;
			}
		}

		private void AddTime(int value)
		{
			currentTimerSeconds += value;
			if (currentTimerSeconds > 600)
			{
				ResetTimer();
				return;
			}
			if (countingDown == false)
			{
				StartCoroutine(TickTimer());
			}
			else
			{
				OnTextBroadcastReceived(StatusDisplayChannel.DoorTimer);
			}
		}

		private void RemoveTime(int value)
		{
			currentTimerSeconds -= value;
			if (currentTimerSeconds < 0)
			{
				ResetTimer();
				return;
			}
			OnTextBroadcastReceived(StatusDisplayChannel.DoorTimer);
		}

		private void ResetTimer()
		{
			currentTimerSeconds = 0;
			OnTextBroadcastReceived(StatusDisplayChannel.DoorTimer);
			if (countingDown)
			{
				OpenDoors();
				countingDown = false;
			}
		}

		private IEnumerator TickTimer()
		{
			countingDown = true;
			CloseDoors();
			while (currentTimerSeconds > 0)
			{
				OnTextBroadcastReceived(StatusDisplayChannel.DoorTimer);
				yield return WaitFor.Seconds(1);
				if (countingDown == false)
				{
					yield break; //timer was reset manually
				}
				currentTimerSeconds -= 1;
			}
			ResetTimer();
		}

		// FIXME: replace the way Status display interacts with doors when I make door able to be interacted with devices.
		private void CloseDoors()
		{
			foreach (var door in doorControllers)
			{
				//Todo make The actual console itself ingame Hackble, I wouldn't put it on the door because this could get removed and leave references on the door Still
				//Putting it on this itself would be best
				door.TryClose();
			}

			foreach (var door in NewdoorControllers)
			{
				//Todo make The actual console itself ingame Hackble, I wouldn't put it on the door because this could get removed and leave references on the door Still
				//Putting it on this itself would be best
				door.TryClose();
			}
		}

		// FIXME: replace the way Status display interacts with doors when I make door able to be interacted with devices.
		private void OpenDoors()
		{
			foreach (var door in doorControllers)
			{
				//To do make The actual console itself ingame Hackble
				door.TryOpen(null, true);
			}

			foreach (var door in NewdoorControllers)
			{
				door.TryOpen(null, true);
			}
		}

		public void SyncSprite(MountedMonitorState stateOld, MountedMonitorState stateNew)
		{
			stateSync = stateNew;
			if (stateNew == MountedMonitorState.Off)
			{
				MonitorSpriteHandler.SetSprite(closedOff);
				DisplaySpriteHandler.Empty(networked: false);
				this.TryStopCoroutine(ref blinkHandle);
				textField.text = "";
			}

			if (stateNew == MountedMonitorState.StatusText)
			{
				this.StartCoroutine(BlinkText(), ref blinkHandle);
				DisplaySpriteHandler.Empty(networked: false);
			}
			else if (stateNew == MountedMonitorState.Image)
			{
				DisplaySpriteHandler.SetSpriteSO(joeNews, networked: false);
				this.TryStopCoroutine(ref blinkHandle);
				textField.text = "";
			}
			else if (stateNew == MountedMonitorState.OpenCabled)
			{
				MonitorSpriteHandler.SetSprite(openCabled);
			}
			else if (stateNew == MountedMonitorState.NonScrewedPanel)
			{
				MonitorSpriteHandler.SetSprite(closedOff);
				DisplaySpriteHandler.Empty(networked: false);
				this.TryStopCoroutine(ref blinkHandle);
				textField.text = "";
			}
			else if (stateNew == MountedMonitorState.OpenEmpty)
			{
				MonitorSpriteHandler.SetSprite(openEmpty);
			}
		}

		#region Interaction-ContextMenu

		public RightClickableResult GenerateRightClickOptions()
		{
			var result = RightClickableResult.Create();

			if (!WillInteract(ContextMenuApply.ByLocalPlayer(gameObject, null), NetworkSide.Client)) return result;

			var stopTimerInteraction = ContextMenuApply.ByLocalPlayer(gameObject, "StopTimer");
			result.AddElement("Stop Timer", () => ContextMenuOptionClicked(stopTimerInteraction));

			var addTimeInteraction = ContextMenuApply.ByLocalPlayer(gameObject, "AddTime");
			result.AddElement("Add 1 Min", () => ContextMenuOptionClicked(addTimeInteraction));

			var removeTimeInteraction = ContextMenuApply.ByLocalPlayer(gameObject, "RemoveTime");
			result.AddElement("Take 1 Min", () => ContextMenuOptionClicked(removeTimeInteraction));

			return result;
		}

		private void ContextMenuOptionClicked(ContextMenuApply interaction)
		{
			if (!AccessRestrictions || AccessRestrictions.CheckAccess(interaction.Performer))
			{
				InteractionUtils.RequestInteract(interaction, this);
			}
			else
			{
				Chat.AddExamineMsg(interaction.Performer, $"Access Denied.");
				// Play sound
				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.AccessDenied, gameObject.AssumedWorldPosServer(),
					sourceObj: gameObject);
			}
		}

		public bool WillInteract(ContextMenuApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(ContextMenuApply interaction)
		{
			if (interaction.RequestedOption == "StopTimer")
			{
				ResetTimer();
			}
			else if (interaction.RequestedOption == "AddTime")
			{
				AddTime(60);
			}
			else if (interaction.RequestedOption == "RemoveTime")
			{
				RemoveTime(60);
			}
		}

		#endregion Interaction-ContextMenu

		#region Ai Interaction

		public bool WillInteract(AiActivate interaction, NetworkSide side)
		{
			if (DefaultWillInteract.AiActivate(interaction, side) == false) return false;
			return true;
		}

		public void ServerPerformInteraction(AiActivate interaction)
		{
			if (interaction.ClickType == AiActivate.ClickTypes.CtrlClick)
			{
				ResetTimer();
			}
			else if (interaction.ClickType == AiActivate.ClickTypes.NormalClick)
			{
				AddTime(60);
			}
			else if (interaction.ClickType == AiActivate.ClickTypes.ShiftClick)
			{
				RemoveTime(60);
			}
		}
		#endregion

		public static string FormatTime(int timerSeconds, string prefix = "ETA: ")
		{
			if (timerSeconds < 0)
			{
				return string.Empty;
			}

			return prefix+TimeSpan.FromSeconds( timerSeconds ).ToString( "mm\\:ss" );
		}
	}

	public enum StatusDisplayChannel
	{
		EscapeShuttle,
		Command,
		DoorTimer,
		CachedChannel
	}

	public class StatusDisplayUpdateEvent : UnityEvent<StatusDisplayChannel>
	{
	}
}

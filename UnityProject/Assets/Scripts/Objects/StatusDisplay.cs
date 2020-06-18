using System;
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Mounted monitor to show simple images or text
/// Escape Shuttle channel is a priority one and will overtake other channels.
/// </summary>
public class StatusDisplay : NetworkBehaviour, IServerLifecycle, ICheckedInteractable<HandApply>
{
	public static readonly int MAX_CHARS_PER_PAGE = 18;

	private Coroutine blinkHandle;

	[SerializeField]
	private Text textField;

	[SyncVar(hook = nameof(SyncSprite))] private MountedMonitorState stateSync;
	[SyncVar(hook = nameof(SyncStatusText))] private string statusText = string.Empty;

	public bool hasCables = true;
	public SpriteHandler MonitorSpriteHandler;
	public SpriteHandler DisplaySpriteHandler;
	public Sprite openEmpty;
	public Sprite openCabled;
	public Sprite closedOff;
	public SpriteSheetAndData joeNews;

	public enum MountedMonitorState
	{
		StatusText,
		Image,
		Off,
		NonScrewedPanel,
		OpenCabled,
		OpenEmpty
	};

	public StatusDisplayChannel Channel
	{
		get => channel;
		set
		{
			cachedText = string.Empty;
			channel = value;
		}
	}
	/// <summary>
	/// don't change it in runtime, use Channel property instead
	/// </summary>
	[SerializeField]
	private StatusDisplayChannel channel = StatusDisplayChannel.Command;

	/// <summary>
	/// used for restoring selected channel message when priority message is gone
	/// </summary>
	private string cachedText = String.Empty;

	#region syncvar boilerplate and init

	public void OnSpawnServer(SpawnInfo info)
	{
		if (!info.SpawnItems)
		{
			hasCables = false;
			stateSync = MountedMonitorState.OpenEmpty;
			statusText = GameManager.Instance.CentComm.CommandStatusString;
		}
		SyncStatusText(statusText, statusText);

	}

	/// <summary>
	/// cleaning up for reuse
	/// </summary>
	public void OnDespawnServer(DespawnInfo info)
	{
		GameManager.Instance.CentComm.OnStatusDisplayUpdate.RemoveListener( OnTextBroadcastReceived );
		channel = StatusDisplayChannel.Command;
		textField.text = string.Empty;
		this.TryStopCoroutine( ref blinkHandle );
	}

	private void Start()
	{
		GameManager.Instance.CentComm.OnStatusDisplayUpdate.AddListener( OnTextBroadcastReceived );
	}

	#endregion

	/// <summary>
	/// SyncVar hook to show text on client.
	/// Text should be 2 pages max
	/// </summary>
	private void SyncStatusText(string oldText, string newText)
	{
		//display font doesn't have lowercase chars!
		statusText = newText.ToUpper().Substring( 0, Mathf.Min(newText.Length, MAX_CHARS_PER_PAGE*2) );

		if ( !textField )
		{
			Logger.LogErrorFormat( "text field not found for status display {0}" , Category.Telecoms, this );
			return;
		}
		if (stateSync == MountedMonitorState.StatusText)
		{
			this.RestartCoroutine( BlinkText(), ref blinkHandle );
		}

	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.Intent == Intent.Harm) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (stateSync == MountedMonitorState.OpenCabled || stateSync == MountedMonitorState.OpenEmpty)
		{
			if (!hasCables && Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Cable) &&
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
			else if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.GlassSheet) &&
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
			} else if (hasCables && Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wirecutter))
			{
				//cut out cables
				Chat.AddActionMsgToChat(interaction, $"You remove the cables.",
					$"{interaction.Performer.ExpensiveName()} removes the cables.");
				ToolUtils.ServerPlayToolSound(interaction);
				Spawn.ServerPrefab(CommonPrefabs.Instance.SingleCableCoil, SpawnDestination.At(gameObject), 5);
				stateSync = MountedMonitorState.OpenEmpty;
				hasCables = false;
			}
		} else if (stateSync == MountedMonitorState.NonScrewedPanel)
		{
			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Crowbar))
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
			else if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver))
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
			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver))
			{
				//disconnect the monitor
				Chat.AddActionMsgToChat(interaction, $"You disconnect the monitor.",
					$"{interaction.Performer.ExpensiveName()} disconnect the monitor.");
				ToolUtils.ServerPlayToolSound(interaction);
				stateSync = MountedMonitorState.NonScrewedPanel;
			}
			else if (stateSync == MountedMonitorState.Image)
			{
				ChangeChannelMessage(interaction);
				stateSync = MountedMonitorState.StatusText;
			} else if (stateSync == MountedMonitorState.StatusText)
			{
				ChangeChannelMessage(interaction);
				stateSync = MountedMonitorState.Image;
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

		yield return WaitFor.Seconds( 3 );

		int shownChars = textField.cachedTextGenerator.characterCount;
		if (shownChars >= statusText.Length)
		{
			yield break;
		}
		textField.text = statusText.Substring(shownChars);

		yield return WaitFor.Seconds( 3 );

		this.StartCoroutine( BlinkText(), ref blinkHandle);
	}


	private void OnTextBroadcastReceived(StatusDisplayChannel broadcastedChannel, string broadcastedText)
	{
		bool textIsEmpty = broadcastedText.Length == 0;
		bool channelIsDifferent = broadcastedChannel != channel;

		if ( channelIsDifferent )
		{
			if ( broadcastedChannel == StatusDisplayChannel.EscapeShuttle )
			{
				SyncStatusText(statusText, textIsEmpty ? cachedText : broadcastedText );
			}
		} else
		{
			cachedText = broadcastedText;
			SyncStatusText(statusText, broadcastedText );
		}
	}

	public void SyncSprite(MountedMonitorState stateOld, MountedMonitorState stateNew)
	{
		stateSync = stateNew;
		if (stateNew == MountedMonitorState.Off)
		{
			MonitorSpriteHandler.SetSprite(closedOff);
			DisplaySpriteHandler.SetSprite(null);
			this.TryStopCoroutine( ref blinkHandle );
			textField.text = "";
		}
		if (stateNew == MountedMonitorState.StatusText)
		{
			this.StartCoroutine( BlinkText(), ref blinkHandle);
			DisplaySpriteHandler.SetSprite(null);
		}else if (stateNew == MountedMonitorState.Image)
		{
			DisplaySpriteHandler.SetSprite(joeNews, 0);
			this.TryStopCoroutine( ref blinkHandle );
			textField.text = "";
		}
		else if (stateNew == MountedMonitorState.OpenCabled)
		{
			MonitorSpriteHandler.SetSprite(openCabled);
		}
		else if (stateNew == MountedMonitorState.NonScrewedPanel)
		{
			MonitorSpriteHandler.SetSprite(closedOff);
			DisplaySpriteHandler.SetSprite(null);
			this.TryStopCoroutine( ref blinkHandle );
			textField.text = "";
		}
		else if (stateNew == MountedMonitorState.OpenEmpty)
		{
			MonitorSpriteHandler.SetSprite(openEmpty);
		}
	}
}

public enum StatusDisplayChannel
{
	EscapeShuttle,
	Command
}
public class StatusDisplayUpdateEvent : UnityEvent<StatusDisplayChannel, string> { }
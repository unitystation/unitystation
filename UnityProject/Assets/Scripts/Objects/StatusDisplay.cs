using System;
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Status display that will show short text messages sent to currently selected channel.
/// Escape Shuttle channel is a priority one and will overtake other channels.
/// </summary>
public class StatusDisplay : NetworkBehaviour, IServerLifecycle
{
	public static readonly int MAX_CHARS_PER_PAGE = 18;

	private Coroutine blinkHandle;

	[SerializeField]
	private Text textField;

	[SyncVar(hook = nameof(SyncStatusText))]
	private string statusText = string.Empty;

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

	public override void OnStartClient()
	{
		EnsureInit();
		SyncStatusText(statusText, statusText);
	}

	public override void OnStartServer()
	{
		EnsureInit();
		SyncStatusText(statusText, statusText);
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		SyncStatusText(statusText, statusText);
	}

	/// <summary>
	/// cleaning up for reuse
	/// </summary>
	public void OnDespawnServer(DespawnInfo info)
	{
		GameManager.Instance.CentComm.OnStatusDisplayUpdate.RemoveListener( OnTextBroadcastReceived() );
		channel = StatusDisplayChannel.Command;
		textField.text = string.Empty;
		this.TryStopCoroutine( ref blinkHandle );
	}

	private void Start()
	{
		EnsureInit();
		GameManager.Instance.CentComm.OnStatusDisplayUpdate.AddListener( OnTextBroadcastReceived() );
	}

	private void EnsureInit()
	{
		if ( !textField )
		{
			textField = GetComponentInChildren<Text>();
		}
	}

	#endregion

	/// <summary>
	/// SyncVar hook to show text on client.
	/// Text should be 2 pages max
	/// </summary>
	private void SyncStatusText(string oldText, string newText)
	{
		EnsureInit();
		//display font doesn't have lowercase chars!
		statusText = newText.ToUpper().Substring( 0, Mathf.Min(newText.Length, MAX_CHARS_PER_PAGE*2) );

		if ( !textField )
		{
			Logger.LogErrorFormat( "text field not found for status display {0}" , Category.Telecoms, this );
			return;
		}

		this.RestartCoroutine( BlinkText(), ref blinkHandle );
	}

	private IEnumerator BlinkText()
	{

		textField.text = statusText.Substring( 0, Mathf.Min( statusText.Length, MAX_CHARS_PER_PAGE ) );

		yield return WaitFor.Seconds( 3 );

		int shownChars = textField.cachedTextGenerator.characterCount;

		if ( shownChars >= statusText.Length )
		{
			yield break;
		}

		textField.text = statusText.Substring( shownChars );

		yield return WaitFor.Seconds( 3 );

		this.StartCoroutine( BlinkText(), ref blinkHandle);
	}

	private UnityAction<StatusDisplayChannel, string> OnTextBroadcastReceived()
	{
		return ( broadcastedChannel, broadcastedText ) =>
		{
			bool textIsEmpty = broadcastedText.Length == 0;
			bool channelIsDifferent = broadcastedChannel != channel;

			if ( channelIsDifferent )
			{
				if ( broadcastedChannel == StatusDisplayChannel.EscapeShuttle )
				{
					SyncStatusText(statusText, textIsEmpty ? cachedText : broadcastedText );
				} else
				{
					//ignoring other channels
				}
			} else
			{
				cachedText = broadcastedText;
				SyncStatusText(statusText, broadcastedText );
			}
		};
	}

}

public enum StatusDisplayChannel { None, EscapeShuttle, Command, Cell1, Cell2 }
public class StatusDisplayUpdateEvent : UnityEvent<StatusDisplayChannel, string> { }
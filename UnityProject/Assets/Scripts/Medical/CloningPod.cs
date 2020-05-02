using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;


public class CloningPod : NetworkBehaviour
{
	[SyncVar(hook = nameof(SyncSprite))] public CloningPodStatus statusSync;
	public SpriteRenderer spriteRenderer;
	public Sprite cloningSprite;
	public Sprite emptySprite;
	public string statusString;
	public CloningConsole console;

	public enum CloningPodStatus
	{
		Empty,
		Cloning
	}

	public override void OnStartServer()
	{
		statusString = "Inactive.";
	}

	public override void OnStartClient()
	{
		SyncSprite(statusSync, statusSync);
	}

	public void ServerStartCloning(CloningRecord record)
	{
		statusSync = CloningPodStatus.Cloning;
		statusString = "Cloning cycle in progress.";
		StartCoroutine(ServerProcessCloning(record));
	}

	private IEnumerator ServerProcessCloning(CloningRecord record)
	{
		yield return WaitFor.Seconds(10f);
		statusString  = "Cloning process complete.";
		if (console)
		{
			console.UpdateDisplay();
		}
		if (record.mind.IsOnline())
		{
			PlayerSpawn.ServerClonePlayer(record.mind, transform.position.CutToInt());
		}
		statusSync = CloningPodStatus.Empty;
	}

	public bool CanClone()
	{
		return statusSync == CloningPodStatus.Empty;
	}

	/// <summary>
	/// Updates the cloning pod's status string according to a mind's state
	/// </summary>
	public void UpdateStatusString(CloneableStatus status)
	{
		statusString = statusStrings[status];
	}

	private static Dictionary<CloneableStatus, string> statusStrings =
		new Dictionary<CloneableStatus, string>
		{
			{ CloneableStatus.Cloneable, "Cloning will commence shortly." },
			{ CloneableStatus.OldRecord, "Outdated record." },
			{ CloneableStatus.DenyingCloning, "Spirit is denying cloning." },
			{ CloneableStatus.StillAlive, "Person is still alive." },
			{ CloneableStatus.Offline, "Spirit cannot be found." }
		};

	public void SyncSprite(CloningPodStatus oldValue, CloningPodStatus value)
	{
		statusSync = value;
		if (value == CloningPodStatus.Empty)
		{
			spriteRenderer.sprite = emptySprite;
		}
		else
		{
			spriteRenderer.sprite = cloningSprite;
		}
	}

}

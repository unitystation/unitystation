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
		if(record.mind.IsOnline(record.mind.GetCurrentMob()))
		{
			PlayerSpawn.ServerClonePlayer(record.mind, transform.position.CutToInt());
		}
		statusSync = CloningPodStatus.Empty;
	}

	public bool CanClone()
	{
		if(statusSync == CloningPodStatus.Cloning)
		{
			return false;
		}
		else
		{
			return true;
		}

	}

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

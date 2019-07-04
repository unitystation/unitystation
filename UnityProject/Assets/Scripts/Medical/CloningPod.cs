using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


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
		SyncSprite(statusSync);
	}

	public void StartCloning(CloningRecord record)
	{
		statusSync = CloningPodStatus.Cloning;
		statusString = "Cloning cycle in progress.";
		StartCoroutine(ProcessCloning(record));
	}

	private IEnumerator ProcessCloning(CloningRecord record)
	{
		yield return WaitFor.Seconds(10f);
		statusString  = "Cloning process complete.";
		if (console)
		{
			console.consoleGUI.UpdateDisplay();
		}
		if(record.mind.IsOnline(record.mind.GetCurrentMob()))
		{
			record.mind.ClonePlayer(gameObject, record.characterSettings);
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

	public void SyncSprite(CloningPodStatus value)
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

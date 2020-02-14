using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
/// Main component for nuke.
/// </summary>
public class Nuke : NetworkBehaviour
{
	public float cooldownTimer = 2f;
	public string interactionMessage;
	public string deniedMessage;
	private bool detonated = false;
	public bool IsDetonated => detonated;
	//Nuke code is only populated on the server
	private int nukeCode;
	public int NukeCode => nukeCode;

	private string currentCode = "";
	public string CurrentCode => currentCode;

//	private GameObject Player;

	public override void OnStartServer()
	{
		//calling the code generator and setting up a 10 second timer to post the code in syndicate chat
		CodeGenerator();
		base.OnStartServer();
	}
	/// <summary>
	/// Tries to add new digit to code input
	/// </summary>
	/// <param name="c"></param>
	/// <returns>true if digit is appended ok</returns>
	public bool AppendKey( char c ) {
		int digit;
		if ( int.TryParse( c.ToString(), out digit ) && currentCode.Length < nukeCode.ToString().Length ) {
			currentCode = CurrentCode + digit;
			return true;
		}
		return false;
	}

	IEnumerator PlayNukeDetSound()
	{
		// Wait for 1 second so audio syncs up
		yield return WaitFor.Seconds(1f);
		SoundManager.Play("SelfDestruct");
	}

	IEnumerator WaitForDeath()
	{
		yield return WaitFor.Seconds(5f);
		GibMessage.Send();
		yield return WaitFor.Seconds(15f);
		// Trigger end of round
		GameManager.Instance.EndRound();
	}

	//Server validating the code sent back by the GUI
	[Server]
	public bool Validate()
	{
		if (CurrentCode == NukeCode.ToString())
		{
			detonated = true;
			//if yes, blow up the nuke
			RpcDetonate();
			//Kill Everyone in the universe
			//FIXME kill only people on the station matrix that the nuke was detonated on
			StartCoroutine(WaitForDeath());
			GameManager.Instance.RespawnCurrentlyAllowed = false;
			return true;
		}
		else
		{
			//if no, tell the GUI that it was an incorrect code
			return false;
		}
	}

	//Server telling the nukes to explode
	[ClientRpc]
	void RpcDetonate()
	{
		if(detonated){
			return;
		}
		detonated = true;

		SoundManager.StopAmbient();
		//turning off all the UI except for the right panel
		UIManager.PlayerHealthUI.gameObject.SetActive(false);
		UIManager.Display.hudBottomHuman.gameObject.SetActive(false);
		UIManager.Display.hudBottomGhost.gameObject.SetActive(false);
		ChatUI.Instance.CloseChatWindow();

		//Playing the video
		UIManager.Display.PlayNukeDetVideo();

		//Playing the sound
		StartCoroutine(PlayNukeDetSound());
	}

	[Server]
	public void CodeGenerator()
	{
		nukeCode = Random.Range(1000, 9999);
	}

	public void Clear() {
		currentCode = "";
	}
}

using System.Collections;
using PlayGroup;
using PlayGroups.Input;
using UI;
using UnityEngine;
using UnityEngine.Networking;

public class NukeInteract : NetworkTabTrigger
{
	public float cooldownTimer = 2f;
	public string interactionMessage;
	public string deniedMessage;
	private bool detonated = false;
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

	IEnumerator WaitForDeath()
	{
		yield return new WaitForSeconds(5f);
		GibMessage.Send();
		GameManager.Instance.RespawnAllowed = false;
		yield return new WaitForSeconds(2f);
		//Restart Round:
		GameManager.Instance.RoundTime = 0f;
	}

	//Server validating the code sent back by the GUI
	[Server]
	public bool Validate()
	{
		if (CurrentCode == NukeCode.ToString()) {
			//if yes, blow up the nuke
			RpcDetonate();
			//Kill Everyone in the universe
			//FIXME kill only people on the station matrix that the nuke was detonated on
			StartCoroutine(WaitForDeath());
			return true;
		} else {
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
		UIManager.Display.hudRight.gameObject.SetActive(false);
		UIManager.Display.hudBottom.gameObject.SetActive(false);
		UIManager.Display.backGround.SetActive(false);
		UIManager.Display.logInWindow.SetActive(false);
		UIManager.Display.infoWindow.SetActive(false);

		//Playing the video
		UIManager.Display.selfDestructVideo.SetActive(true);
		//Playing the sound
		SoundManager.Play("SelfDestruct");
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

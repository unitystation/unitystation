using System.Collections;
using UnityEngine;

public class GUI_Nuke : NetTab
{
	private bool isAnchored = true;
	private bool isSafetyOn = true;
	private Nuke nuke;
	private Nuke Nuke {
		get {
			if ( !nuke ) {
				nuke = Provider.GetComponent<Nuke>();
			}

			return nuke;
		}
	}
	private bool cooldown;

	//define elements you want to visually update here

	//Example: with caching (uglier, but cheaper)
	private NetUIElement infoDisplay;
	private NetUIElement InfoDisplay {
		get {
			if ( !infoDisplay ) {
				infoDisplay = this["InfoDisplay"];
			}
			return infoDisplay;
		}
	}
	//Example: without caching (prettier, more expensive)
	private NetUIElement CodeDisplay => this["CodeDisplay"];

	private string InitialInfoText;

	private void Start() {
		//Not doing this for clients
		if ( IsServer ) {
			//			Logger.Log( $"{name} Kinda init. Nuke code is {NukeInteract.NukeCode}" );
			//InitialInfoText = $"Enter {Nuke.NukeCode.ToString().Length}-digit code:";
			//InfoDisplay.SetValue = InitialInfoText;
			InitialInfoText = "Insert the disk!";
			InfoDisplay.SetValue = InitialInfoText;
		}
	}

	public void DiskButton()
	{
		Chat.AddGameWideSystemMsgToChat("DiskButton");
	}

	public void SafetyToggle()
	{
		if (isSafetyOn)
		{
			GetComponent<NetColorChanger>().Value = "FF0600";
		}
		else
		{
			GetComponent<NetColorChanger>().Value = "00FF11";
		}
		Chat.AddGameWideSystemMsgToChat("SafetyToggle");
	}

	public void AnchorNuke()
	{
		//toggle bool
		isAnchored = !isAnchored;
		if(isAnchored)
		{
			GetComponent<NetColorChanger>().Value = "FF0600";
		}
		else
		{
			GetComponent<NetColorChanger>().Value = "00FF11";
		}
		Nuke.AnchorNuke(isAnchored);
		Chat.AddGameWideSystemMsgToChat("AnchorNuke");
	}

	public void ChangeColor()
	{
		
	}

	public void EnterDigit(char digit) {
		if ( cooldown ) {
			return;
		}
		if ( Nuke.AppendKey( digit ) ) {
			int length = Nuke.CurrentCode.Length;
			//replace older digits with asterisks
			string newDigit = Nuke.CurrentCode.Substring( length <= 0 ? 0 : length - 1 );
			CodeDisplay.SetValue = newDigit.PadLeft( length, '*' );
			StartCoroutine( HideCode() );
		}
	}

	private IEnumerator HideCode() {
		yield return WaitFor.Seconds( 1 );
		CodeDisplay.SetValue = "".PadLeft( CodeDisplay.Value.Length, '*' );
	}

	public void Clear() {
		if ( cooldown ) {
			return;
		}
		Nuke.Clear();
		CodeDisplay.SetValue = "";
	}
	public void TryArm() {
		if ( cooldown ) {
			return;
		}

		if (Nuke.Validate()) {
			InfoDisplay.SetValue = "PREPARE TO DIE";
		} else {
			StartCoroutine( ErrorCooldown() );
		}

	}

	public IEnumerator ErrorCooldown() {
		cooldown = true;
		InfoDisplay.SetValue = "Incorrect code!";
		yield return WaitFor.Seconds( 0.5F );
		InfoDisplay.SetValue = "";
		yield return WaitFor.Seconds( 0.5F );
		InfoDisplay.SetValue = "Incorrect code!";
		yield return WaitFor.Seconds( 0.5F );
		InfoDisplay.SetValue = "";
		yield return WaitFor.Seconds( 0.5F );
		InfoDisplay.SetValue = "Incorrect code!";
		yield return WaitFor.Seconds( 0.5F );
		InfoDisplay.SetValue = "";
		yield return WaitFor.Seconds( 0.5F );
		cooldown = false;
		Clear();
		InfoDisplay.SetValue = InitialInfoText;
	}
}